"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { motion, useReducedMotion } from "motion/react";
import { useEffect, useMemo, useState } from "react";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import {
  IconoBrujula,
  IconoFlechaDerecha,
  IconoLuna,
  IconoObjetivo,
  IconoPersonas,
  IconoPin,
  IconoRayo,
  IconoReloj,
} from "@/components/app/Iconos";
import { Entrada, MotionLink, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import HeroePuente from "@/components/inicio/HeroePuente";
import HeroeSinPuentes from "@/components/inicio/HeroeSinPuentes";
import { ComunidadActiva, listarComunidades } from "@/lib/comunidades";
import { Bloque, listarHorario } from "@/lib/horario";
import { LugarViable, ResultadoLugares, buscarLugaresViables } from "@/lib/lugares";
import { ESTADOS_ANIMO } from "@/lib/maqueta";
import { Puente, listarPuentes, duracionMin, hhmm } from "@/lib/puentes";
import { useSesion } from "@/lib/useSesion";
import estilos from "./inicio.module.css";

const CON_ILUSTRACION = ["comida", "estudio", "descanso", "deporte"];

function textoSugerencias(
  cargando: boolean,
  hayPuente: boolean,
  sugerencias: { resultado: ResultadoLugares | null; error: string } | null,
): string {
  if (cargando) return "Buscando lugares para tu puente…";
  if (!hayPuente) return "Cuando tengas un puente, aquí verás los lugares del campus que te caben.";
  if (!sugerencias) return "Buscando lugares para tu puente…";
  if (sugerencias.error) return sugerencias.error;
  const r = sugerencias.resultado;
  if (!r) return "";
  if (r.estado === "OK") return `Lugares del catálogo que caben en tu puente de ${r.minutosDisponibles} min.`;
  return r.mensaje ?? "";
}

const DIAS_SEMANA = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

const ICONO_ESTADO = {
  cansado: IconoLuna,
  enfocado: IconoObjetivo,
  energia: IconoRayo,
  social: IconoPersonas,
};

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

const aHora = (m: number) => `${String(Math.floor(m / 60)).padStart(2, "0")}:${String(m % 60).padStart(2, "0")}`;

/** Tus actividades de hoy: cómo estás en cada una (la creaste, te invitaron o vas). */
function etiquetaActividad(c: ComunidadActiva): string {
  if (c.esMia) return "La creaste tú";
  return c.miEstado === "CONFIRMADO" ? "Vas" : "Te invitaron";
}

function saludoPara(hora: number) {
  if (hora < 12) return "Buenos días";
  if (hora < 19) return "Buenas tardes";
  return "Buenas noches";
}

export default function Inicio() {
  const router = useRouter();
  const { sesion } = useSesion();
  const aviso = useAviso();
  const reducir = useReducedMotion();
  const [ahora, setAhora] = useState(() => new Date());
  const [puentes, setPuentes] = useState<Puente[]>([]);
  const [bloques, setBloques] = useState<Bloque[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");
  const [sugeridas, setSugeridas] = useState<{ puenteId: number; resultado: ResultadoLugares | null; error: string } | null>(null);
  // Tus actividades de hoy (las que creaste y aquellas a las que te invitaron), de Comunidades → Descubrir.
  // null mientras carga o si falló: Inicio no muestra la tarjeta en vez de mostrar un error.
  const [actividades, setActividades] = useState<ComunidadActiva[] | null>(null);

  useEffect(() => {
    const t = setInterval(() => setAhora(new Date()), 30_000);
    return () => clearInterval(t);
  }, []);

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    void Promise.allSettled([listarPuentes(), listarHorario()]).then(([p, h]) => {
      if (!activo) return;
      if (p.status === "fulfilled") setPuentes(p.value);
      else setError((p.reason as Error).message);
      if (h.status === "fulfilled") setBloques(h.value);
      setCargando(false);
    });
    return () => {
      activo = false;
    };
  }, [sesion]);

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    listarComunidades()
      .then((r) => {
        if (!activo) return;
        // Solo las propias y las de las que es parte: las de otros del campus se ven en Descubrir.
        setActividades(r.comunidades.filter((c) => c.esMia || c.miEstado === "PENDIENTE" || c.miEstado === "CONFIRMADO"));
      })
      .catch(() => activo && setActividades(null));
    return () => {
      activo = false;
    };
  }, [sesion]);

  const minutosAhora = ahora.getHours() * 60 + ahora.getMinutes();
  const diaHoy = ahora.getDay();

  const { activo, proximo } = useMemo(() => {
    const deHoy = puentes
      .filter((p) => p.diaSemana === diaHoy)
      .sort((a, b) => aMin(a.horaInicio) - aMin(b.horaInicio));
    return {
      activo: deHoy.find((p) => aMin(p.horaInicio) <= minutosAhora && minutosAhora < aMin(p.horaFin)),
      proximo: deHoy.find((p) => aMin(p.horaInicio) > minutosAhora),
    };
  }, [puentes, diaHoy, minutosAhora]);

  const foco = activo ?? proximo;

  const otroDia = useMemo(() => {
    for (let k = 1; k <= 7; k++) {
      const dia = (diaHoy + k) % 7;
      if (dia === 0) continue;
      const primero = puentes
        .filter((p) => p.diaSemana === dia)
        .sort((a, b) => aMin(a.horaInicio) - aMin(b.horaInicio))[0];
      if (primero) {
        return {
          puente: primero,
          datos: {
            dia: DIAS_SEMANA[dia],
            corto: DIAS_SEMANA[dia].slice(0, 3).toUpperCase(),
            rango: `${hhmm(primero.horaInicio)} – ${hhmm(primero.horaFin)}`,
            minutos: duracionMin(primero),
            enDias: k,
          },
        };
      }
    }
    return undefined;
  }, [puentes, diaHoy]);

  const puenteSugerencias = foco ?? otroDia?.puente;

  useEffect(() => {
    if (!sesion || !puenteSugerencias) return;
    const puenteId = puenteSugerencias.id;
    let activoEfecto = true;
    buscarLugaresViables(puenteId)
      .then((resultado) => {
        if (activoEfecto) setSugeridas({ puenteId, resultado, error: "" });
      })
      .catch((e: Error) => {
        if (activoEfecto) setSugeridas({ puenteId, resultado: null, error: e.message });
      });
    return () => {
      activoEfecto = false;
    };
  }, [sesion, puenteSugerencias]);

  const sugerencias = sugeridas && puenteSugerencias && sugeridas.puenteId === puenteSugerencias.id ? sugeridas : null;
  const lugaresSugeridos: LugarViable[] = sugerencias?.resultado?.estado === "OK" ? sugerencias.resultado.lugares.slice(0, 6) : [];

  const clasesHoy = useMemo(
    () => bloques.filter((b) => b.diaSemana === diaHoy).sort((a, b) => aMin(a.horaInicio) - aMin(b.horaInicio)),
    [bloques, diaHoy],
  );
  const puentesHoy = useMemo(() => puentes.filter((p) => p.diaSemana === diaHoy), [puentes, diaHoy]);

  const siguienteClase = foco ? clasesHoy.find((b) => aMin(b.horaInicio) >= aMin(foco.horaFin)) : undefined;

  const nombre = sesion?.usuario.nombre.split(" ")[0] ?? "";
  const fechaTexto = ahora.toLocaleDateString("es-BO", { weekday: "long", day: "numeric", month: "long" });
  const fecha = fechaTexto.charAt(0).toUpperCase() + fechaTexto.slice(1);

  const minutosRestantes = activo ? aMin(activo.horaFin) - minutosAhora : 0;
  const usado = activo ? Math.round(((minutosAhora - aMin(activo.horaInicio)) / duracionMin(activo)) * 100) : 0;
  let minutosParaIa = 0;
  if (activo) minutosParaIa = minutosRestantes;
  else if (foco) minutosParaIa = duracionMin(foco);
  const rutaIa = foco && minutosParaIa > 0 ? `/recomendaciones?puente=${foco.id}&min=${minutosParaIa}` : "/recomendaciones";

  const tramos = [
    ...clasesHoy.map((b) => ({ tipo: "clase" as const, id: `c${b.id}`, inicio: aMin(b.horaInicio), fin: aMin(b.horaFin), titulo: b.materia })),
    ...puentesHoy.map((p) => ({ tipo: "puente" as const, id: `p${p.id}`, inicio: aMin(p.horaInicio), fin: aMin(p.horaFin), titulo: `Puente de ${duracionMin(p)} min` })),
  ].sort((a, b) => a.inicio - b.inicio);
  const desde = tramos.length ? Math.floor(Math.min(...tramos.map((t) => t.inicio)) / 60) * 60 : 0;
  const hasta = tramos.length ? Math.ceil(Math.max(...tramos.map((t) => t.fin)) / 60) * 60 : 0;
  const total = Math.max(hasta - desde, 60);
  const posicion = (m: number) => `${((m - desde) / total) * 100}%`;
  const ahoraVisible = tramos.length > 0 && minutosAhora >= desde && minutosAhora <= hasta;

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header" className={estilos.cabecera}>
        <p className={estilos.fecha}>{fecha}</p>
        <h1 className={estilos.saludo} aria-label={`${saludoPara(ahora.getHours())}, ${nombre}`}>
          {`${saludoPara(ahora.getHours())},`.split(" ").map((palabra, i) => (
            <motion.span
              key={palabra}
              className={estilos.palabra}
              aria-hidden="true"
              initial={reducir ? false : { opacity: 0, y: 12, filter: "blur(6px)" }}
              animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
              transition={{ duration: 0.6, ease: SALIDA, delay: 0.1 + i * 0.09 }}
            >
              {palabra}
            </motion.span>
          ))}
          <span className={estilos.nombre} aria-hidden="true">
            <motion.span
              className={estilos.resaltador}
              initial={reducir ? false : { scaleX: 0 }}
              animate={{ scaleX: 1 }}
              transition={{ duration: 0.7, ease: SALIDA, delay: 0.55 }}
            />
            <motion.span
              className={estilos.palabra}
              initial={reducir ? false : { opacity: 0, y: 12, filter: "blur(6px)" }}
              animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
              transition={{ duration: 0.6, ease: SALIDA, delay: 0.3 }}
            >
              {nombre}
            </motion.span>
          </span>
        </h1>
      </Pieza>

      {}
      <Pieza as="section" className={estilos.marcoHeroe}>
        {foco ? (
          <HeroePuente
            activo={!!activo}
            minutos={activo ? minutosRestantes : duracionMin(foco)}
            rango={`${hhmm(foco.horaInicio)} – ${hhmm(foco.horaFin)}`}
            empiezaEn={activo ? undefined : aMin(foco.horaInicio) - minutosAhora}
            usado={usado}
            siguiente={siguienteClase ? { materia: siguienteClase.materia, aula: siguienteClase.aula } : undefined}
            rutaIa={rutaIa}
          />
        ) : (
          <HeroeSinPuentes
            cargando={cargando}
            error={error}
            proximo={otroDia ? { ...otroDia.datos } : undefined}
            rutaIa={otroDia ? `/recomendaciones?puente=${otroDia.puente.id}&min=${otroDia.datos.minutos}` : "/recomendaciones"}
          />
        )}
      </Pieza>

      <div className={estilos.columnaIzquierda}>
      {tramos.length > 0 && (
        <Pieza as="section" className={estilos.tarjeta}>
          <div className={estilos.encabezado}>
            <h2 className={estilos.subtitulo}>Tu día</h2>
            <Link href="/horario" className={estilos.verTodo}>
              Horario completo <IconoFlechaDerecha tamano={14} />
            </Link>
          </div>

          <div className={estilos.linea}>
            {tramos.map((t, i) => (
              <motion.span
                key={t.id}
                className={t.tipo === "clase" ? estilos.tramoClase : estilos.tramoPuente}
                style={{ left: posicion(t.inicio), width: `${((t.fin - t.inicio) / total) * 100}%` }}
                tabIndex={0}
                aria-label={`${t.titulo}, de ${aHora(t.inicio)} a ${aHora(t.fin)}`}
                initial={reducir ? false : { scaleX: 0, opacity: 0 }}
                animate={{ scaleX: 1, opacity: 1 }}
                transition={{ duration: 0.6, ease: SALIDA, delay: 0.35 + i * 0.06 }}
              >
                <span className={estilos.globo} aria-hidden="true">
                  <strong>{t.titulo}</strong>
                  {aHora(t.inicio)} – {aHora(t.fin)}
                </span>
              </motion.span>
            ))}
            {ahoraVisible && (
              <span className={estilos.ahora} style={{ left: posicion(minutosAhora) }} aria-hidden="true">
                <span className={estilos.ahoraPunto} />
              </span>
            )}
          </div>
          <div className={estilos.horas} aria-hidden="true">
            <span>{aHora(desde)}</span>
            <span>{aHora(hasta)}</span>
          </div>

          <ul className={estilos.leyenda}>
            <li>
              <span className={estilos.muestraClase} /> {clasesHoy.length} {clasesHoy.length === 1 ? "clase" : "clases"}
            </li>
            <li>
              <span className={estilos.muestraPuente} /> {puentesHoy.length} {puentesHoy.length === 1 ? "puente" : "puentes"}
            </li>
          </ul>
        </Pieza>
      )}

      {actividades && (
        <Pieza as="section" className={`${estilos.tarjeta} ${estilos.zonaActividades}`}>
          <div className={estilos.encabezado}>
            <h2 className={estilos.subtitulo}>
              Tus actividades de hoy
            </h2>
            <Link href="/comunidades?pestana=descubrir" className={estilos.verTodo}>
              Ver todas <IconoFlechaDerecha tamano={14} />
            </Link>
          </div>
          {actividades.length > 0 ? (
            <ul className={estilos.actividades}>
              {actividades.slice(0, 3).map((c) => (
                <li key={c.id}>
                  <Link href={`/comunidades?pestana=descubrir&comunidad=${c.id}`} className={estilos.actividad}>
                    <span className={estilos.actividadTexto}>
                      <strong>{c.actividad}</strong>
                      <small>
                        <IconoReloj tamano={13} /> {c.horaInicio}–{c.horaFin} · <IconoPersonas tamano={13} /> {c.confirmados}/
                        {c.maxParticipantes}
                      </small>
                    </span>
                    <span className={c.esMia || c.miEstado === "CONFIRMADO" ? estilos.actividadMia : estilos.actividadInvitada}>
                      {etiquetaActividad(c)}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <p className={estilos.nota}>
              Hoy no tienes actividades.{" "}
              <Link href="/comunidades?pestana=crear" className={estilos.verTodo}>
                Crea una
              </Link>
            </p>
          )}
          {actividades.length > 3 && <p className={estilos.nota}>Y {actividades.length - 3} más en Comunidades.</p>}
        </Pieza>
      )}

      <Pieza as="section" className={estilos.zonaSugerencias}>
        <div className={estilos.encabezado}>
          <h2 className={estilos.subtitulo}>Sugerencias rápidas</h2>
          <Link href="/campus" className={estilos.verTodo}>
            Ver campus <IconoFlechaDerecha tamano={14} />
          </Link>
        </div>
        {lugaresSugeridos.length > 0 && (
          <div className={estilos.carrusel}>
            {lugaresSugeridos.map((l) => (
              <motion.article
                key={l.lugarId}
                className={`${estilos.lugar} lugar-arte`}
                whileHover={reducir ? undefined : { y: -4 }}
                transition={RESORTE_TOQUE}
              >
                <ArteCategoria categoria={CON_ILUSTRACION.includes(l.categoria.toLowerCase()) ? l.categoria : "Descanso"} fotoUrl={l.fotoUrl} />
                <span className={`${estilos.lugarCategoria} ${estilos[`cat_${l.categoria.toLowerCase()}`] ?? ""}`}>{l.categoria}</span>
                <h3 className={estilos.lugarNombre}>{l.nombre}</h3>
                <p className={estilos.lugarMeta}>
                  <span>
                    <IconoPin tamano={14} /> {l.distanciaM} m
                  </span>
                  <span>
                    <IconoReloj tamano={14} /> {l.tiempoTrasladoMin} min
                  </span>
                </p>
              </motion.article>
            ))}
          </div>
        )}
        <p className={estilos.nota} role="status">
          {textoSugerencias(cargando, !!puenteSugerencias, sugerencias)}{" "}
          {sugerencias?.resultado?.estado === "SIN_ZONA" && (
            <Link href="/configuracion/ubicacion" className={estilos.verTodo}>
              Elegir mi campus
            </Link>
          )}
        </p>
      </Pieza>

      </div>

      <div className={estilos.columnaDerecha}>
      <Pieza as="section" className={estilos.zonaAnimo}>
        <h2 className={estilos.subtitulo}>¿Cómo te sientes?</h2>
        <div className={estilos.estados}>
          {ESTADOS_ANIMO.map((e) => {
            const Icono = ICONO_ESTADO[e.id];
            return (
              <motion.button
                key={e.id}
                type="button"
                className={`${estilos.estado} ${estilos[`estado_${e.id}`]}`}
                onClick={() => router.push(`${rutaIa}${rutaIa.includes("?") ? "&" : "?"}estado=${e.id}`)}
                onPointerMove={seguirPuntero}
                whileHover={reducir ? undefined : { y: -3 }}
                whileTap={{ scale: 0.96 }}
                transition={RESORTE_TOQUE}
              >
                <span className={estilos.estadoIcono}>
                  <Icono tamano={20} />
                </span>
                <span className={estilos.estadoTexto}>
                  <strong>{e.nombre}</strong>
                  <small>{e.descripcion}</small>
                </span>
              </motion.button>
            );
          })}
        </div>
      </Pieza>

      <Pieza as="section" className={estilos.atajos}>
        <MotionLink href="/comunidades" className={estilos.atajo} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
          <span className={estilos.atajoIcono}>
            <IconoPersonas tamano={20} />
          </span>
          <span>
            <strong>Grupo rápido</strong>
            <small>Crea o únete a uno</small>
          </span>
        </MotionLink>
        <MotionLink href="/campus" className={estilos.atajo} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
          <span className={estilos.atajoIcono}>
            <IconoBrujula tamano={20} />
          </span>
          <span>
            <strong>Descubrir</strong>
            <small>El campus</small>
          </span>
        </MotionLink>
      </Pieza>

      </div>

      <AvisoFlotante texto={aviso.texto} />
    </Entrada>
  );
}

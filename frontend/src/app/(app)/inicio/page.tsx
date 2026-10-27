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
import { Bloque, listarHorario } from "@/lib/horario";
import { ESTADOS_ANIMO, LUGARES } from "@/lib/maqueta";
import { Puente, listarPuentes, duracionMin, hhmm } from "@/lib/puentes";
import { useSesion } from "@/lib/useSesion";
import estilos from "./inicio.module.css";

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
  // El puente va en la URL para que la IA pida recomendaciones de ese puente (HU7).
  const rutaIa = foco && minutosParaIa > 0 ? `/ia?puente=${foco.id}&min=${minutosParaIa}` : "/ia";

  // Línea del día: de la primera a la última hora con algo (clase o puente).
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

      {/* El puente: la única superficie azul marino de la pantalla. */}
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
          <div className={estilos.heroeVacio}>
            <p className={estilos.vacioTitulo}>{cargando ? "Buscando tus puentes…" : "Hoy no te quedan puentes"}</p>
            <p className={estilos.vacioTexto}>
              {error ||
                "Un puente es un hueco libre de 45 minutos o más entre dos clases del mismo día. Registra tu horario y aparecerán aquí."}
            </p>
            <MotionLink href="/horario" className={estilos.accionVacio} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
              Ver mi horario <IconoFlechaDerecha tamano={18} />
            </MotionLink>
          </div>
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

      <Pieza as="section" className={estilos.zonaSugerencias}>
        <div className={estilos.encabezado}>
          <h2 className={estilos.subtitulo}>Sugerencias rápidas</h2>
          <Link href="/campus" className={estilos.verTodo}>
            Ver campus <IconoFlechaDerecha tamano={14} />
          </Link>
        </div>
        <div className={estilos.carrusel}>
          {LUGARES.map((l) => (
            <motion.article
              key={l.id}
              className={`${estilos.lugar} lugar-arte`}
              whileHover={reducir ? undefined : { y: -4 }}
              transition={RESORTE_TOQUE}
            >
              <ArteCategoria categoria={l.categoria} />
              <span className={`${estilos.lugarCategoria} ${estilos[`cat_${l.categoria.toLowerCase()}`] ?? ""}`}>{l.categoria}</span>
              <h3 className={estilos.lugarNombre}>{l.nombre}</h3>
              <p className={estilos.lugarMeta}>
                <span>
                  <IconoPin tamano={14} /> {l.distanciaM} m
                </span>
                <span>
                  <IconoReloj tamano={14} /> {l.minutos} min
                </span>
              </p>
            </motion.article>
          ))}
        </div>
        <p className={estilos.nota}>Lugares de ejemplo; las recomendaciones reales salen del catálogo según tu puente.</p>
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
        <motion.button
          type="button"
          className={estilos.atajo}
          onClick={() => aviso.mostrar("Los grupos rápidos estarán disponibles pronto.")}
          whileTap={{ scale: 0.97 }}
          transition={RESORTE_TOQUE}
        >
          <span className={estilos.atajoIcono}>
            <IconoPersonas tamano={20} />
          </span>
          <span>
            <strong>Grupo rápido</strong>
            <small>Pronto</small>
          </span>
        </motion.button>
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

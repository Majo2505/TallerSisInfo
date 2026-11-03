"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { IconoAjustes, IconoCalendario, IconoPersonas, IconoReloj } from "@/components/app/Iconos";
import { Odometro, RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { iniciales } from "@/lib/iniciales";
import { ResultadoMatching, buscarCompaneros, guardarConfiguracionMatching, obtenerConfiguracionMatching } from "@/lib/matching";
import { Puente, duracionMin, hhmm, listarPuentes } from "@/lib/puentes";
import estilos from "./CompanerosLibres.module.css";

const DIAS = ["", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];
const CORTOS = ["", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

function duracion(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

/** El próximo puente desde ahora (hoy más tarde o los días que siguen); si no hay, el primero de la semana. */
function puenteSiguiente(puentes: Puente[]): Puente | undefined {
  const ahora = new Date();
  const hoy = ahora.getDay(); // 0 = domingo
  const minutos = ahora.getHours() * 60 + ahora.getMinutes();
  const orden = [...puentes].sort((a, b) => a.diaSemana - b.diaSemana || aMin(a.horaInicio) - aMin(b.horaInicio));
  return orden.find((p) => p.diaSemana > hoy || (p.diaSemana === hoy && aMin(p.horaFin) > minutos)) ?? orden[0];
}

type Carga =
  | { tipo: "cargando" }
  | { tipo: "error"; mensaje: string }
  | { tipo: "listo"; puentes: Puente[]; visible: boolean };

type Busqueda = { tipo: "buscando" } | { tipo: "error"; mensaje: string } | { tipo: "listo"; resultado: ResultadoMatching };

export default function CompanerosLibres() {
  const reducir = useReducedMotion();
  const [carga, setCarga] = useState<Carga>({ tipo: "cargando" });
  const [elegido, setElegido] = useState<number | null>(null);
  const [busqueda, setBusqueda] = useState<Busqueda>({ tipo: "buscando" });
  const [intento, setIntento] = useState(0);
  const [aceptando, setAceptando] = useState(false);
  const [errorAceptar, setErrorAceptar] = useState("");

  const [intentoCarga, setIntentoCarga] = useState(0);

  // El estado inicial ya es "cargando"; al reintentar se vuelve a él antes de pedir de nuevo.
  useEffect(() => {
    let activo = true;
    Promise.all([listarPuentes(), obtenerConfiguracionMatching()])
      .then(([puentes, config]) => {
        if (!activo) return;
        setCarga({ tipo: "listo", puentes, visible: config.visible });
        setElegido((actual) => (actual && puentes.some((p) => p.id === actual) ? actual : (puenteSiguiente(puentes)?.id ?? null)));
      })
      .catch((e: Error) => activo && setCarga({ tipo: "error", mensaje: e.message }));
    return () => {
      activo = false;
    };
  }, [intentoCarga]);

  const visible = carga.tipo === "listo" && carga.visible;

  useEffect(() => {
    if (!visible || elegido === null) return;
    let activo = true;
    buscarCompaneros(elegido)
      .then((resultado) => activo && setBusqueda({ tipo: "listo", resultado }))
      .catch((e: Error) => activo && setBusqueda({ tipo: "error", mensaje: e.message }));
    return () => {
      activo = false;
    };
  }, [visible, elegido, intento]);

  function elegir(id: number) {
    if (id === elegido) return;
    setBusqueda({ tipo: "buscando" });
    setElegido(id);
  }

  function reintentar() {
    setBusqueda({ tipo: "buscando" });
    setIntento((n) => n + 1);
  }

  async function aceptar() {
    setAceptando(true);
    setErrorAceptar("");
    try {
      const c = await guardarConfiguracionMatching(true);
      setCarga((actual) => (actual.tipo === "listo" ? { ...actual, visible: c.visible } : actual));
    } catch (e) {
      setErrorAceptar((e as Error).message);
    } finally {
      setAceptando(false);
    }
  }

  const aparecer = {
    initial: reducir ? false : ({ opacity: 0, y: 12, filter: "blur(4px)" } as const),
    animate: { opacity: 1, y: 0, filter: "blur(0px)" },
    exit: reducir ? undefined : { opacity: 0, y: -8, filter: "blur(4px)" },
    transition: { duration: 0.28, ease: SALIDA },
  };

  return (
    <section className={estilos.seccion} aria-labelledby="libres-titulo">
      <span className={estilos.chip}>Con tus puentes reales</span>
      <h2 id="libres-titulo" className={estilos.subtitulo}>
        Compañeros libres contigo
      </h2>
      <p className={estilos.texto}>Gente de tu campus con un puente que se cruza con el tuyo, de quien más tiempo comparte contigo a quien menos.</p>

      <AnimatePresence mode="wait" initial={false}>
        {carga.tipo === "cargando" && (
          <motion.div key="cargando" {...aparecer}>
            <Esqueleto />
          </motion.div>
        )}

        {carga.tipo === "error" && (
          <motion.div key="error" className={estilos.estado} role="alert" {...aparecer}>
            <strong>No pudimos cargar tus puentes</strong>
            <p>{carga.mensaje}</p>
            <button
              type="button"
              className={estilos.secundario}
              onClick={() => {
                setCarga({ tipo: "cargando" });
                setIntentoCarga((n) => n + 1);
              }}
            >
              Reintentar
            </button>
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length === 0 && (
          <motion.div key="sin-puentes" className={estilos.estado} {...aparecer}>
            <span className={estilos.estadoIcono}>
              <IconoCalendario tamano={22} />
            </span>
            <strong>Todavía no tienes puentes</strong>
            <p>Registra tu horario: cuando tengas un hueco de 45 minutos o más entre clases, aquí verás quién está libre contigo.</p>
            <Link href="/horario" className={estilos.primario}>
              Registrar mi horario
            </Link>
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length > 0 && !carga.visible && (
          <motion.div key="consentimiento" className={estilos.consentimiento} {...aparecer}>
            <span className={estilos.consentimientoIcono} aria-hidden="true">
              <IconoPersonas tamano={26} />
            </span>
            <div className={estilos.consentimientoTexto}>
              <strong>Encuentra a quién está libre contigo</strong>
              <p>
                Para ver a tus compañeros, ellos también tienen que poder encontrarte. Solo verán tu nombre, tu carrera y el tramo libre que
                comparten. Nunca tu correo.
              </p>
              {errorAceptar && (
                <motion.p
                  key={errorAceptar}
                  className={estilos.error}
                  role="alert"
                  initial={reducir ? false : { x: 0 }}
                  animate={{ x: [0, -8, 8, -5, 5, 0] }}
                  transition={{ duration: 0.42 }}
                >
                  {errorAceptar}
                </motion.p>
              )}
              <motion.button
                type="button"
                className={estilos.primario}
                onClick={() => void aceptar()}
                disabled={aceptando}
                whileTap={{ scale: 0.97 }}
                transition={RESORTE_TOQUE}
              >
                {aceptando ? "Guardando…" : "Aceptar y ver compañeros"}
              </motion.button>
              <small>Puedes cambiarlo cuando quieras en Perfil → Ajustes.</small>
            </div>
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length > 0 && carga.visible && (
          <motion.div key="matching" className={estilos.matching} {...aparecer}>
            <SelectorPuentes puentes={carga.puentes} elegido={elegido} onElegir={elegir} />
            <AnimatePresence mode="wait" initial={false}>
              <motion.div key={`${elegido}-${busqueda.tipo}`} {...aparecer}>
                <Resultado busqueda={busqueda} onReintentar={reintentar} />
              </motion.div>
            </AnimatePresence>
          </motion.div>
        )}
      </AnimatePresence>
    </section>
  );
}

function SelectorPuentes({ puentes, elegido, onElegir }: { puentes: Puente[]; elegido: number | null; onElegir: (id: number) => void }) {
  const reducir = useReducedMotion();
  const orden = [...puentes].sort((a, b) => a.diaSemana - b.diaSemana || aMin(a.horaInicio) - aMin(b.horaInicio));

  return (
    <div className={estilos.selector} role="group" aria-label="Elige uno de tus puentes">
      {orden.map((p) => {
        const activo = p.id === elegido;
        return (
          <motion.button
            key={p.id}
            type="button"
            aria-pressed={activo}
            className={activo ? estilos.puenteActivo : estilos.puente}
            onClick={() => onElegir(p.id)}
            whileTap={{ scale: 0.95 }}
            transition={RESORTE_TOQUE}
          >
            {activo && (
              <motion.span
                layoutId="puente-matching"
                className={estilos.pastilla}
                transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
              />
            )}
            <span className={estilos.puenteTexto}>
              <strong>{CORTOS[p.diaSemana]}</strong>
              <span>
                {hhmm(p.horaInicio)}–{hhmm(p.horaFin)}
              </span>
            </span>
          </motion.button>
        );
      })}
    </div>
  );
}

function Resultado({ busqueda, onReintentar }: { busqueda: Busqueda; onReintentar: () => void }) {
  const reducir = useReducedMotion();

  if (busqueda.tipo === "buscando") return <Esqueleto />;

  if (busqueda.tipo === "error")
    return (
      <div className={estilos.estado} role="alert">
        <strong>No pudimos buscar compañeros</strong>
        <p>{busqueda.mensaje}</p>
        <button type="button" className={estilos.secundario} onClick={onReintentar}>
          Reintentar
        </button>
      </div>
    );

  const { resultado } = busqueda;
  const { puente } = resultado;

  if (resultado.estado === "SIN_ZONA")
    return (
      <div className={estilos.estado}>
        <span className={estilos.estadoIcono}>
          <IconoAjustes tamano={22} />
        </span>
        <strong>Falta elegir tu campus</strong>
        <p>{resultado.mensaje}</p>
        <Link href="/configuracion/ubicacion" className={estilos.primario}>
          Elegir mi campus
        </Link>
      </div>
    );

  if (resultado.estado !== "OK")
    return (
      <div className={estilos.estado}>
        <span className={estilos.estadoIcono}>
          <IconoReloj tamano={22} />
        </span>
        <strong>
          Nadie libre el {DIAS[puente.diaSemana]} de {hhmm(puente.horaInicio)} a {hhmm(puente.horaFin)}
        </strong>
        <p>{resultado.mensaje} Prueba con otro de tus puentes.</p>
      </div>
    );

  const inicio = aMin(puente.horaInicio);
  const total = duracionMin(puente);
  const cantidad = resultado.companeros.length;

  return (
    <div className={estilos.resultado}>
      <p className={estilos.resumen} aria-live="polite">
        <span className={estilos.cifra}>
          <Odometro valor={cantidad} />
        </span>
        {cantidad === 1 ? "compañero libre" : "compañeros libres"} contigo el {DIAS[puente.diaSemana]}, de {hhmm(puente.horaInicio)} a{" "}
        {hhmm(puente.horaFin)}
      </p>

      <motion.ol
        className={estilos.lista}
        initial={reducir ? false : "oculto"}
        animate="visible"
        variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.06 } } }}
      >
        {resultado.companeros.map((c, i) => {
          const desde = ((aMin(c.horaInicio) - inicio) / total) * 100;
          const ancho = (c.minutosEnComun / total) * 100;
          const meta = [c.carrera, c.semestre ? `Semestre ${c.semestre}` : null].filter(Boolean).join(" · ");
          return (
            <motion.li
              key={`${c.nombre}-${c.horaInicio}`}
              className={i === 0 ? estilos.tarjetaTop : estilos.tarjeta}
              variants={{
                oculto: { opacity: 0, y: 16, filter: "blur(4px)" },
                visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.42, ease: SALIDA } },
              }}
            >
              <div className={estilos.cabeza}>
                <span className={estilos.avatar} aria-hidden="true">
                  {iniciales(c.nombre)}
                </span>
                <div className={estilos.quien}>
                  {i === 0 && <span className={estilos.sello}>Más compatible</span>}
                  <strong>{c.nombre}</strong>
                  {meta && <small>{meta}</small>}
                </div>
                <span className={estilos.porcentaje} aria-label={`${c.porcentaje} % de tu puente`}>
                  {c.porcentaje}
                  <small>%</small>
                </span>
              </div>

              <div className={estilos.linea}>
                <div className={estilos.riel} aria-hidden="true">
                  <motion.span
                    className={estilos.tramo}
                    style={{ left: `${desde}%`, width: `${ancho}%` }}
                    initial={reducir ? false : { scaleX: 0 }}
                    animate={{ scaleX: 1 }}
                    transition={{ duration: 0.7, ease: SALIDA, delay: 0.15 + i * 0.06 }}
                  />
                </div>
                <div className={estilos.horas} aria-hidden="true">
                  <span>{hhmm(puente.horaInicio)}</span>
                  <span>{hhmm(puente.horaFin)}</span>
                </div>
              </div>

              <p className={estilos.juntos}>
                <IconoReloj tamano={15} />
                <span>
                  Libres juntos de <strong>{hhmm(c.horaInicio)}</strong> a <strong>{hhmm(c.horaFin)}</strong> · <span className={estilos.duracion}>{duracion(c.minutosEnComun)}</span>
                </span>
              </p>
              {c.franjasCompartidas > 1 && <span className={estilos.franjas}>Coinciden en {c.franjasCompartidas} puentes esta semana</span>}
            </motion.li>
          );
        })}
      </motion.ol>
    </div>
  );
}

function Esqueleto() {
  return (
    <div className={estilos.esqueleto} aria-busy="true" aria-label="Buscando compañeros">
      {[0, 1, 2].map((i) => (
        <span key={i} className={estilos.hueso} style={{ animationDelay: `${i * 120}ms` }} />
      ))}
    </div>
  );
}

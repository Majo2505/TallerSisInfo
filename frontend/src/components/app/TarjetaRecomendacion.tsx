"use client";

import { useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import { Recomendacion, Valoracion, guardarFeedback } from "@/lib/recomendaciones";
import { IconoCheck, IconoDestello, IconoPin, IconoPulgarAbajo, IconoPulgarArriba, IconoReloj } from "./Iconos";
import { NumeroAnimado, RESORTE_TOQUE, SALIDA } from "./Movimiento";
import estilos from "./TarjetaRecomendacion.module.css";

const OPCIONES: { valor: Valoracion; etiqueta: string; Icono: typeof IconoPulgarArriba }[] = [
  { valor: "LIKE", etiqueta: "Me gusta", Icono: IconoPulgarArriba },
  { valor: "DISLIKE", etiqueta: "No me sirve", Icono: IconoPulgarAbajo },
];

// Cuánto se ve el «¡Gracias!» después de votar: una confirmación breve que no interrumpe.
const DURACION_GRACIAS_MS = 2200;

type Props = {
  recomendacion: Recomendacion;
  /** La primera es la mejor opción según el asistente. */
  destacada?: boolean;
};

/** Una recomendación del asistente (HU7) con sus métricas a la vista (HU8). Las cifras salen del catálogo. */
export default function TarjetaRecomendacion({ recomendacion: r, destacada = false }: Props) {
  const reducir = useReducedMotion();
  return (
    <motion.article
      className={`${destacada ? estilos.destacada : estilos.tarjeta} lugar-arte`}
      whileHover={reducir ? undefined : { y: -4 }}
      transition={RESORTE_TOQUE}
    >
      <div className={estilos.portada}>
        <ArteCategoria categoria={r.categoria} />
        {destacada && <span className={estilos.mejor}>Mejor opción</span>}
      </div>

      <div className={estilos.cuerpo}>
        <div className={estilos.cabecera}>
          <h3 className={estilos.nombre}>{r.nombre}</h3>
          <span className={estilos.categoria}>{r.categoria}</span>
        </div>

        <p className={estilos.explicacion}>
          <IconoDestello tamano={16} />
          <span>{r.explicacion}</span>
        </p>

        <dl className={estilos.metricas}>
          <div>
            <dt>
              <IconoPin tamano={14} /> Distancia
            </dt>
            <dd>
              <NumeroAnimado valor={r.distanciaM} /> m
            </dd>
          </div>
          <div>
            <dt>
              <IconoReloj tamano={14} /> Ida y vuelta
            </dt>
            <dd>{r.tiempoTrasladoMin > 0 ? <><NumeroAnimado valor={r.tiempoTrasladoMin} /> min</> : "<1 min"}</dd>
          </div>
          <div>
            <dt>
              <IconoCheck tamano={14} /> Actividad
            </dt>
            <dd>
              <NumeroAnimado valor={r.duracionEstimadaMin} /> min
            </dd>
          </div>
        </dl>
        <Valorar id={r.id} nombre={r.nombre} inicial={r.feedback} />
      </div>
    </motion.article>
  );
}

/**
 * HU9: me gusta / no me sirve. El voto se marca al instante (y se revierte si falla), el ícono elegido rebota y
 * aparece un «¡Gracias!» breve. Votar de nuevo reemplaza el voto anterior: el backend nunca lo duplica.
 */
function Valorar({ id, nombre, inicial }: { id: number; nombre: string; inicial: Valoracion | null }) {
  const reducir = useReducedMotion();
  const [voto, setVoto] = useState<Valoracion | null>(inicial);
  const [guardando, setGuardando] = useState(false);
  const [gracias, setGracias] = useState(0); // cambia en cada voto guardado para volver a mostrar la confirmación
  const [error, setError] = useState("");

  useEffect(() => {
    if (!gracias) return;
    const t = setTimeout(() => setGracias(0), DURACION_GRACIAS_MS);
    return () => clearTimeout(t);
  }, [gracias]);

  async function votar(valor: Valoracion) {
    if (guardando || valor === voto) return;
    const anterior = voto;
    setVoto(valor);
    setGuardando(true);
    setError("");
    try {
      const guardado = await guardarFeedback(id, valor);
      setVoto(guardado.feedback);
      setGracias((n) => n + 1);
    } catch (e) {
      setVoto(anterior);
      setError(e instanceof Error ? e.message : "No pudimos guardar tu valoración. Inténtalo de nuevo.");
    } finally {
      setGuardando(false);
    }
  }

  return (
    <div className={estilos.valorar}>
      <div className={estilos.valorarFila}>
        <span className={estilos.valorarPregunta}>
          <AnimatePresence mode="wait" initial={false}>
            {gracias ? (
              <motion.span
                key="gracias"
                className={estilos.gracias}
                initial={reducir ? false : { opacity: 0, y: 6, filter: "blur(3px)" }}
                animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
                exit={reducir ? undefined : { opacity: 0, y: -6, filter: "blur(3px)" }}
                transition={{ duration: 0.22, ease: SALIDA }}
              >
                <IconoCheck tamano={15} /> ¡Gracias! Nos ayuda a recomendarte mejor.
              </motion.span>
            ) : (
              <motion.span
                key="pregunta"
                initial={reducir ? false : { opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={reducir ? undefined : { opacity: 0, y: -6 }}
                transition={{ duration: 0.22, ease: SALIDA }}
              >
                ¿Te sirve esta recomendación?
              </motion.span>
            )}
          </AnimatePresence>
        </span>
        <div className={estilos.votos} role="group" aria-label={`Valorar ${nombre}`}>
          {OPCIONES.map(({ valor, etiqueta, Icono }) => {
            const elegido = voto === valor;
            return (
              <motion.button
                key={valor}
                type="button"
                className={elegido ? (valor === "LIKE" ? estilos.votoMeGusta : estilos.votoNoMeSirve) : estilos.voto}
                aria-pressed={elegido}
                aria-busy={guardando && elegido}
                onClick={() => void votar(valor)}
                whileTap={{ scale: 0.94 }}
                transition={RESORTE_TOQUE}
              >
                <motion.span
                  key={elegido ? `${valor}-si` : `${valor}-no`}
                  className={estilos.votoIcono}
                  initial={reducir || !elegido ? false : { scale: 0.6, rotate: valor === "LIKE" ? -18 : 18 }}
                  animate={{ scale: 1, rotate: 0 }}
                  transition={{ type: "spring", stiffness: 520, damping: 14 }}
                >
                  <Icono tamano={18} />
                </motion.span>
                {etiqueta}
              </motion.button>
            );
          })}
        </div>
      </div>
      <span className={estilos.anuncio} aria-live="polite">
        {gracias ? `Guardamos tu valoración: ${voto === "LIKE" ? "me gusta" : "no me sirve"}.` : ""}
      </span>
      {error && (
        <motion.p
          key={error}
          className={estilos.errorVoto}
          role="alert"
          initial={reducir ? false : { x: 0 }}
          animate={{ x: [0, -8, 8, -5, 5, 0] }}
          transition={{ duration: 0.42 }}
        >
          {error}
        </motion.p>
      )}
    </div>
  );
}

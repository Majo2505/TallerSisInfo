"use client";

import { motion, useReducedMotion } from "motion/react";
import { SALIDA } from "@/components/app/Movimiento";
import estilos from "./CaminoTest.module.css";

export type ParadaCamino = {
  nombre: string;
  opcional: boolean;
  /** Preguntas del bloque y cuántas tienen respuesta. */
  total: number;
  respondidas: number;
  /** Índice de su primera pregunta, para saltar ahí. */
  primera: number;
};

type Props = {
  paradas: ParadaCamino[];
  /** Bloque donde está la persona. */
  actual: number;
  /** Avance dentro del bloque actual, de 0 a 1 (para que el camino crezca pregunta a pregunta). */
  avanceEnBloque: number;
  deshabilitado?: boolean;
  onIr: (pregunta: number) => void;
};

const ANCHO = 240;
const PASO = 92;
const MARGEN = 30;
/** Las paradas zigzaguean suave: izquierda, derecha, izquierda… */
const X = [52, 168, 70, 176, 60];

/** Curva suave entre paradas (bezier vertical): un camino, no una escalera. */
function trazo(puntos: { x: number; y: number }[]): string {
  return puntos
    .map((p, i) => {
      if (i === 0) return `M ${p.x} ${p.y}`;
      const a = puntos[i - 1];
      const medio = (p.y - a.y) / 2;
      return `C ${a.x} ${a.y + medio}, ${p.x} ${p.y - medio}, ${p.x} ${p.y}`;
    })
    .join(" ");
}

/**
 * El progreso del test como un camino con una parada por bloque. El tramo recorrido se dibuja (pathLength) a medida que
 * avanzas; las paradas hechas se rellenan y la actual late. Se puede tocar una parada para volver a ese bloque.
 */
export default function CaminoTest({ paradas, actual, avanceEnBloque, deshabilitado, onIr }: Props) {
  const reducir = useReducedMotion();
  const alto = MARGEN * 2 + PASO * (paradas.length - 1);
  const puntos = paradas.map((_, i) => ({ x: X[i % X.length], y: MARGEN + i * PASO }));
  const d = trazo(puntos);
  const tramos = Math.max(paradas.length - 1, 1);
  const recorrido = Math.min(1, (actual + Math.min(Math.max(avanceEnBloque, 0), 1)) / tramos);

  return (
    <div className={estilos.camino} style={{ aspectRatio: `${ANCHO} / ${alto}` }}>
      <svg className={estilos.lienzo} viewBox={`0 0 ${ANCHO} ${alto}`} aria-hidden="true" focusable="false">
        <path className={estilos.pendiente} d={d} />
        <motion.path
          className={estilos.hecho}
          d={d}
          initial={false}
          animate={{ pathLength: recorrido }}
          transition={reducir ? { duration: 0 } : { duration: 0.7, ease: SALIDA }}
        />
      </svg>

      <ol className={estilos.paradas}>
        {paradas.map((p, i) => {
          const estado = i < actual ? "hecho" : i === actual ? "actual" : "pendiente";
          const aLaDerecha = puntos[i].x < ANCHO / 2;
          return (
            <li
              key={p.nombre}
              className={estilos.parada}
              style={{ left: `${(puntos[i].x / ANCHO) * 100}%`, top: `${(puntos[i].y / alto) * 100}%` }}
            >
              <button
                type="button"
                className={estilos.boton}
                data-estado={estado}
                data-lado={aLaDerecha ? "derecha" : "izquierda"}
                aria-current={estado === "actual" ? "step" : undefined}
                aria-label={`${p.nombre}: ${p.respondidas} de ${p.total} respondidas${p.opcional ? ", opcional" : ""}`}
                onClick={() => onIr(p.primera)}
                disabled={deshabilitado}
              >
                <span className={estilos.marca} aria-hidden="true">
                  {estado === "actual" && <span className={estilos.latido} />}
                  {estado === "hecho" ? (
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3.4" strokeLinecap="round" strokeLinejoin="round">
                      <path d="m5 12.5 4.5 4.5L19 7.5" />
                    </svg>
                  ) : (
                    <b>{i + 1}</b>
                  )}
                </span>
                <span className={estilos.texto} aria-hidden="true">
                  <strong>{p.nombre}</strong>
                  <small>
                    {estado === "actual" ? "Ahora · " : ""}
                    {p.respondidas} de {p.total}
                    {p.opcional ? " · opcional" : ""}
                  </small>
                </span>
              </button>
            </li>
          );
        })}
      </ol>
    </div>
  );
}

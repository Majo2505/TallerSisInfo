"use client";

import { motion, useReducedMotion } from "motion/react";
import { useId } from "react";
import estilos from "./AlertaDuplicado.module.css";

type Props = {
  mensaje: string;
  tono?: "error" | "revision" | "aviso";
  compacta?: boolean;
  onCerrar?: () => void;
};

const ETIQUETAS: Record<NonNullable<Props["tono"]>, string> = {
  error: "No se pudo publicar · evento duplicado",
  revision: "Posible duplicado en revisión",
  aviso: "Aviso de duplicado · publicado de todas formas",
};

export default function AlertaDuplicado({ mensaje, tono = "revision", compacta = false, onCerrar }: Props) {
  const reducir = useReducedMotion();
  const id = useId().replace(/:/g, "");
  const hojaAtras = `atras-${id}`;
  const hojaAdelante = `adelante-${id}`;
  const cristal = `cristal-${id}`;

  return (
    <motion.section
      className={`${estilos.alerta} ${estilos[tono]} ${compacta ? estilos.compacta : ""}`}
      role={tono === "error" ? "alert" : "status"}
      initial={reducir ? false : { opacity: 0, y: -10, scale: 0.98 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, y: -6 }}
      transition={{ duration: 0.32 }}
    >
      <motion.div
        className={estilos.ilustracion}
        aria-hidden="true"
        animate={reducir ? undefined : { y: [0, -4, 0] }}
        transition={{ duration: 3.4, repeat: Infinity, ease: "easeInOut" }}
      >
        <svg viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id={hojaAtras} x1="10" y1="10" x2="70" y2="80" gradientUnits="userSpaceOnUse">
              <stop stopColor="currentColor" stopOpacity="0.18" />
              <stop offset="1" stopColor="currentColor" stopOpacity="0.05" />
            </linearGradient>
            <linearGradient id={hojaAdelante} x1="34" y1="14" x2="82" y2="76" gradientUnits="userSpaceOnUse">
              <stop stopColor="currentColor" stopOpacity="0.32" />
              <stop offset="1" stopColor="currentColor" stopOpacity="0.12" />
            </linearGradient>
            <linearGradient id={cristal} x1="56" y1="48" x2="84" y2="80" gradientUnits="userSpaceOnUse">
              <stop stopColor="white" stopOpacity="0.5" />
              <stop offset="1" stopColor="white" stopOpacity="0.05" />
            </linearGradient>
          </defs>
          <g transform="rotate(-9 40 48)">
            <rect x="12" y="19" width="50" height="57" rx="9" fill={`url(#${hojaAtras})`} stroke="currentColor" strokeOpacity="0.45" strokeWidth="2" />
            <path d="M22 33h30M22 42h22M22 51h26" stroke="currentColor" strokeOpacity="0.3" strokeWidth="2.5" strokeLinecap="round" />
          </g>
          <g transform="rotate(8 54 44)">
            <rect x="30" y="14" width="50" height="57" rx="9" fill={`url(#${hojaAdelante})`} stroke="currentColor" strokeOpacity="0.7" strokeWidth="2" />
            <rect x="38" y="23" width="18" height="18" rx="5" fill="currentColor" fillOpacity="0.22" />
            <path d="M38 52h34M38 61h24" stroke="currentColor" strokeOpacity="0.45" strokeWidth="2.5" strokeLinecap="round" />
          </g>
          <circle cx="64" cy="58" r="18" fill={`url(#${cristal})`} stroke="currentColor" strokeWidth="3" />
          <path d="M76 71l11 11" stroke="currentColor" strokeWidth="5" strokeLinecap="round" />
          <path d="M64 49v10" stroke="currentColor" strokeWidth="3.5" strokeLinecap="round" />
          <circle cx="64" cy="64" r="1.9" fill="currentColor" />
        </svg>
      </motion.div>
      <div className={estilos.contenido}>
        <strong className={estilos.etiqueta}>{ETIQUETAS[tono]}</strong>
        <p className={estilos.texto}>{mensaje}</p>
        {!compacta && tono !== "aviso" && (
          <p className={estilos.pista}>
            {tono === "error"
              ? "Cambia el título, la fecha o el lugar y vuelve a enviarlo."
              : "Un administrador decidirá si se publica o no."}
          </p>
        )}
      </div>
      {onCerrar && (
        <button type="button" className={estilos.cerrar} aria-label="Cerrar aviso" onClick={onCerrar}>
          ×
        </button>
      )}
    </motion.section>
  );
}

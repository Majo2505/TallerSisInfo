"use client";

import { motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoCalendario, IconoFlechaDerecha } from "@/components/app/Iconos";
import { MotionLink, Odometro, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import estilos from "./HeroeSinPuentes.module.css";

export type ProximoPuente = {
  dia: string; // "Miércoles"
  corto: string; // "MIÉ"
  rango: string; // "10:30 – 12:30"
  minutos: number;
  enDias: number; // 1 = mañana
};

type Props = {
  cargando: boolean;
  error?: string;
  proximo?: ProximoPuente;
  rutaIa: string;
};

/**
 * Héroe de Inicio cuando hoy ya no quedan puentes: fondo vivo, un calendario que pasa la hoja
 * y, si hay, el próximo puente de la semana con sus minutos en odómetro.
 */
export default function HeroeSinPuentes({ cargando, error, proximo, rutaIa }: Props) {
  const reducir = useReducedMotion();
  const cuando = proximo ? (proximo.enDias === 1 ? "Mañana" : proximo.dia) : "";

  let titulo = "Hoy no te quedan puentes";
  if (cargando) titulo = "Buscando tus puentes…";
  else if (error) titulo = "No pudimos revisar tus puentes";
  else if (!proximo) titulo = "Todavía no hay puentes";

  let etiqueta = "Tu semana";
  if (cargando) etiqueta = "Revisando tu semana";
  else if (proximo) etiqueta = "Por hoy terminaste";

  return (
    <section className={estilos.heroe} onPointerMove={seguirPuntero}>
      <FondoVivo />
      <div className={estilos.texto}>
        <p className={estilos.etiqueta}>
          <span className={estilos.punto} aria-hidden="true" /> {etiqueta}
        </p>
        <h2 className={estilos.titulo}>{titulo}</h2>

        {proximo && !cargando ? (
          <motion.div
            className={estilos.proximo}
            initial={reducir ? false : { opacity: 0, y: 12, filter: "blur(4px)" }}
            animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
            transition={{ duration: 0.6, ease: SALIDA, delay: 0.25 }}
          >
            <span className={estilos.proximoEtiqueta}>Tu próximo puente</span>
            <span className={estilos.proximoCifra}>
              <Odometro valor={proximo.minutos} />
              <small>min libres</small>
            </span>
            <span className={estilos.proximoCuando}>
              {cuando}, {proximo.rango}
            </span>
          </motion.div>
        ) : (
          <p className={estilos.descripcion}>
            {error ||
              "Un puente es un hueco libre de 45 minutos o más entre dos clases del mismo día. Registra tu horario y van a aparecer aquí."}
          </p>
        )}

        <div className={estilos.acciones}>
          {proximo && (
            <MotionLink href={rutaIa} className={estilos.primaria} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
              Planea ese puente <IconoFlechaDerecha tamano={18} />
            </MotionLink>
          )}
          <MotionLink
            href="/horario"
            className={proximo ? estilos.secundaria : estilos.primaria}
            whileTap={{ scale: 0.97 }}
            transition={RESORTE_TOQUE}
          >
            <IconoCalendario tamano={18} /> Ver mi horario
          </MotionLink>
        </div>
      </div>

      <Calendario dia={proximo?.corto ?? "—"} />
    </section>
  );
}

/** Calendario que pasa la hoja hasta el día del próximo puente; las estrellas titilan alrededor. */
function Calendario({ dia }: { dia: string }) {
  return (
    <svg className={estilos.calendario} viewBox="0 0 140 120" aria-hidden="true">
      <circle className={`${estilos.estrella} ${estilos.e1}`} cx="16" cy="22" r="2.2" />
      <circle className={`${estilos.estrella} ${estilos.e2}`} cx="126" cy="16" r="1.8" />
      <circle className={`${estilos.estrella} ${estilos.e3}`} cx="130" cy="92" r="2" />
      <path className={`${estilos.destello} ${estilos.d1}`} d="M20 84l2 5 5 2-5 2-2 5-2-5-5-2 5-2Z" />
      <g className={estilos.cuerpo}>
        <rect x="30" y="22" width="80" height="84" rx="14" fill="#ffffff" />
        <rect x="30" y="22" width="80" height="24" rx="14" fill="#fdd537" />
        <rect x="30" y="34" width="80" height="12" fill="#fdd537" />
        <rect x="46" y="14" width="6" height="16" rx="3" fill="#052845" />
        <rect x="88" y="14" width="6" height="16" rx="3" fill="#052845" />
        <text x="70" y="88" className={estilos.dia}>
          {dia}
        </text>
        <rect className={estilos.hoja} x="30" y="46" width="80" height="60" rx="0" fill="#e8eef6" />
      </g>
    </svg>
  );
}

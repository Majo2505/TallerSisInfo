"use client";

import { motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import fotoBanca from "@/components/app/fotos/banca.webp";
import { IconoCalendario, IconoFlechaDerecha } from "@/components/app/Iconos";
import { MotionLink, Odometro, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import estilos from "./HeroeSinPuentes.module.css";

export type ProximoPuente = {
  dia: string;
  corto: string;
  rango: string;
  minutos: number;
  enDias: number;
  despuesDeClases?: boolean;
};

type Props = {
  cargando: boolean;
  error?: string;
  proximo?: ProximoPuente;
  rutaIa: string;
};

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
      <FondoVivo foto={fotoBanca} enfoque="78% 45%" />
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
            <span className={estilos.proximoEtiqueta}>{proximo.despuesDeClases ? "Libre después de clases" : "Tu próximo puente"}</span>
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
              {proximo.despuesDeClases ? "Planea esa tarde" : "Planea ese puente"} <IconoFlechaDerecha tamano={18} />
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

    </section>
  );
}

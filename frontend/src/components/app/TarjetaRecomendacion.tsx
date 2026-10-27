"use client";

import { motion, useReducedMotion } from "motion/react";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import { Recomendacion } from "@/lib/recomendaciones";
import { IconoCheck, IconoDestello, IconoPin, IconoReloj } from "./Iconos";
import { NumeroAnimado, RESORTE_TOQUE } from "./Movimiento";
import estilos from "./TarjetaRecomendacion.module.css";

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
      </div>
    </motion.article>
  );
}

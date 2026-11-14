"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { motion, useReducedMotion } from "motion/react";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import { LugarViable, buscarLugaresViables } from "@/lib/lugares";
import { IconoFlechaDerecha, IconoPin, IconoReloj } from "./Iconos";
import { SALIDA } from "./Movimiento";
import Puntuacion, { SelloVerificado } from "./Puntuacion";
import estilos from "./MejorValorados.module.css";

const MAXIMO = 8;

/** Primero los mejor puntuados; los que todavía no tienen votos, al final y por cercanía. */
function ordenar(lugares: LugarViable[]): LugarViable[] {
  return [...lugares]
    .sort((a, b) => (b.puntuacion ?? -1) - (a.puntuacion ?? -1) || a.distanciaM - b.distanciaM)
    .slice(0, MAXIMO);
}

/**
 * Debajo de los ánimos: lugares reales que caben en tu puente, ordenados por lo que votaron otros estudiantes.
 * Llena el espacio vacío con algo útil antes de elegir, sin inventar nada.
 */
export default function MejorValorados({ puenteId }: { puenteId: number | undefined }) {
  const reducir = useReducedMotion();
  const [lugares, setLugares] = useState<LugarViable[] | null>(null);

  useEffect(() => {
    if (!puenteId) return;
    let activo = true;
    buscarLugaresViables(puenteId)
      .then((r) => activo && setLugares(ordenar(r.lugares ?? [])))
      .catch(() => activo && setLugares([]));
    return () => {
      activo = false;
    };
  }, [puenteId]);

  if (!puenteId || (lugares !== null && lugares.length === 0)) return null;

  const conVotos = lugares?.some((l) => l.puntuacion != null) ?? false;

  return (
    <section className={estilos.seccion} aria-labelledby="mejor-valorados">
      <div className={estilos.cabecera}>
        <div>
          <h2 id="mejor-valorados" className={estilos.titulo}>
            {conVotos ? "Lo mejor valorado para tu puente" : "Lugares que caben en tu puente"}
          </h2>
          <p className={estilos.bajada}>
            {conVotos ? "Según los votos de otros estudiantes del campus." : "Todavía nadie votó: cuando pruebes uno, cuéntanos si te sirvió."}
          </p>
        </div>
        <Link href="/campus" className={estilos.verTodo}>
          Ver campus <IconoFlechaDerecha tamano={14} />
        </Link>
      </div>

      {lugares === null ? (
        <div className={estilos.fila} aria-busy="true" aria-label="Buscando lugares">
          {[0, 1, 2].map((i) => (
            <span key={i} className={estilos.hueso} />
          ))}
        </div>
      ) : (
        <motion.ul
          className={estilos.fila}
          initial={reducir ? false : "oculto"}
          whileInView="visible"
          viewport={{ once: true, margin: "-60px" }}
          variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.06 } } }}
        >
          {lugares.map((l) => (
            <motion.li
              key={l.lugarId}
              className={`${estilos.lugar} lugar-arte`}
              variants={{
                oculto: { opacity: 0, y: 18, scale: 0.97 },
                visible: { opacity: 1, y: 0, scale: 1, transition: { duration: 0.5, ease: SALIDA } },
              }}
            >
              <div className={estilos.portada}>
                <ArteCategoria categoria={l.categoria} fotoUrl={l.fotoUrl} />
                {l.validado && (
                  <span className={estilos.sello}>
                    <SelloVerificado />
                  </span>
                )}
                <span className={estilos.puntaje}>
                  <Puntuacion valor={l.puntuacion} votos={l.votos} variante="sobreFoto" />
                </span>
              </div>
              <div className={estilos.cuerpo}>
                <h3 className={estilos.nombre}>{l.nombre}</h3>
                <p className={estilos.meta}>
                  <span>{l.categoria}</span>
                  <span>
                    <IconoPin tamano={13} /> {l.distanciaM} m
                  </span>
                  <span>
                    <IconoReloj tamano={13} /> {l.tiempoTrasladoMin > 0 ? `${l.tiempoTrasladoMin} min` : "<1 min"}
                  </span>
                </p>
              </div>
            </motion.li>
          ))}
        </motion.ul>
      )}
    </section>
  );
}

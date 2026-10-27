"use client";

import { motion, useMotionValue, useReducedMotion, useSpring, useTransform } from "motion/react";
import { IconoCalendario, IconoFlechaDerecha } from "@/components/app/Iconos";
import { MotionLink, Odometro, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import estilos from "./HeroePuente.module.css";

const RADIO = 30;
const CIRCUNFERENCIA = 2 * Math.PI * RADIO;

type Props = {
  activo: boolean;
  minutos: number;
  rango: string;
  empiezaEn?: number;
  usado: number;
  siguiente?: { materia: string; aula: string | null };
  rutaIa: string;
};

/**
 * La tarjeta del puente: la única superficie azul marino de Inicio.
 * Con mouse, la luz sigue al cursor y la tarjeta se inclina en 3D con resorte (decorativo, no cambia la tarea).
 */
export default function HeroePuente({ activo, minutos, rango, empiezaEn, usado, siguiente, rutaIa }: Props) {
  const reducir = useReducedMotion();
  const x = useMotionValue(0);
  const y = useMotionValue(0);
  const resorte = { stiffness: 140, damping: 18, mass: 0.6 };
  const girarX = useSpring(useTransform(y, [-0.5, 0.5], [5, -5]), resorte);
  const girarY = useSpring(useTransform(x, [-0.5, 0.5], [-7, 7]), resorte);

  function mover(e: React.PointerEvent<HTMLDivElement>) {
    if (e.pointerType !== "mouse" || reducir) return;
    const caja = e.currentTarget.getBoundingClientRect();
    x.set((e.clientX - caja.left) / caja.width - 0.5);
    y.set((e.clientY - caja.top) / caja.height - 0.5);
    seguirPuntero(e);
  }

  function soltar() {
    x.set(0);
    y.set(0);
  }

  return (
    <motion.div
      className={estilos.heroe}
      onPointerMove={mover}
      onPointerLeave={soltar}
      style={{ rotateX: girarX, rotateY: girarY, transformPerspective: 1200 }}
    >
      {/* Fondo vivo: tres luces que flotan, grano y un foco que sigue al mouse. */}
      <div className={estilos.fondo} aria-hidden="true">
        <span className={estilos.luzAzul} />
        <span className={estilos.luzCeleste} />
        <span className={estilos.luzAmarilla} />
        <span className={estilos.foco} />
        <span className={estilos.grano} />
      </div>

      <div className={estilos.cuerpo}>
        <div className={estilos.datos}>
          <span className={estilos.estado}>
            <span className={activo ? estilos.latido : estilos.puntoQuieto} aria-hidden="true" />
            {activo ? "Puente activo" : "Próximo puente"}
          </span>

          <p className={estilos.minutos}>
            <Odometro valor={minutos} />
            <span className={estilos.unidad}>min libres</span>
          </p>

          <p className={estilos.rango}>
            {rango}
            {empiezaEn !== undefined && <span className={estilos.falta}>empieza en {empiezaEn} min</span>}
          </p>

          {siguiente && (
            <p className={estilos.siguiente}>
              <IconoCalendario tamano={16} />
              <span>
                Después: <strong>{siguiente.materia}</strong>
                {siguiente.aula ? `, ${siguiente.aula}` : ""}
              </span>
            </p>
          )}
        </div>

        {activo && (
          <div className={estilos.anillo} role="img" aria-label={`${usado}% del puente usado`}>
            <svg viewBox="0 0 72 72" aria-hidden="true">
              <circle cx="36" cy="36" r={RADIO} className={estilos.pista} />
              <motion.circle
                cx="36"
                cy="36"
                r={RADIO}
                className={estilos.progreso}
                strokeDasharray={CIRCUNFERENCIA}
                initial={{ strokeDashoffset: reducir ? CIRCUNFERENCIA * (1 - usado / 100) : CIRCUNFERENCIA }}
                animate={{ strokeDashoffset: CIRCUNFERENCIA * (1 - usado / 100) }}
                transition={{ duration: 1.4, ease: SALIDA, delay: 0.3 }}
                transform="rotate(-90 36 36)"
              />
              {/* Punto de luz que recorre el anillo hasta el porcentaje usado. */}
              <motion.g
                style={{ transformOrigin: "36px 36px", transformBox: "view-box" }}
                initial={{ rotate: reducir ? usado * 3.6 : 0 }}
                animate={{ rotate: usado * 3.6 }}
                transition={{ duration: 1.4, ease: SALIDA, delay: 0.3 }}
              >
                <circle cx="36" cy="6" r="6" className={estilos.halo} />
                <circle cx="36" cy="6" r="3.2" className={estilos.orbita} />
              </motion.g>
            </svg>
            <span className={estilos.anilloTexto}>
              <span>{usado}%</span>
              <small>usado</small>
            </span>
          </div>
        )}

        <MotionLink
          href={rutaIa}
          className={estilos.accion}
          whileHover={reducir ? undefined : { y: -2 }}
          whileTap={{ scale: 0.97 }}
          transition={RESORTE_TOQUE}
        >
          <span>¿Qué hago con mi tiempo?</span>
          <span className={estilos.accionFlecha}>
            <IconoFlechaDerecha tamano={18} />
          </span>
        </MotionLink>
      </div>
    </motion.div>
  );
}

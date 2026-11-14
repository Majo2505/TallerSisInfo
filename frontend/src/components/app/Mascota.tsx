"use client";

import { useEffect, useRef } from "react";
import { motion, useReducedMotion, useSpring } from "motion/react";
import estilos from "./Mascota.module.css";

/**
 * Breaki, la mascota de BREAKU: un cuerpo azul marino con un puente amarillo encima (el «puente» entre clases).
 * Sus pupilas siguen al cursor con un resorte, parpadea de vez en cuando y, si piensa, mira hacia arriba.
 * Es decorativa: siempre va con aria-hidden y el texto de al lado dice lo que hace falta.
 */
type Props = {
  /** `parada` para el chat; `recostada` boca abajo, mirando hacia abajo (sobre el horario). */
  pose?: "parada" | "recostada";
  tamano?: number;
  /** Mientras la IA responde: mira hacia arriba y no sigue al cursor. */
  pensando?: boolean;
  /** Saluda con el brazo al aparecer (solo la pose parada). */
  saludar?: boolean;
  className?: string;
};

const ALCANCE = 2.4; // cuánto se mueven las pupilas dentro del ojo, en unidades del viewBox

export default function Mascota({ pose = "parada", tamano = 64, pensando = false, saludar = false, className }: Props) {
  const reducir = useReducedMotion();
  const raiz = useRef<SVGSVGElement>(null);
  const x = useSpring(0, { stiffness: 180, damping: 18, mass: 0.6 });
  const y = useSpring(0, { stiffness: 180, damping: 18, mass: 0.6 });

  useEffect(() => {
    if (reducir) return;
    if (pensando) {
      x.set(1.2);
      y.set(-ALCANCE);
      return;
    }
    x.set(0);
    y.set(pose === "recostada" ? 1.2 : 0);
    // Solo con mouse: en el celular no hay cursor que seguir.
    if (!window.matchMedia("(hover: hover) and (pointer: fine)").matches) return;

    function mover(e: PointerEvent) {
      const caja = raiz.current?.getBoundingClientRect();
      if (!caja) return;
      const dx = e.clientX - (caja.left + caja.width / 2);
      const dy = e.clientY - (caja.top + caja.height * 0.55);
      const distancia = Math.hypot(dx, dy) || 1;
      const fuerza = Math.min(1, distancia / 160);
      x.set((dx / distancia) * ALCANCE * fuerza);
      y.set((dy / distancia) * ALCANCE * fuerza);
    }
    window.addEventListener("pointermove", mover, { passive: true });
    return () => window.removeEventListener("pointermove", mover);
  }, [reducir, pensando, pose, x, y]);

  const ojos = (
    <g className={estilos.parpado}>
      <ellipse cx="25" cy="35" rx="5.6" ry="6.6" className={estilos.ojo} />
      <ellipse cx="39" cy="35" rx="5.6" ry="6.6" className={estilos.ojo} />
      <motion.g style={{ x, y }}>
        <circle cx="25.6" cy="35.6" r="3.1" className={estilos.pupila} />
        <circle cx="39.6" cy="35.6" r="3.1" className={estilos.pupila} />
        <circle cx="26.8" cy="34.2" r="1" className={estilos.brillo} />
        <circle cx="40.8" cy="34.2" r="1" className={estilos.brillo} />
      </motion.g>
    </g>
  );

  if (pose === "recostada") {
    // Boca abajo: el cuerpo estirado, la cabeza a la derecha apoyada en las manos y las piernas al aire.
    return (
      <svg
        ref={raiz}
        viewBox="0 0 140 72"
        width={(tamano * 140) / 72}
        height={tamano}
        className={`${estilos.mascota} ${className ?? ""}`}
        aria-hidden="true"
        data-quieta={reducir || undefined}
      >
        <ellipse cx="74" cy="69" rx="58" ry="3" className={estilos.sombra} />
        <g className={estilos.piernaA}>
          <rect x="26" y="22" width="10" height="30" rx="5" className={estilos.cuerpoOscuro} />
          <rect x="23" y="17" width="16" height="9" rx="4.5" className={estilos.zapato} />
        </g>
        <g className={estilos.piernaB}>
          <rect x="41" y="27" width="10" height="25" rx="5" className={estilos.cuerpoOscuro} />
          <rect x="38" y="22" width="16" height="9" rx="4.5" className={estilos.zapato} />
        </g>
        <rect x="18" y="44" width="80" height="24" rx="12" className={estilos.cuerpo} />
        <g className={estilos.respiro}>
          <g transform="translate(76 6)">
            <path d="M20 19 Q32 3 44 19" className={estilos.puente} />
            <path d="M22 18v-3M42 18v-3" className={estilos.puentePilar} />
            <rect x="10" y="17" width="44" height="42" rx="19" className={estilos.cuerpo} />
            {ojos}
            <path d="M28.5 46 q3.5 2.6 7 0" className={estilos.boca} />
            <ellipse cx="19" cy="44" rx="3" ry="1.8" className={estilos.mejilla} />
            <ellipse cx="45" cy="44" rx="3" ry="1.8" className={estilos.mejilla} />
          </g>
        </g>
        <ellipse cx="92" cy="66" rx="8" ry="4.5" className={estilos.mano} />
        <ellipse cx="124" cy="66" rx="8" ry="4.5" className={estilos.mano} />
      </svg>
    );
  }

  return (
    <svg
      ref={raiz}
      viewBox="0 0 64 64"
      width={tamano}
      height={tamano}
      className={`${estilos.mascota} ${className ?? ""}`}
      aria-hidden="true"
      data-quieta={reducir || undefined}
      data-pensando={pensando || undefined}
    >
      <g className={estilos.flote}>
        <path d="M20 19 Q32 3 44 19" className={estilos.puente} />
        <path d="M22 18v-3M42 18v-3" className={estilos.puentePilar} />
        {saludar && (
          <g className={estilos.brazo}>
            <rect x="50" y="30" width="8" height="15" rx="4" className={estilos.cuerpo} />
          </g>
        )}
        <rect x="10" y="17" width="44" height="42" rx="19" className={estilos.cuerpo} />
        {ojos}
        <path d="M28.5 46 q3.5 2.6 7 0" className={estilos.boca} />
        <ellipse cx="19" cy="44" rx="3" ry="1.8" className={estilos.mejilla} />
        <ellipse cx="45" cy="44" rx="3" ry="1.8" className={estilos.mejilla} />
      </g>
    </svg>
  );
}

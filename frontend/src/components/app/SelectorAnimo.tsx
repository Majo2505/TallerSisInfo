"use client";

import { useState } from "react";
import { motion, useReducedMotion } from "motion/react";
import { ESTADOS_ANIMO, EstadoAnimo } from "@/lib/maqueta";
import { SALIDA, seguirPuntero } from "./Movimiento";
import estilos from "./SelectorAnimo.module.css";

/** Tiempo que se ve la elección (onda y realce) antes de pasar a las recomendaciones. */
const PAUSA_ELECCION_MS = 420;

type Onda = { x: number; y: number };

/** Estado de cada tarjeta tras elegir; todos los valores van completos para que Motion no vuelva al estado oculto. */
function animacionTarjeta(esElegido: boolean, apartado: boolean) {
  if (esElegido) {
    return {
      opacity: 1,
      filter: "blur(0px)",
      transform: "translateY(-6px) scale(1.03)",
      transition: { type: "spring" as const, duration: 0.4, bounce: 0.25 },
    };
  }
  if (apartado) {
    return {
      opacity: 0.35,
      filter: "blur(2px)",
      transform: "translateY(0px) scale(0.96)",
      transition: { duration: 0.25, ease: SALIDA },
    };
  }
  return undefined;
}

/**
 * Elección del ánimo (HU7): cuatro tarjetas que entran escalonadas. Cada ícono es una pequeña escena
 * que se anima con su tema (la luna se mece, el dardo da en el blanco, el rayo se carga, las personas se acercan).
 * Al elegir, una onda nace desde el punto tocado y las demás tarjetas se apartan.
 */
export default function SelectorAnimo({ onElegir }: { onElegir: (estado: EstadoAnimo) => void }) {
  const reducir = useReducedMotion();
  const [elegido, setElegido] = useState<EstadoAnimo | null>(null);
  const [onda, setOnda] = useState<Onda | null>(null);

  function elegir(id: EstadoAnimo, evento: React.MouseEvent<HTMLButtonElement>) {
    if (elegido) return;
    if (reducir) {
      onElegir(id);
      return;
    }
    const caja = evento.currentTarget.getBoundingClientRect();
    // Con teclado no hay coordenadas: la onda nace del centro.
    const x = evento.clientX ? evento.clientX - caja.left : caja.width / 2;
    const y = evento.clientY ? evento.clientY - caja.top : caja.height / 2;
    setOnda({ x, y });
    setElegido(id);
    setTimeout(() => onElegir(id), PAUSA_ELECCION_MS);
  }

  return (
    <motion.div
      className={estilos.grilla}
      variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.08, delayChildren: 0.05 } } }}
    >
      {ESTADOS_ANIMO.map((e, i) => {
        const esElegido = elegido === e.id;
        const apartado = elegido !== null && !esElegido;
        return (
          <motion.button
            key={e.id}
            type="button"
            className={`${estilos.tarjeta} ${estilos[e.id]} animo-escena`}
            style={{ ["--i" as string]: i }}
            onClick={(ev) => elegir(e.id, ev)}
            onPointerMove={seguirPuntero}
            variants={{
              oculto: { opacity: 0, transform: "translateY(28px) scale(0.94)", filter: "blur(6px)" },
              visible: {
                opacity: 1,
                transform: "translateY(0px) scale(1)",
                filter: "blur(0px)",
                transition: { type: "spring", duration: 0.7, bounce: 0.2 },
              },
            }}
            animate={animacionTarjeta(esElegido, apartado)}
            whileHover={reducir ? undefined : { transform: "translateY(-5px) scale(1)" }}
            whileTap={{ transform: "translateY(0px) scale(0.97)" }}
            transition={{ type: "spring", duration: 0.35, bounce: 0.15 }}
            data-elegido={esElegido || undefined}
            aria-pressed={esElegido}
          >
            {esElegido && onda && (
              <motion.span
                className={estilos.onda}
                style={{ left: onda.x, top: onda.y }}
                initial={{ opacity: 0.45, transform: "translate(-50%, -50%) scale(0.2)" }}
                animate={{ opacity: 0, transform: "translate(-50%, -50%) scale(1)" }}
                transition={{ duration: 0.6, ease: SALIDA }}
                aria-hidden="true"
              />
            )}
            <span className={estilos.escena} aria-hidden="true">
              <Escena estado={e.id} />
            </span>
            <strong className={estilos.nombre}>{e.nombre}</strong>
            <span className={estilos.texto}>{e.descripcion}</span>
          </motion.button>
        );
      })}
    </motion.div>
  );
}

/** Escenas SVG de 64×64; las animaciones viven en el CSS y se disparan con hover, foco, toque o al entrar. */
function Escena({ estado }: { estado: EstadoAnimo }) {
  switch (estado) {
    case "cansado":
      return (
        <svg viewBox="0 0 64 64" className={estilos.svg}>
          <circle className={`${estilos.estrella} ${estilos.e1}`} cx="14" cy="16" r="1.8" />
          <circle className={`${estilos.estrella} ${estilos.e2}`} cx="50" cy="44" r="1.4" />
          <circle className={`${estilos.estrella} ${estilos.e3}`} cx="12" cy="44" r="1.2" />
          <g className={estilos.luna}>
            <path d="M38 13a19 19 0 1 0 13 31A16 16 0 0 1 38 13Z" />
          </g>
          <text className={`${estilos.zeta} ${estilos.z1}`} x="44" y="22">z</text>
          <text className={`${estilos.zeta} ${estilos.z2}`} x="49" y="15">z</text>
          <text className={`${estilos.zeta} ${estilos.z3}`} x="54" y="9">z</text>
        </svg>
      );
    case "enfocado":
      return (
        <svg viewBox="0 0 64 64" className={estilos.svg}>
          <circle className={estilos.ondaBlanco} cx="30" cy="34" r="22" />
          <g className={estilos.blanco}>
            <circle className={estilos.anillo} cx="30" cy="34" r="20" />
            <circle className={`${estilos.anillo} ${estilos.anilloMedio}`} cx="30" cy="34" r="13" />
            <circle className={estilos.centro} cx="30" cy="34" r="6" />
          </g>
          <g className={estilos.dardo}>
            <path d="M30 34 L52 12" />
            <path className={estilos.plumas} d="M52 12 l6 -1 M52 12 l1 -6 M49 15 l6 -1 M49 15 l1 -6" />
          </g>
        </svg>
      );
    case "energia":
      return (
        <svg viewBox="0 0 64 64" className={estilos.svg}>
          <g className={estilos.chispas}>
            <path className={estilos.chispa} d="M32 6v6" />
            <path className={estilos.chispa} d="M54 20l-5 3" />
            <path className={estilos.chispa} d="M54 46l-5 -3" />
            <path className={estilos.chispa} d="M10 20l5 3" />
            <path className={estilos.chispa} d="M10 46l5 -3" />
            <path className={estilos.chispa} d="M32 58v-6" />
          </g>
          <path className={estilos.rayo} d="M36 10 18 36h12l-4 18 20-28H34l2-16Z" />
        </svg>
      );
    case "social":
      return (
        <svg viewBox="0 0 64 64" className={estilos.svg}>
          <g className={estilos.personaIzq}>
            <circle cx="21" cy="26" r="7" />
            <path d="M8 50c0-8 6-13 13-13s13 5 13 13" />
          </g>
          <g className={estilos.personaDer}>
            <circle cx="43" cy="26" r="7" />
            <path d="M30 50c0-8 6-13 13-13s13 5 13 13" />
          </g>
          <path className={estilos.corazon} d="M32 17c-2-4-8-3-8 1 0 3 4 6 8 9 4-3 8-6 8-9 0-4-6-5-8-1Z" />
        </svg>
      );
  }
}

"use client";

import Link from "next/link";
import { animate, motion, useMotionValue, useReducedMotion, useTransform } from "motion/react";
import { useEffect } from "react";

/** Curva de salida fuerte (la misma que --curva-salida en globals.css). */
export const SALIDA = [0.23, 1, 0.32, 1] as const;

/** Resorte para presiones y toques: responde al instante y se asienta sin rebote visible. */
export const RESORTE_TOQUE = { type: "spring", stiffness: 520, damping: 32, mass: 0.6 } as const;

const contenedor = {
  oculto: {},
  visible: { transition: { staggerChildren: 0.07, delayChildren: 0.05 } },
};

const pieza = {
  oculto: { opacity: 0, y: 16, filter: "blur(4px)" },
  visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.55, ease: SALIDA } },
};

/** Contenedor que hace entrar a sus <Pieza> una tras otra, una sola vez al cargar. */
export function Entrada({ children, className, as = "div" }: { children: React.ReactNode; className?: string; as?: "div" | "main" | "section" }) {
  const reducir = useReducedMotion();
  const Componente = motion[as];
  return (
    <Componente className={className} variants={contenedor} initial={reducir ? false : "oculto"} animate="visible">
      {children}
    </Componente>
  );
}

/** Un bloque que entra subiendo y enfocándose, dentro de <Entrada>. */
export function Pieza({ children, className, as = "div" }: { children: React.ReactNode; className?: string; as?: "div" | "section" | "header" | "li" }) {
  const Componente = motion[as];
  return (
    <Componente className={className} variants={pieza}>
      {children}
    </Componente>
  );
}

/** Número que cuenta hasta su valor (y vuelve a contar cuando cambia). */
export function NumeroAnimado({ valor, duracion = 1.1 }: { valor: number; duracion?: number }) {
  const reducir = useReducedMotion();
  const actual = useMotionValue(reducir ? valor : 0);
  const redondeado = useTransform(actual, (v) => Math.round(v).toString());

  useEffect(() => {
    if (reducir) {
      actual.set(valor);
      return;
    }
    const control = animate(actual, valor, { duration: duracion, ease: SALIDA });
    return () => control.stop();
  }, [valor, duracion, reducir, actual]);

  return <motion.span className="cifras">{redondeado}</motion.span>;
}

/** Link de Next con física de presión. */
export const MotionLink = motion.create(Link);

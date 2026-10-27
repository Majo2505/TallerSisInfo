"use client";

import Link from "next/link";
import { animate, motion, useMotionValue, useReducedMotion, useTransform } from "motion/react";
import { useEffect } from "react";
import estilos from "./Movimiento.module.css";

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

const DIGITOS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

/** Una rueda del odómetro: la tira 0-9 gira con resorte hasta el dígito. */
function Rueda({ digito }: { digito: number }) {
  const reducir = useReducedMotion();
  return (
    <span className={estilos.rueda} aria-hidden="true">
      <motion.span
        className={estilos.tira}
        initial={{ y: reducir ? `${-digito * 10}%` : "0%" }}
        animate={{ y: `${-digito * 10}%` }}
        transition={{ type: "spring", stiffness: 70, damping: 16, mass: 1.1 }}
      >
        {DIGITOS.map((n) => (
          <span key={n}>{n}</span>
        ))}
      </motion.span>
    </span>
  );
}

/** Número como contador mecánico: cada dígito gira hasta su valor. */
export function Odometro({ valor }: { valor: number }) {
  const digitos = String(Math.max(0, Math.round(valor))).split("");
  return (
    <span className={estilos.odometro} aria-label={String(valor)} role="img">
      {digitos.map((d, i) => (
        // La clave cuenta desde la derecha: las unidades siguen siendo la misma rueda aunque cambie la cantidad de dígitos.
        <Rueda key={digitos.length - i} digito={Number(d)} />
      ))}
    </span>
  );
}

/**
 * Escribe la posición del mouse dentro del elemento en --mx / --my (0 a 100 %), para luces que siguen al cursor.
 * Solo con mouse: en pantallas táctiles no hace nada. Escribe directo en el estilo, sin re-renderizar.
 */
export function seguirPuntero(evento: React.PointerEvent<HTMLElement>) {
  if (evento.pointerType !== "mouse") return;
  const caja = evento.currentTarget.getBoundingClientRect();
  evento.currentTarget.style.setProperty("--mx", `${((evento.clientX - caja.left) / caja.width) * 100}%`);
  evento.currentTarget.style.setProperty("--my", `${((evento.clientY - caja.top) / caja.height) * 100}%`);
}

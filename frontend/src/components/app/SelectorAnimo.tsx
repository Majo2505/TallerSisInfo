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
 * Elección del ánimo (HU7): cuatro tarjetas que entran escalonadas. Cada una trae un personaje animado
 * (alguien que se duerme, alguien concentrado, alguien que corre y amigos conversando).
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

/** Cara simple: ojos y sonrisa, reutilizada por los personajes. */
function Cara({ x, y, ojos = "abiertos", boca = "sonrisa" }: { x: number; y: number; ojos?: "abiertos" | "cerrados"; boca?: "sonrisa" | "recta" | "risa" }) {
  return (
    <g>
      {ojos === "abiertos" ? (
        <g className={estilos.ojos}>
          <circle cx={x - 3} cy={y} r="1.15" className={estilos.tinta} />
          <circle cx={x + 3} cy={y} r="1.15" className={estilos.tinta} />
        </g>
      ) : (
        <path d={`M${x - 4.6} ${y} q1.6 1.3 3.2 0 M${x + 1.4} ${y} q1.6 1.3 3.2 0`} className={estilos.linea} />
      )}
      {boca === "sonrisa" && <path d={`M${x - 2} ${y + 4} q2 1.6 4 0`} className={estilos.linea} />}
      {boca === "recta" && <path d={`M${x - 1.6} ${y + 4.4} h3.2`} className={estilos.linea} />}
      {boca === "risa" && <path d={`M${x - 2.4} ${y + 3.6} q2.4 3.4 4.8 0Z`} className={estilos.tinta} />}
    </g>
  );
}

/**
 * Escenas con personajes (120×64). Las animaciones viven en el CSS y se disparan con hover, foco,
 * al elegir o, en pantallas táctiles, solas y más lentas.
 */
function Escena({ estado }: { estado: EstadoAnimo }) {
  switch (estado) {
    case "cansado":
      // Se queda dormido sobre el escritorio: bosteza, cabecea y vuelve a despertar.
      return (
        <svg viewBox="0 0 120 64" className={estilos.svg}>
          <path d="M22 10a6 6 0 1 0 6 9 5 5 0 0 1-6-9Z" className={estilos.decoTinte} />
          <circle className={`${estilos.estrella} ${estilos.e1}`} cx="34" cy="8" r="1.2" />
          <circle className={`${estilos.estrella} ${estilos.e2}`} cx="96" cy="30" r="1" />
          <path d="M44 50q0-15 16-15t16 15Z" className={estilos.ropa} />
          <g className={estilos.cabeceo}>
            <circle cx="60" cy="27" r="10" className={estilos.piel} />
            <path d="M50 26q0-11 10-11t10 11q-3-5-10-5t-10 5Z" className={estilos.pelo} />
            <Cara x={60} y={28.5} ojos="cerrados" boca="recta" />
            <ellipse className={estilos.bostezo} cx="60" cy="33.2" rx="1.6" ry="1.2" />
          </g>
          <rect x="41" y="45" width="17" height="5" rx="2.5" className={estilos.ropa} />
          <rect x="62" y="45" width="17" height="5" rx="2.5" className={estilos.ropa} />
          <rect x="28" y="50" width="64" height="5" rx="2.5" className={estilos.mueble} />
          <rect x="82" y="41" width="8" height="9" rx="2" className={estilos.taza} />
          <text className={`${estilos.zeta} ${estilos.z1}`} x="72" y="20">z</text>
          <text className={`${estilos.zeta} ${estilos.z2}`} x="77" y="13">z</text>
          <text className={`${estilos.zeta} ${estilos.z3}`} x="82" y="7">Z</text>
        </svg>
      );
    case "enfocado":
      // Con audífonos frente a la laptop: teclea, parpadea y la lámpara ilumina.
      return (
        <svg viewBox="0 0 120 64" className={estilos.svg}>
          <path className={estilos.luz} d="M100 30 84 55h24Z" />
          <path d="M92 55h14M99 55l-4-16 8-9" className={estilos.lampara} />
          <path d="M99 24l9 4-4 6Z" className={estilos.pantalla} />
          <g className={estilos.foco}>
            <path d="M60 9V5M50 11l-2.5-3M70 11l2.5-3" className={estilos.lineaTinte} />
          </g>
          <path d="M44 51q0-15 16-15t16 15Z" className={estilos.ropa} />
          <circle cx="60" cy="25" r="10" className={estilos.piel} />
          <path d="M50 24q0-11 10-11t10 11q-4-4-10-4t-10 4Z" className={estilos.pelo} />
          <path d="M49 25a11 11 0 0 1 22 0" className={estilos.auricular} />
          <rect x="46.5" y="21.5" width="4.5" height="8" rx="2" className={estilos.acento} />
          <rect x="69" y="21.5" width="4.5" height="8" rx="2" className={estilos.acento} />
          <path d="M53 22.4l4 1.1M67 22.4l-4 1.1" className={estilos.linea} />
          <Cara x={60} y={27} boca="recta" />
          <rect x="43" y="36" width="34" height="15" rx="2.5" className={estilos.mueble} />
          <circle cx="60" cy="43.5" r="2" className={estilos.acento} />
          <circle className={estilos.manoIzq} cx="49" cy="50" r="3" />
          <circle className={estilos.manoDer} cx="71" cy="50" r="3" />
          <rect x="35" y="50.5" width="50" height="4" rx="2" className={estilos.mueble} />
        </svg>
      );
    case "energia":
      // Sale a correr: brazos y piernas alternan, el cuerpo rebota y vuela una gota de sudor.
      return (
        <svg viewBox="0 0 120 64" className={estilos.svg}>
          <path d="M22 57h76" className={estilos.suelo} />
          <ellipse className={estilos.sombra} cx="60" cy="57" rx="11" ry="2" />
          <g className={estilos.velocidad}>
            <path d="M28 22h10M24 31h12M29 40h8" className={estilos.lineaTinte} />
          </g>
          <g className={estilos.corredor}>
            <path className={`${estilos.pierna} ${estilos.piernaAtras}`} d="M58 40l-6 8-7 2" />
            <path className={`${estilos.brazo} ${estilos.brazoAtras}`} d="M61 26l-7 5-4-3" />
            <path className={estilos.torso} d="M62 24l-4 16" />
            <path className={`${estilos.pierna} ${estilos.piernaDelante}`} d="M58 40l8 7-2 8" />
            <path className={`${estilos.brazo} ${estilos.brazoDelante}`} d="M61 26l7 6 6-4" />
            <circle cx="64" cy="16" r="7" className={estilos.piel} />
            <path d="M57 15q1-8 8-8 6 0 7 6-4-2-8-1t-7 3Z" className={estilos.pelo} />
            <path d="M57.4 13.6q7-3.2 14-1" className={estilos.vincha} />
            <circle cx="67" cy="16.5" r="1.1" className={estilos.tinta} />
            <path d="M66 19.6q1.6 1.2 3.2 0" className={estilos.linea} />
            <path className={estilos.gota} d="M55 13q-2 3 0 4 2-1 0-4Z" />
          </g>
        </svg>
      );
    case "social":
      // Tres amigos en una mesa: uno cuenta algo, otro asiente y el tercero se ríe.
      return (
        <svg viewBox="0 0 120 64" className={estilos.svg}>
          <g className={estilos.burbuja1}>
            <rect x="14" y="3" width="22" height="12" rx="6" className={estilos.burbuja} />
            <path d="M23 15l-2 4 6-4Z" className={estilos.burbuja} />
            <circle cx="20" cy="9" r="1.3" className={estilos.puntoTinte} />
            <circle cx="25" cy="9" r="1.3" className={estilos.puntoTinte} />
            <circle cx="30" cy="9" r="1.3" className={estilos.puntoTinte} />
          </g>
          <g className={estilos.burbuja2}>
            <rect x="84" y="2" width="24" height="12" rx="6" className={estilos.burbuja} />
            <path d="M91 14l-1 4 5-4Z" className={estilos.burbuja} />
            <text x="96" y="10.4" className={estilos.jaja}>jaja</text>
          </g>
          <path d="M22 47q0-11 11-11t11 11Z" className={estilos.ropa} />
          <path d="M49 47q0-12 11-12t11 12Z" className={estilos.ropaB} />
          <path d="M76 47q0-11 11-11t11 11Z" className={estilos.ropaC} />
          <g className={estilos.cabezaA}>
            <circle cx="33" cy="28" r="7" className={estilos.piel} />
            <path d="M26 27q0-8 7-8t7 8q-3-3-7-3t-7 3Z" className={estilos.pelo} />
            <Cara x={33} y={29} />
          </g>
          <g className={estilos.cabezaB}>
            <circle cx="60" cy="25" r="7.5" className={estilos.piel} />
            <path d="M52.5 24q0-8.5 7.5-8.5t7.5 8.5l-1 6q1-7-6.5-9.5-7 2.5-6.5 9.5Z" className={estilos.peloB} />
            <Cara x={60} y={26.5} />
          </g>
          <g className={estilos.cabezaC}>
            <circle cx="87" cy="28" r="7" className={estilos.piel} />
            <circle cx="82" cy="22" r="3" className={estilos.pelo} />
            <circle cx="87" cy="20.5" r="3.2" className={estilos.pelo} />
            <circle cx="92" cy="22" r="3" className={estilos.pelo} />
            <Cara x={87} y={29} boca="risa" />
          </g>
          <rect x="28" y="46" width="64" height="4" rx="2" className={estilos.mueble} />
          <rect x="34" y="50" width="3" height="9" rx="1.5" className={estilos.mueble} />
          <rect x="83" y="50" width="3" height="9" rx="1.5" className={estilos.mueble} />
          <rect x="45" y="40" width="6" height="6" rx="1.5" className={estilos.taza} />
          <rect x="70" y="40" width="6" height="6" rx="1.5" className={estilos.acento} />
        </svg>
      );
  }
}

"use client";

import { useState } from "react";
import { motion, useReducedMotion } from "motion/react";
import { ESTADOS_ANIMO, EstadoAnimo } from "@/lib/maqueta";
import { SALIDA, seguirPuntero } from "./Movimiento";
import estilos from "./SelectorAnimo.module.css";

const PAUSA_ELECCION_MS = 420;

type Onda = { x: number; y: number };

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

/*
 * Escenas de noche en azul marino con luz amarilla, en el mismo tono que la foto de la portada: objetos y siluetas,
 * sin caras. El movimiento es lento (vapor, luz, una página, una pelota) y se apaga con «reducir movimiento».
 */
function Escena({ estado }: { estado: EstadoAnimo }) {
  switch (estado) {
    case "cansado":
      return (
        <svg viewBox="0 0 160 80" preserveAspectRatio="xMidYMid slice" className={estilos.svg}>
          <circle cx="122" cy="22" r="22" className={estilos.halo} />
          <path d="M126 10a12 12 0 1 0 8 20 10 10 0 0 1-8-20Z" className={`${estilos.amarillo} ${estilos.luna}`} />
          <circle cx="28" cy="14" r="1.3" className={`${estilos.estrella} ${estilos.d1}`} />
          <circle cx="58" cy="9" r="1" className={`${estilos.estrella} ${estilos.d2}`} />
          <circle cx="92" cy="18" r="1.2" className={`${estilos.estrella} ${estilos.d3}`} />
          <path d="M0 66 Q40 52 80 60 T160 56 V80 H0Z" className={estilos.loma} />
          <path d="M36 58h44M40 58v10M76 58v10M36 52h44" className={estilos.banca} />
          <rect x="92" y="54" width="10" height="11" rx="2.5" className={estilos.taza} />
          <path d="M102 57.5h2.5a2.5 2.5 0 0 1 0 5H102" className={estilos.asa} />
          <path d="M95 50q-2-3 0-6t0-6" className={`${estilos.vapor} ${estilos.d1}`} />
          <path d="M99 50q-2-3 0-6t0-6" className={`${estilos.vapor} ${estilos.d2}`} />
        </svg>
      );
    case "enfocado":
      return (
        <svg viewBox="0 0 160 80" preserveAspectRatio="xMidYMid slice" className={estilos.svg}>
          <path d="M40 30 H58 L84 70 H16Z" className={estilos.cono} />
          <path d="M26 70V46l23-20" className={estilos.brazoLampara} />
          <path d="M40 30a9 8 0 0 1 18 0Z" className={estilos.amarillo} />
          <rect x="0" y="70" width="160" height="10" className={estilos.mesa} />
          <path d="M84 68 L60 64 V48 L84 52Z" className={estilos.pagina} />
          <path d="M84 68 L108 64 V48 L84 52Z" className={estilos.pagina} />
          <path d="M84 52 L108 48 V64 L84 68Z" className={`${estilos.paginaGira} ${estilos.pagina}`} />
          <path d="M64 55l14 2M64 59l14 2M90 56l14-2M90 60l14-2" className={estilos.renglon} />
          <rect x="116" y="62" width="30" height="8" rx="2" className={estilos.libros} />
          <rect x="119" y="54" width="25" height="8" rx="2" className={estilos.librosB} />
          <rect x="117" y="46" width="27" height="8" rx="2" className={estilos.libros} />
          <circle cx="130" cy="20" r="1.2" className={`${estilos.estrella} ${estilos.d2}`} />
          <circle cx="148" cy="30" r="1" className={`${estilos.estrella} ${estilos.d3}`} />
        </svg>
      );
    case "energia":
      return (
        <svg viewBox="0 0 160 80" preserveAspectRatio="xMidYMid slice" className={estilos.svg}>
          <path d="M0 80 L62 46 H98 L160 80Z" className={estilos.cancha} />
          <path d="M80 46V80M40 80 L70 46M120 80 L90 46" className={estilos.carril} />
          <ellipse cx="112" cy="66" rx="10" ry="2.6" className={estilos.sombraPelota} />
          <g className={estilos.pelota}>
            <circle cx="112" cy="40" r="10" className={estilos.amarillo} />
            <path d="M103 36q9 4 18 0M104 45q8-4 16 0" className={estilos.costura} />
          </g>
          <circle cx="140" cy="14" r="1.2" className={`${estilos.estrella} ${estilos.d1}`} />
        </svg>
      );
    case "social":
      return (
        <svg viewBox="0 0 160 80" preserveAspectRatio="xMidYMid slice" className={estilos.svg}>
          <g className={`${estilos.globo} ${estilos.g1}`}>
            <rect x="26" y="10" width="44" height="20" rx="10" className={estilos.globoBlanco} />
            <path d="M38 30l-3 6 9-6Z" className={estilos.globoBlanco} />
            <circle cx="40" cy="20" r="2" className={estilos.punto} />
            <circle cx="48" cy="20" r="2" className={estilos.punto} />
            <circle cx="56" cy="20" r="2" className={estilos.punto} />
          </g>
          <g className={`${estilos.globo} ${estilos.g2}`}>
            <rect x="88" y="8" width="46" height="20" rx="10" className={estilos.amarillo} />
            <path d="M120 28l4 6-10-6Z" className={estilos.amarillo} />
            <path d="M98 18h26" className={estilos.lineaGlobo} />
          </g>
          <rect x="0" y="66" width="160" height="14" className={estilos.mesa} />
          <rect x="54" y="50" width="14" height="16" rx="3" className={estilos.taza} />
          <path d="M68 54h3a3 3 0 0 1 0 6h-3" className={estilos.asa} />
          <rect x="92" y="50" width="14" height="16" rx="3" className={estilos.tazaAmarilla} />
          <path d="M92 54h-3a3 3 0 0 0 0 6h3" className={estilos.asaAmarilla} />
          <path d="M59 46q-2-3 0-6t0-6" className={`${estilos.vapor} ${estilos.d1}`} />
          <path d="M99 46q-2-3 0-6t0-6" className={`${estilos.vapor} ${estilos.d2}`} />
        </svg>
      );
  }
}

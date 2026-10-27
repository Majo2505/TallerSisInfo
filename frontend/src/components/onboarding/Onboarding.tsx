"use client";

import { useRouter } from "next/navigation";
import { AnimatePresence, motion, useReducedMotion, type PanInfo } from "motion/react";
import { useCallback, useEffect, useState, useSyncExternalStore } from "react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoFlechaDerecha } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { useSesion } from "@/lib/useSesion";
import { Icono, Informacion } from "./Iconos";
import styles from "./Onboarding.module.css";
import { PASOS } from "./pasos";

// Se recuerda solo durante la pestaña: cada visita nueva sin sesión vuelve a mostrar el onboarding.
const CLAVE_VISTO = "breaku_onboarding_visto";
const UMBRAL_SWIPE = 60; // píxeles: o una pasada larga...
const UMBRAL_VELOCIDAD = 400; // ...o un toque rápido (px/s), aunque sea corto

function yaFueVisto(): boolean {
  try {
    return window.sessionStorage.getItem(CLAVE_VISTO) !== null;
  } catch {
    return false;
  }
}

const sinSuscripcion = () => () => {};

function marcarVisto() {
  try {
    window.sessionStorage.setItem(CLAVE_VISTO, "1");
  } catch {
    // Sin sessionStorage (modo privado estricto): se mostrará de nuevo al volver a "/".
  }
}

type Props = {
  /** Ignora la marca de "ya visto" (útil en desarrollo con ?onboarding=1). */
  forzar?: boolean;
};

export default function Onboarding({ forzar = false }: Props) {
  const router = useRouter();
  const reducir = useReducedMotion();
  const [[indice, direccion], setPaso] = useState<[number, number]>([0, 1]);
  // null en el servidor y durante la hidratación: aún no se sabe si ya lo vio.
  const visto = useSyncExternalStore<boolean | null>(sinSuscripcion, yaFueVisto, () => null);
  const { sesion, listo } = useSesion();
  // Con la sesión abierta no hace falta el onboarding: se va directo a Inicio.
  const verificando = !listo || !!sesion || (!forzar && (visto === null || visto));

  const paso = PASOS[indice];
  const esUltimo = indice === PASOS.length - 1;

  useEffect(() => {
    if (!listo) return;
    if (sesion) router.replace("/inicio");
    else if (!forzar && visto) router.replace("/login");
  }, [listo, sesion, forzar, visto, router]);

  const terminar = useCallback(() => {
    marcarVisto();
    router.push("/login");
  }, [router]);

  const irA = useCallback((nuevo: number) => {
    setPaso(([actual]) => [Math.min(Math.max(nuevo, 0), PASOS.length - 1), nuevo > actual ? 1 : -1]);
  }, []);

  const siguiente = useCallback(() => {
    if (esUltimo) terminar();
    else irA(indice + 1);
  }, [esUltimo, terminar, irA, indice]);

  const anterior = useCallback(() => irA(indice - 1), [irA, indice]);

  useEffect(() => {
    if (verificando) return;
    function alPresionarTecla(evento: KeyboardEvent) {
      if (evento.key === "ArrowRight") siguiente();
      if (evento.key === "ArrowLeft") anterior();
    }
    window.addEventListener("keydown", alPresionarTecla);
    return () => window.removeEventListener("keydown", alPresionarTecla);
  }, [verificando, siguiente, anterior]);

  // Deslizar: cuenta la distancia o la velocidad, así un toque rápido alcanza.
  function alSoltar(_: unknown, info: PanInfo) {
    if (info.offset.x < -UMBRAL_SWIPE || info.velocity.x < -UMBRAL_VELOCIDAD) siguiente();
    else if (info.offset.x > UMBRAL_SWIPE || info.velocity.x > UMBRAL_VELOCIDAD) anterior();
  }

  const variantes = {
    entra: (dir: number) => ({ opacity: 0, x: reducir ? 0 : dir * 60, filter: "blur(8px)" }),
    centro: { opacity: 1, x: 0, filter: "blur(0px)" },
    sale: (dir: number) => ({ opacity: 0, x: reducir ? 0 : dir * -60, filter: "blur(8px)" }),
  };

  return (
    <div className={styles.pantalla} style={{ "--brillo": paso.brillo } as React.CSSProperties}>
      <FondoVivo />
      {!verificando && (
        <main className={styles.contenedor}>
          <header className={styles.cabecera}>
            <span className={styles.marca}>
              Break<span>U</span>
            </span>
            <button type="button" className={styles.saltar} onClick={terminar}>
              Saltar
            </button>
          </header>

          <AnimatePresence mode="wait" custom={direccion} initial={false}>
            <motion.section
              key={indice}
              className={styles.contenido}
              aria-live="polite"
              custom={direccion}
              variants={variantes}
              initial="entra"
              animate="centro"
              exit="sale"
              transition={{ duration: 0.38, ease: SALIDA }}
              drag={reducir ? false : "x"}
              dragConstraints={{ left: 0, right: 0 }}
              dragElastic={0.18}
              onDragEnd={alSoltar}
            >
              {/* Ilustración: el ícono dentro de anillos que laten, con un punto en órbita. */}
              <div className={styles.ilustracion} aria-hidden="true">
                <span className={styles.anillo1} />
                <span className={styles.anillo2} />
                <span className={styles.orbita}>
                  <span className={styles.satelite} />
                </span>
                <motion.span
                  className={styles.icono}
                  initial={reducir ? false : { scale: 0.8, rotate: -12 }}
                  animate={{ scale: 1, rotate: 0 }}
                  transition={{ type: "spring", bounce: 0.45, duration: 0.7, delay: 0.1 }}
                >
                  <Icono nombre={paso.icono} tamano={44} />
                </motion.span>
              </div>

              <h1 className={styles.titulo}>{paso.titulo}</h1>
              <p className={styles.texto}>{paso.texto}</p>
              <p className={styles.nota}>
                <Informacion />
                <span>{paso.nota}</span>
              </p>
            </motion.section>
          </AnimatePresence>

          <footer className={styles.pie}>
            <div className={styles.puntos}>
              {PASOS.map((_, i) => (
                <button
                  key={i}
                  type="button"
                  className={styles.punto}
                  aria-label={`Paso ${i + 1} de ${PASOS.length}`}
                  aria-current={i === indice ? "step" : undefined}
                  onClick={() => irA(i)}
                >
                  {i === indice && <motion.span layoutId="punto-activo" className={styles.puntoActivo} transition={RESORTE_TOQUE} />}
                </button>
              ))}
            </div>
            <motion.button
              type="button"
              className={styles.boton}
              onClick={siguiente}
              whileTap={{ scale: 0.97 }}
              transition={RESORTE_TOQUE}
            >
              <span>{paso.boton}</span>
              <span className={styles.botonFlecha}>
                <IconoFlechaDerecha tamano={18} />
              </span>
            </motion.button>
          </footer>
        </main>
      )}
    </div>
  );
}

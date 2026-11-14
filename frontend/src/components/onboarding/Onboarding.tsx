"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { AnimatePresence, motion, useReducedMotion, type PanInfo } from "motion/react";
import { useCallback, useEffect, useState, useSyncExternalStore } from "react";
import { IconoFlechaDerecha } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { useSesion } from "@/lib/useSesion";
import { rutaDeEntrada } from "@/services/authService";
import fotoEstudiantes from "./fotos/estudiantes.webp";
import fotoValeria from "./fotos/valeria.webp";
import { Icono } from "./Iconos";
import styles from "./Onboarding.module.css";
import { PASOS } from "./pasos";

const CLAVE_VISTO = "breaku_onboarding_visto";
const UMBRAL_SWIPE = 60;
const UMBRAL_VELOCIDAD = 400;

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
  }
}

type Props = {
  forzar?: boolean;
};

export default function Onboarding({ forzar = false }: Props) {
  const router = useRouter();
  const reducir = useReducedMotion();
  const [[indice, direccion], setPaso] = useState<[number, number]>([0, 1]);
  const visto = useSyncExternalStore<boolean | null>(sinSuscripcion, yaFueVisto, () => null);
  const { sesion, listo } = useSesion();
  const verificando = !listo || !!sesion || (!forzar && (visto === null || visto));

  const paso = PASOS[indice];
  const esUltimo = indice === PASOS.length - 1;

  useEffect(() => {
    if (!listo) return;
    if (sesion) router.replace(rutaDeEntrada(sesion));
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

  function alSoltar(_: unknown, info: PanInfo) {
    if (info.offset.x < -UMBRAL_SWIPE || info.velocity.x < -UMBRAL_VELOCIDAD) siguiente();
    else if (info.offset.x > UMBRAL_SWIPE || info.velocity.x > UMBRAL_VELOCIDAD) anterior();
  }

  const variantes = {
    entra: (dir: number) => ({ opacity: 0, x: reducir ? 0 : dir * 40, filter: "blur(6px)" }),
    centro: { opacity: 1, x: 0, filter: "blur(0px)" },
    sale: (dir: number) => ({ opacity: 0, x: reducir ? 0 : dir * -40, filter: "blur(6px)" }),
  };

  return (
    <div className={styles.pantalla}>
      {/* La foto y el velo se pintan aunque se esté verificando la sesión: así no hay pantalla vacía. */}
      <motion.div
        className={styles.foto}
        initial={reducir ? false : { scale: 1.08, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        transition={{ duration: 1.6, ease: SALIDA }}
        aria-hidden="true"
      >
        <Image
          src={fotoEstudiantes}
          alt=""
          fill
          placeholder="blur"
          fetchPriority="high"
          sizes="100vw"
          className={styles.fotoImagen}
        />
      </motion.div>
      <div className={styles.velo} aria-hidden="true" />

      {!verificando && (
        <div className={styles.contenedor}>
          <header className={styles.cabecera}>
            <span className={styles.marca}>
              Break<span>U</span>
            </span>

            <nav className={styles.pasos} aria-label="Pasos de la introducción">
              {PASOS.map((p, i) => (
                <button
                  key={p.etiqueta}
                  type="button"
                  className={styles.pasoEnlace}
                  aria-current={i === indice ? "step" : undefined}
                  onClick={() => irA(i)}
                >
                  {p.etiqueta}
                  {i === indice && (
                    <motion.span layoutId="paso-activo" className={styles.pasoSubrayado} transition={RESORTE_TOQUE} />
                  )}
                </button>
              ))}
            </nav>

            <button type="button" className={styles.entrar} onClick={terminar} aria-label="Iniciar sesión">
              <span>Iniciar sesión</span>
              <IconoFlechaDerecha tamano={16} />
            </button>
          </header>

          {/* En el celular, la barra de pasos se vuelve una línea de progreso. */}
          <div
            className={styles.progreso}
            role="progressbar"
            aria-label="Progreso de la introducción"
            aria-valuemin={1}
            aria-valuemax={PASOS.length}
            aria-valuenow={indice + 1}
            aria-valuetext={`Paso ${indice + 1} de ${PASOS.length}: ${paso.etiqueta}`}
          >
            {PASOS.map((p, i) => (
              <button
                key={p.etiqueta}
                type="button"
                className={styles.segmento}
                data-lleno={i <= indice || undefined}
                aria-label={`Ir al paso ${i + 1}: ${p.etiqueta}`}
                onClick={() => irA(i)}
              />
            ))}
          </div>

          <main className={styles.principal}>
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
                transition={{ duration: 0.34, ease: SALIDA }}
                drag={reducir ? false : "x"}
                dragConstraints={{ left: 0, right: 0 }}
                dragElastic={0.14}
                onDragEnd={alSoltar}
              >
                <h1 className={styles.titulo}>
                  {paso.titulo[0]}
                  <span className={styles.acento}>{paso.titulo[1]}</span>
                  {paso.titulo[2]}
                </h1>
                <p className={styles.texto}>{paso.texto}</p>
                <p className={styles.nota}>
                  <Icono nombre={paso.icono} tamano={18} />
                  <span>{paso.nota}</span>
                </p>
              </motion.section>
            </AnimatePresence>

            <div className={styles.acciones}>
              <motion.button
                type="button"
                className={styles.primario}
                onClick={siguiente}
                whileTap={{ scale: 0.97 }}
                transition={RESORTE_TOQUE}
              >
                <span>{paso.boton}</span>
                <IconoFlechaDerecha tamano={18} />
              </motion.button>
              {indice > 0 ? (
                <button type="button" className={styles.secundario} onClick={anterior}>
                  Atrás
                </button>
              ) : (
                <button type="button" className={styles.secundario} onClick={terminar}>
                  <span>Ya tengo cuenta</span>
                  <IconoFlechaDerecha tamano={16} />
                </button>
              )}
            </div>
          </main>

          <motion.figure
            className={styles.testimonio}
            initial={reducir ? false : { opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, ease: SALIDA, delay: 0.5 }}
          >
            <figcaption className={styles.autora}>
              <Image src={fotoValeria} alt="" width={48} height={48} className={styles.avatar} />
              <span>
                <strong>Valeria Quispe</strong>
                <small>Ing. de Sistemas · 5.º semestre</small>
              </span>
            </figcaption>
            <blockquote className={styles.cita}>
              «Los martes tenía dos horas muertas y siempre terminaba en el celular. Ahora BreakU me avisa cuando empieza
              mi puente y ya sé a dónde ir: la biblio para repasar o la cafetería con mis amigas.»
            </blockquote>
          </motion.figure>
        </div>
      )}
    </div>
  );
}

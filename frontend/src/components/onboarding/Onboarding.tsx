"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { motion, useReducedMotion } from "motion/react";
import { useEffect } from "react";
import { IconoFlechaDerecha } from "@/components/app/Iconos";
import { SALIDA } from "@/components/app/Movimiento";
import { useSesion } from "@/lib/useSesion";
import { rutaDeEntrada } from "@/services/authService";
import fotoEstudiantes from "./fotos/estudiantes.webp";
import fotoValeria from "./fotos/valeria.webp";
import { Estrellas } from "./Iconos";
import Navegacion from "./Navegacion";
import styles from "./Onboarding.module.css";
import PiePagina from "./PiePagina";
import { ComoFunciona, Funciones, Llamado, Opiniones, Promesas } from "./Secciones";

/** Portada pública de BreakU: lo primero que ve quien todavía no inició sesión. */
export default function Onboarding() {
  const router = useRouter();
  const reducir = useReducedMotion();
  const { sesion, listo } = useSesion();
  const verificando = !listo || !!sesion;

  useEffect(() => {
    if (listo && sesion) router.replace(rutaDeEntrada(sesion));
  }, [listo, sesion, router]);

  const aparece = (retraso: number) =>
    reducir
      ? {}
      : {
          initial: { opacity: 0, y: 18, filter: "blur(6px)" },
          animate: { opacity: 1, y: 0, filter: "blur(0px)" },
          transition: { duration: 0.8, ease: SALIDA, delay: retraso },
        };

  return (
    <div className={styles.pagina}>
      {!verificando && <Navegacion sobreFoto />}
      <main className={styles.principal}>
        <section className={styles.portada} aria-labelledby="titulo-portada">
          {/* La foto se pinta aunque se esté verificando la sesión: así no hay pantalla vacía. */}
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
            <div className={styles.portadaContenido}>
              <div className={styles.mensaje}>
                <motion.h1 id="titulo-portada" className={styles.titulo} {...aparece(0.15)}>
                  Ese hueco entre clases ahora es <span className={styles.acento}>tuyo</span>.
                </motion.h1>
                <motion.p className={styles.bajada} {...aparece(0.28)}>
                  BreakU encuentra los ratos libres de tu horario y te propone qué hacer con ellos en el campus: comer
                  sin correr, repasar antes del parcial o armar un partido con tu curso.
                </motion.p>
                <motion.div className={styles.botones} {...aparece(0.4)}>
                  <Link href="/login?modo=registro" className={styles.primario}>
                    <span>Empezar ahora</span>
                    <IconoFlechaDerecha tamano={18} />
                  </Link>
                  <Link href="#como-funciona" className={styles.secundario}>
                    Ver cómo funciona
                  </Link>
                </motion.div>
              </div>

              <motion.figure className={styles.testimonio} {...aparece(0.6)}>
                <div className={styles.testimonioCabeza}>
                  <Image src={fotoValeria} alt="" width={48} height={48} className={styles.avatar} />
                  <figcaption>
                    <strong>Valeria Quispe</strong>
                    <small>Ing. de Sistemas · 5.º semestre</small>
                  </figcaption>
                </div>
                <span className={styles.puntaje} role="img" aria-label="5 de 5 estrellas">
                  <Estrellas tamano={16} />
                </span>
                <blockquote className={styles.cita}>
                  «Los martes tenía dos horas muertas y siempre terminaba en el celular. Ahora BreakU me avisa cuando
                  empieza mi puente y ya sé a dónde ir: la biblio para repasar o la cafetería con mis amigas.»
                </blockquote>
              </motion.figure>
            </div>
          )}
        </section>

        {!verificando && (
          <>
            <Promesas />
            <ComoFunciona />
            <Funciones />
            <Opiniones />
            <Llamado />
          </>
        )}
      </main>
      {!verificando && <PiePagina />}
    </div>
  );
}

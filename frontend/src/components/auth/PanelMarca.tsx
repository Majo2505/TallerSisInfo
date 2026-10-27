"use client";

import { motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import { Odometro, SALIDA } from "@/components/app/Movimiento";
import { Candado, Logo, Personas, Rayo } from "./Iconos";
import styles from "./PanelMarca.module.css";

const BENEFICIOS = [
  {
    icono: <Rayo />,
    titulo: "Detecta tus puentes",
    texto: "Identifica bloques de tiempo libre entre tus clases automáticamente.",
  },
  {
    icono: <Candado />,
    titulo: "Lugares que existen de verdad",
    texto: "Solo te sugerimos lugares del catálogo del campus, revisados por la universidad.",
  },
  {
    icono: <Personas />,
    titulo: "Comunidades efímeras",
    texto: "Conecta con compañeros que tienen el mismo tiempo libre.",
  },
];

/**
 * Lado de la marca: azul marino con fondo vivo. En laptop muestra la app funcionando (una mini tarjeta de puente)
 * y los beneficios; en celular queda solo la franja con la marca y el lema, para dejar el formulario a la vista.
 */
export default function PanelMarca() {
  const reducir = useReducedMotion();
  const entrar = (retraso: number) => ({
    initial: reducir ? false : { opacity: 0, y: 14, filter: "blur(6px)" },
    animate: { opacity: 1, y: 0, filter: "blur(0px)" },
    transition: { duration: 0.6, ease: SALIDA, delay: retraso },
  });

  return (
    <aside className={styles.panel}>
      <FondoVivo />

      <div className={styles.marca}>
        <div className={styles.logo}>
          <Logo tamano={22} />
        </div>
        <span className={styles.nombre}>
          Break<span className={styles.u}>U</span>
        </span>
      </div>

      <motion.h1 className={styles.lema} {...entrar(0.1)}>
        Tu tiempo libre,{" "}
        <span className={styles.resaltado}>
          <motion.span
            className={styles.trazo}
            initial={reducir ? false : { scaleX: 0 }}
            animate={{ scaleX: 1 }}
            transition={{ duration: 0.8, ease: SALIDA, delay: 0.6 }}
          />
          <span className={styles.resaltadoTexto}>bien aprovechado.</span>
        </span>
      </motion.h1>

      {/* La app funcionando: un puente de ejemplo que flota. */}
      <motion.div className={styles.demo} aria-hidden="true" {...entrar(0.35)}>
        <span className={styles.demoEstado}>
          <span className={styles.demoPunto} /> Puente activo
        </span>
        <p className={styles.demoMinutos}>
          <Odometro valor={45} />
          <span>min libres</span>
        </p>
        <p className={styles.demoSiguiente}>Después: Física II, B-204</p>
        <span className={styles.demoBoton}>¿Qué hago con mi tiempo?</span>
      </motion.div>

      <ul className={styles.beneficios}>
        {BENEFICIOS.map((b, i) => (
          <motion.li key={b.titulo} className={styles.beneficio} {...entrar(0.55 + i * 0.08)}>
            <span className={styles.icono}>{b.icono}</span>
            <div>
              <strong>{b.titulo}</strong>
              <p>{b.texto}</p>
            </div>
          </motion.li>
        ))}
      </ul>

      <p className={styles.pie}>© 2026 BreakU. Solo correos institucionales verificados.</p>
    </aside>
  );
}

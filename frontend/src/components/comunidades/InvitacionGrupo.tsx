"use client";

import Link from "next/link";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { IconoAjustes, IconoPersonas, IconoReloj } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { Invitacion } from "@/lib/comunidades";
import { iniciales } from "@/lib/iniciales";
import estilos from "./InvitacionGrupo.module.css";

type Props = {
  invitacion: Invitacion;
  buscando: boolean;
  /** Vuelve a buscar compañeros libres (SIN_COINCIDENCIAS o ERROR). */
  onBuscar: () => void;
  /** NO_VISIBLE: acepta aparecer en el matching (HU12) y vuelve a buscar. */
  onAceptarYBuscar: () => void;
};

/**
 * HU13: al crear la actividad se invita automáticamente a los compañeros libres a esa hora. Muestra a quiénes se invitó
 * (ya avisados) o qué falta para poder invitarlos.
 */
export default function InvitacionGrupo({ invitacion, buscando, onBuscar, onAceptarYBuscar }: Props) {
  const reducir = useReducedMotion();
  const entrada = reducir ? false : { opacity: 0, transform: "translateY(8px)" };
  const visible = { opacity: 1, transform: "translateY(0px)" };
  const { estado, mensaje, invitados } = invitacion;

  return (
    <AnimatePresence mode="wait" initial={false}>
      {estado === "INVITADOS" ? (
        <motion.div key="invitados" className={estilos.bloque} initial={entrada} animate={visible} transition={{ duration: 0.3, ease: SALIDA }}>
          <p className={estilos.mensaje} role="status">
            {mensaje}
          </p>
          <motion.ul
            className={estilos.invitados}
            initial={reducir ? false : "oculto"}
            animate="visible"
            variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.07, delayChildren: 0.1 } } }}
          >
            {invitados.map((i) => (
              <motion.li
                key={`${i.nombre}-${i.libreDesde}`}
                className={estilos.invitado}
                variants={{
                  oculto: { opacity: 0, transform: "translateY(10px) scale(0.97)" },
                  visible: { opacity: 1, transform: "translateY(0px) scale(1)", transition: { duration: 0.35, ease: SALIDA } },
                }}
              >
                <span className={estilos.avatar} aria-hidden="true">
                  {iniciales(i.nombre)}
                </span>
                <span className={estilos.quien}>
                  <strong>{i.nombre}</strong>
                  {i.carrera && <small title={i.carrera}>{i.carrera}</small>}
                </span>
                <span className={estilos.libre}>
                  <IconoReloj tamano={14} />
                  {i.libreDesde}–{i.libreHasta}
                </span>
              </motion.li>
            ))}
          </motion.ul>
          <p className={estilos.pie}>Les llegó una invitación. Tú ya estás dentro.</p>
        </motion.div>
      ) : estado === "SIN_ZONA" ? (
        <motion.div key="zona" className={estilos.bloque} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          <p className={estilos.texto}>{mensaje}</p>
          <Link href="/configuracion/ubicacion" className={estilos.secundario}>
            <IconoAjustes tamano={16} /> Elegir mi campus
          </Link>
        </motion.div>
      ) : estado === "NO_VISIBLE" ? (
        <motion.div key="visible" className={estilos.bloque} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          <p className={estilos.texto}>{mensaje}</p>
          <small className={estilos.privacidad}>
            Tus compañeros de campus podrán ver cuándo estás libre. Puedes quitarlo cuando quieras en «Libres contigo».
          </small>
          <motion.button
            type="button"
            className={estilos.primario}
            onClick={onAceptarYBuscar}
            disabled={buscando}
            whileTap={{ scale: 0.97 }}
            transition={RESORTE_TOQUE}
          >
            {buscando ? <Buscando /> : <><IconoPersonas tamano={18} /> Aceptar e invitar</>}
          </motion.button>
        </motion.div>
      ) : estado === "COMPLETA" ? (
        <motion.div key="completa" className={estilos.bloque} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          <p className={estilos.texto}>{mensaje}</p>
        </motion.div>
      ) : (
        // SIN_COINCIDENCIAS o ERROR: la actividad ya existe; se puede volver a buscar más tarde.
        <motion.div key="nadie" className={estilos.bloque} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          <p className={estilos.nadie} role="status">
            {mensaje}
          </p>
          <motion.button
            type="button"
            className={estilos.primario}
            onClick={onBuscar}
            disabled={buscando}
            whileTap={{ scale: 0.97 }}
            transition={RESORTE_TOQUE}
          >
            {buscando ? <Buscando /> : <><IconoPersonas tamano={18} /> Volver a buscar</>}
          </motion.button>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

function Buscando() {
  return (
    <>
      <span className={estilos.radar} aria-hidden="true" /> Buscando compañeros libres…
    </>
  );
}

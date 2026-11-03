"use client";

import Link from "next/link";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { IconoAjustes, IconoPersonas, IconoReloj } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { ResultadoComunidad } from "@/lib/comunidades";
import { iniciales } from "@/lib/iniciales";
import estilos from "./CrearGrupo.module.css";

type Props = {
  resultado: ResultadoComunidad | null;
  creando: boolean;
  onCrear: () => void;
  /** NO_VISIBLE: acepta aparecer en el matching (HU12) y vuelve a intentar. */
  onAceptarYCrear: () => void;
};

/**
 * HU13: con la actividad completa, busca a quién le coincide el tiempo libre y arma el grupo solo si hay alguien.
 * Muestra a los invitados (ya avisados) o qué falta para poder crearlo.
 */
export default function CrearGrupo({ resultado, creando, onCrear, onAceptarYCrear }: Props) {
  const reducir = useReducedMotion();
  const entrada = reducir ? false : { opacity: 0, transform: "translateY(8px)" };
  const visible = { opacity: 1, transform: "translateY(0px)" };

  return (
    <AnimatePresence mode="wait" initial={false}>
      {resultado?.estado === "CREADA" && resultado.comunidad ? (
        <motion.div key="creada" className={estilos.creada} initial={entrada} animate={visible} transition={{ duration: 0.3, ease: SALIDA }}>
          {/* El título de la tarjeta ya dice «¡Grupo armado!»: aquí solo a cuántos se invitó. */}
          <p className={estilos.mensaje} role="status">
            {resultado.comunidad.invitados.length === 1
              ? "Invitamos a 1 compañero libre a esa hora."
              : `Invitamos a ${resultado.comunidad.invitados.length} compañeros libres a esa hora.`}
          </p>
          <motion.ul
            className={estilos.invitados}
            initial={reducir ? false : "oculto"}
            animate="visible"
            variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.07, delayChildren: 0.1 } } }}
          >
            {resultado.comunidad.invitados.map((i) => (
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
      ) : resultado?.estado === "SIN_ZONA" ? (
        <motion.div key="zona" className={estilos.falta} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          <p>{resultado.mensaje}</p>
          <Link href="/configuracion/ubicacion" className={estilos.secundario}>
            <IconoAjustes tamano={16} /> Elegir mi campus
          </Link>
        </motion.div>
      ) : resultado?.estado === "NO_VISIBLE" ? (
        <motion.div key="visible" className={estilos.falta} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          <p>{resultado.mensaje}</p>
          <small>Tus compañeros de campus podrán ver cuándo estás libre. Puedes quitarlo cuando quieras en «Libres contigo».</small>
          <motion.button type="button" className={estilos.primario} onClick={onAceptarYCrear} disabled={creando} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
            {creando ? "Creando…" : "Aceptar y crear el grupo"}
          </motion.button>
        </motion.div>
      ) : (
        <motion.div key="crear" className={estilos.crear} initial={entrada} animate={visible} transition={{ duration: 0.25, ease: SALIDA }}>
          {resultado?.estado === "SIN_COINCIDENCIAS" ? (
            <p className={estilos.nadie} role="status">
              {resultado.mensaje}
            </p>
          ) : (
            <p className={estilos.ayuda}>Buscamos a quien está libre a esa hora en tu campus. El grupo solo se crea si alguien coincide.</p>
          )}
          <motion.button type="button" className={estilos.primario} onClick={onCrear} disabled={creando} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
            {creando ? (
              <>
                <span className={estilos.radar} aria-hidden="true" /> Buscando compañeros libres…
              </>
            ) : (
              <>
                <IconoPersonas tamano={18} />
                {resultado?.estado === "SIN_COINCIDENCIAS" ? "Volver a buscar" : "Crear el grupo y avisar"}
              </>
            )}
          </motion.button>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

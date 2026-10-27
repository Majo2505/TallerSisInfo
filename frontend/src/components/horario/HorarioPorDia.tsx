"use client";

import { useMemo, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { IconoMas, IconoRayo } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { Bloque } from "@/lib/horario";
import { Puente, duracionMin, hhmm } from "@/lib/puentes";
import estilos from "./HorarioPorDia.module.css";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const CORTOS = ["", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];

type Color = { fondo: string; borde: string; texto: string };

type Props = {
  bloques: Bloque[];
  puentes: Puente[];
  colorDe: (materia: string) => Color;
  onAgregar: (dia: number) => void;
  onEditar: (bloque: Bloque) => void;
};

type Elemento =
  | { tipo: "clase"; inicio: string; bloque: Bloque }
  | { tipo: "puente"; inicio: string; puente: Puente };

function diaInicial(): number {
  const hoy = new Date().getDay();
  return hoy >= 1 && hoy <= 6 ? hoy : 1;
}

export default function HorarioPorDia({ bloques, puentes, colorDe, onAgregar, onEditar }: Props) {
  const reducir = useReducedMotion();
  const [dia, setDia] = useState(diaInicial);

  const elementos = useMemo<Elemento[]>(() => {
    const clases: Elemento[] = bloques
      .filter((b) => b.diaSemana === dia)
      .map((bloque) => ({ tipo: "clase", inicio: bloque.horaInicio, bloque }));
    const huecos: Elemento[] = puentes
      .filter((p) => p.diaSemana === dia)
      .map((puente) => ({ tipo: "puente", inicio: puente.horaInicio, puente }));
    return [...clases, ...huecos].sort((a, b) => a.inicio.localeCompare(b.inicio));
  }, [bloques, puentes, dia]);

  const item = {
    oculto: { opacity: 0, y: 10, filter: "blur(4px)" },
    visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.35, ease: SALIDA } },
  };

  return (
    <section className={estilos.contenedor} aria-label="Horario por día">
      <div className={estilos.dias} role="tablist" aria-label="Días de la semana">
        {[1, 2, 3, 4, 5, 6].map((d) => {
          const clases = bloques.filter((b) => b.diaSemana === d).length;
          const huecos = puentes.filter((p) => p.diaSemana === d).length;
          return (
            <motion.button
              key={d}
              type="button"
              role="tab"
              aria-selected={dia === d}
              aria-label={`${DIAS[d]}: ${clases} clases y ${huecos} puentes`}
              className={dia === d ? estilos.diaActivo : estilos.dia}
              onClick={() => setDia(d)}
              whileTap={{ scale: 0.94 }}
              transition={RESORTE_TOQUE}
            >
              {dia === d && (
                <motion.span
                  layoutId="dia-horario"
                  className={estilos.pastilla}
                  transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
                />
              )}
              <span className={estilos.textoDia}>{CORTOS[d]}</span>
              <span className={estilos.puntos} aria-hidden="true">
                {Array.from({ length: Math.min(clases, 4) }, (_, i) => (
                  <i key={`c${i}`} className={estilos.puntoClase} />
                ))}
                {Array.from({ length: Math.min(huecos, 2) }, (_, i) => (
                  <i key={`p${i}`} className={estilos.puntoPuente} />
                ))}
              </span>
            </motion.button>
          );
        })}
      </div>

      <div className={estilos.tarjeta}>
        <header className={estilos.cabecera}>
          <AnimatePresence mode="wait" initial={false}>
            <motion.h3
              key={dia}
              className={estilos.titulo}
              initial={reducir ? false : { opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -6 }}
              transition={{ duration: 0.2, ease: SALIDA }}
            >
              {DIAS[dia]}
            </motion.h3>
          </AnimatePresence>
          <button type="button" className={estilos.agregar} onClick={() => onAgregar(dia)}>
            <IconoMas tamano={16} /> Agregar
          </button>
        </header>

        <AnimatePresence mode="wait" initial={false}>
          {elementos.length === 0 ? (
            <motion.p
              key={`vacio-${dia}`}
              className={estilos.vacio}
              initial={reducir ? false : { opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
            >
              No tienes clases este día.
            </motion.p>
          ) : (
            <motion.ul
              key={`lista-${dia}`}
              className={estilos.lista}
              initial={reducir ? false : "oculto"}
              animate="visible"
              exit={{ opacity: 0, transition: { duration: 0.12 } }}
              variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.05 } } }}
            >
              {elementos.map((el) =>
                el.tipo === "clase" ? (
                  <motion.li key={`c${el.bloque.id}`} variants={item}>
                    <button
                      type="button"
                      className={estilos.clase}
                      style={{ ["--acento" as string]: colorDe(el.bloque.materia).borde }}
                      onClick={() => onEditar(el.bloque)}
                      aria-label={`Editar ${el.bloque.materia}, de ${hhmm(el.bloque.horaInicio)} a ${hhmm(el.bloque.horaFin)}`}
                    >
                      <span className={estilos.hora}>
                        {hhmm(el.bloque.horaInicio)}
                        <small>{hhmm(el.bloque.horaFin)}</small>
                      </span>
                      <span className={estilos.datosClase}>
                        <strong>{el.bloque.materia}</strong>
                        {el.bloque.aula && <span>{el.bloque.aula}</span>}
                      </span>
                    </button>
                  </motion.li>
                ) : (
                  <motion.li key={`p${el.puente.id}`} className={estilos.puente} variants={item}>
                    <span className={estilos.iconoPuente}>
                      <IconoRayo tamano={16} />
                    </span>
                    <span>
                      <strong>Puente libre · {duracionMin(el.puente)} min</strong>
                      <small>
                        {hhmm(el.puente.horaInicio)} – {hhmm(el.puente.horaFin)}
                      </small>
                    </span>
                  </motion.li>
                ),
              )}
            </motion.ul>
          )}
        </AnimatePresence>
      </div>
    </section>
  );
}

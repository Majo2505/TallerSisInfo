"use client";

import { motion, useReducedMotion } from "motion/react";
import { SALIDA } from "@/components/app/Movimiento";
import { Bloque, minutosOcupados } from "@/lib/horario";
import { Puente, duracionMin, hhmm } from "@/lib/puentes";
import estilos from "./ResumenSemanal.module.css";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const CORTOS = ["", "L", "M", "X", "J", "V", "S"];

type Props = {
  bloques: Bloque[];
  puentes: Puente[];
  cargando: boolean;
};

function duracion(min: number): string {
  const h = Math.floor(min / 60);
  const m = Math.round(min % 60);
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

/**
 * La semana de un vistazo: cuánto pesa cada día (horas de clase y tiempo libre en puentes) y los datos que no
 * están en otro lado (día más cargado, día más libre, entrada más temprana y salida más tarde). Los totales
 * (clases, puentes y minutos libres) ya están arriba, en la portada, así que aquí no se repiten.
 * Solo cuentan las clases a las que el estudiante asiste.
 */
export default function ResumenSemanal({ bloques, puentes, cargando }: Props) {
  const reducir = useReducedMotion();
  const asistidas = bloques.filter((b) => b.asiste);

  const dias = [1, 2, 3, 4, 5, 6].map((d) => {
    const clases = asistidas.filter((b) => b.diaSemana === d);
    return {
      dia: d,
      clase: minutosOcupados(clases),
      libre: puentes.filter((p) => p.diaSemana === d).reduce((acc, p) => acc + duracionMin(p), 0),
      entrada: clases.map((b) => b.horaInicio).sort()[0],
      salida: clases.map((b) => b.horaFin).sort().at(-1),
    };
  });
  const tope = Math.max(60, ...dias.map((d) => d.clase + d.libre));
  const conClases = dias.filter((d) => d.clase > 0);

  const cargado = conClases.reduce<(typeof dias)[number] | undefined>((a, d) => (!a || d.clase > a.clase ? d : a), undefined);
  const libre = dias.reduce<(typeof dias)[number] | undefined>((a, d) => (d.libre > 0 && (!a || d.libre > a.libre) ? d : a), undefined);
  const temprano = conClases.reduce<(typeof dias)[number] | undefined>((a, d) => (!a || d.entrada! < a.entrada! ? d : a), undefined);
  const tarde = conClases.reduce<(typeof dias)[number] | undefined>((a, d) => (!a || d.salida! > a.salida! ? d : a), undefined);
  const sinClases = dias.filter((d) => d.clase === 0).map((d) => DIAS[d.dia]);

  const datos = [
    { titulo: "Día más cargado", valor: cargado ? `${DIAS[cargado.dia]} · ${duracion(cargado.clase)} de clase` : "—" },
    {
      titulo: "Día más libre",
      valor: libre ? `${DIAS[libre.dia]} · ${duracion(libre.libre)} en puentes` : sinClases.length ? `${sinClases[0]} sin clases` : "—",
    },
    { titulo: "Entrada más temprana", valor: temprano ? `${DIAS[temprano.dia]} · ${hhmm(temprano.entrada!)}` : "—" },
    { titulo: "Salida más tarde", valor: tarde ? `${DIAS[tarde.dia]} · ${hhmm(tarde.salida!)}` : "—" },
  ];

  return (
    <section className={estilos.panel} aria-labelledby="resumen-titulo">
      <h2 id="resumen-titulo" className={estilos.titulo}>
        Tu semana de un vistazo
      </h2>

      <div className={estilos.grafico} role="img" aria-label={dias.map((d) => `${DIAS[d.dia]}: ${duracion(d.clase)} de clase y ${duracion(d.libre)} libres`).join(". ")}>
        {dias.map((d, i) => (
          <div key={d.dia} className={estilos.columna}>
            <div className={estilos.pista}>
              <motion.span
                className={estilos.libre}
                style={{ height: `${(d.libre / tope) * 100}%` }}
                initial={reducir ? false : { scaleY: 0 }}
                animate={{ scaleY: cargando ? 0 : 1 }}
                transition={{ duration: 0.6, ease: SALIDA, delay: 0.15 + i * 0.05 }}
              />
              <motion.span
                className={estilos.clase}
                style={{ height: `${(d.clase / tope) * 100}%` }}
                initial={reducir ? false : { scaleY: 0 }}
                animate={{ scaleY: cargando ? 0 : 1 }}
                transition={{ duration: 0.6, ease: SALIDA, delay: 0.1 + i * 0.05 }}
              />
            </div>
            <span className={estilos.dia}>{CORTOS[d.dia]}</span>
          </div>
        ))}
      </div>
      <p className={estilos.leyenda} aria-hidden="true">
        <span className={estilos.puntoClase} /> Clases <span className={estilos.puntoLibre} /> Puentes
      </p>

      <dl className={estilos.datos}>
        {datos.map(({ titulo, valor }) => (
          <div key={titulo} className={estilos.dato}>
            <dt>{titulo}</dt>
            <dd>{cargando ? "–" : valor}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

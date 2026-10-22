"use client";

import { useMemo, useState } from "react";
import { IconoMas, IconoRayo } from "@/components/app/Iconos";
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

  return (
    <section className={estilos.contenedor} aria-label="Horario por día">
      <div className={estilos.dias} role="tablist" aria-label="Días de la semana">
        {[1, 2, 3, 4, 5, 6].map((d) => {
          const clases = bloques.filter((b) => b.diaSemana === d).length;
          const huecos = puentes.filter((p) => p.diaSemana === d).length;
          return (
            <button
              key={d}
              type="button"
              role="tab"
              aria-selected={dia === d}
              aria-label={`${DIAS[d]}: ${clases} clases y ${huecos} puentes`}
              className={dia === d ? estilos.diaActivo : estilos.dia}
              onClick={() => setDia(d)}
            >
              {CORTOS[d]}
              <span className={estilos.conteos}>
                <span className={estilos.clases}>{clases}c</span>
                <span className={estilos.huecos}>{huecos}p</span>
              </span>
            </button>
          );
        })}
      </div>

      <div className={estilos.tarjeta}>
        <header className={estilos.cabecera}>
          <h3 className={estilos.titulo}>{DIAS[dia]}</h3>
          <button type="button" className={estilos.agregar} onClick={() => onAgregar(dia)}>
            <IconoMas tamano={16} /> Agregar clase
          </button>
        </header>

        {elementos.length === 0 ? (
          <p className={estilos.vacio}>No tienes clases este día.</p>
        ) : (
          <ul className={estilos.lista}>
            {elementos.map((el) =>
              el.tipo === "clase" ? (
                <li key={`c${el.bloque.id}`}>
                  <button
                    type="button"
                    className={estilos.clase}
                    style={{ borderLeftColor: colorDe(el.bloque.materia).borde }}
                    onClick={() => onEditar(el.bloque)}
                    aria-label={`Editar ${el.bloque.materia}, de ${hhmm(el.bloque.horaInicio)} a ${hhmm(el.bloque.horaFin)}`}
                  >
                    <strong>{el.bloque.materia}</strong>
                    <span>
                      {el.bloque.aula ? `${el.bloque.aula} · ` : ""}
                      {hhmm(el.bloque.horaInicio)}–{hhmm(el.bloque.horaFin)}
                    </span>
                  </button>
                </li>
              ) : (
                <li key={`p${el.puente.id}`} className={estilos.puente}>
                  <IconoRayo tamano={16} />
                  <span>
                    <strong>Puente libre · {duracionMin(el.puente)} min</strong>
                    <small>
                      {hhmm(el.puente.horaInicio)} – {hhmm(el.puente.horaFin)}
                    </small>
                  </span>
                </li>
              ),
            )}
          </ul>
        )}
      </div>
    </section>
  );
}

"use client";
import { Puente, duracionMin, esDespuesDeClases, hhmm } from "@/lib/puentes";
import estilos from "./puentesDetectados.module.css";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

function IconoRayo() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8Z" />
    </svg>
  );
}

type Props = {
  puentes: Puente[];
  cargando: boolean;
  error: string;
  onReintentar: () => void;
};

export default function PuentesDetectados({ puentes, cargando, error, onReintentar }: Props) {
  const entreClases = puentes.filter((p) => !esDespuesDeClases(p));
  const numero = (p: Puente) => entreClases.indexOf(p) + 1;
  return (
    <section className={estilos.panel} aria-labelledby="puentes-titulo">
      <h2 id="puentes-titulo" className={estilos.titulo}>Puentes detectados</h2>

      {cargando && <div className={estilos.skeleton} aria-busy="true" aria-label="Cargando puentes" />}

      {!cargando && error && (
        <div className={estilos.error} role="alert">
          <span>{error}</span>
          <button className={estilos.reintentar} onClick={onReintentar}>Reintentar</button>
        </div>
      )}

      {!cargando && !error && puentes.length === 0 && (
        <p className={estilos.vacio}>
          Todavía no hay puentes. Un puente es un hueco libre de 45 min o más entre dos clases del mismo día; se detecta solo cuando registras tu horario, junto con tu tiempo libre después de clases.
        </p>
      )}

      {!cargando && !error && puentes.length > 0 && (
        <ul className={estilos.lista}>
          {puentes.map((p) => (
            <li key={p.id} className={esDespuesDeClases(p) ? `${estilos.tarjeta} ${estilos.despues}` : estilos.tarjeta}>
              <div className={estilos.fila}>
                <span className={estilos.nombre}>
                  <IconoRayo /> {esDespuesDeClases(p) ? "Después de clases" : `Puente ${numero(p)}`}
                </span>
                <span className={estilos.insignia}>{duracionMin(p)} min</span>
              </div>
              <p className={estilos.hora}>{DIAS[p.diaSemana]} · {hhmm(p.horaInicio)} – {hhmm(p.horaFin)}</p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

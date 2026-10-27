"use client";
import { Puente, duracionMin, hhmm } from "@/lib/puentes";
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

/** Lista de puentes detectados. Los datos los trae la pantalla que lo usa (hoy, el horario). */
export default function PuentesDetectados({ puentes, cargando, error, onReintentar }: Props) {
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
          Todavía no hay puentes. Un puente es un hueco libre de 45 min o más entre dos clases del mismo día; se detecta solo cuando registras tu horario.
        </p>
      )}

      {!cargando && !error && puentes.length > 0 && (
        <ul className={estilos.lista}>
          {puentes.map((p, i) => (
            <li key={p.id} className={estilos.tarjeta}>
              <div className={estilos.fila}>
                <span className={estilos.nombre}><IconoRayo /> Puente {i + 1}</span>
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

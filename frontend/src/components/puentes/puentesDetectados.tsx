"use client";
import { IconoLuna } from "@/components/app/Iconos";
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
  const despues = puentes.filter(esDespuesDeClases);
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

      {!cargando && !error && entreClases.length === 0 && (
        <p className={estilos.vacio}>
          Todavía no hay puentes. Un puente es un hueco libre de 45 min o más entre dos clases del mismo día; se detecta solo cuando registras tu horario.
        </p>
      )}

      {!cargando && !error && entreClases.length > 0 && (
        <ul className={estilos.lista}>
          {entreClases.map((p, i) => (
            <li key={p.id} className={estilos.tarjeta}>
              <div className={estilos.fila}>
                <span className={estilos.nombre}>
                  <IconoRayo /> Puente {i + 1}
                </span>
                <span className={estilos.insignia}>{duracionMin(p)} min</span>
              </div>
              <p className={estilos.hora}>{DIAS[p.diaSemana]} · {hhmm(p.horaInicio)} – {hhmm(p.horaFin)}</p>
            </li>
          ))}
        </ul>
      )}

      {/* No es un puente: va aparte, más chico y sin color, para que los puentes sean lo primero que se ve. */}
      {!cargando && !error && despues.length > 0 && (
        <section className={estilos.secundario} aria-labelledby="despues-titulo">
          <h3 id="despues-titulo" className={estilos.subtitulo}>
            <IconoLuna tamano={14} /> Después de clases
          </h3>
          <p className={estilos.subnota}>No es un puente: es tu tiempo libre desde la última clase hasta las 21:00.</p>
          <ul className={estilos.listaTenue}>
            {despues.map((p) => (
              <li key={p.id}>
                <span>{DIAS[p.diaSemana]}</span>
                <span>desde las {hhmm(p.horaInicio)}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </section>
  );
}

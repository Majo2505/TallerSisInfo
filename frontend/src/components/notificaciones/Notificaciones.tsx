"use client";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  INTERVALO_CONSULTA_MS,
  Notificacion,
  guardarConfiguracion,
  listarNotificaciones,
  marcarLeida,
  obtenerConfiguracion,
} from "@/lib/notificaciones";
import { hhmm } from "@/lib/puentes";
import estilos from "./Notificaciones.module.css";

function IconoCampana() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </svg>
  );
}

function IconoRayo() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8Z" />
    </svg>
  );
}

function formatearDuracion(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

// "Hoy" o "22/10", seguido del rango del puente.
function cuando(n: Notificacion): string {
  if (!n.fecha || !n.horaInicio || !n.horaFin) return "";
  const hoy = new Date().toLocaleDateString("en-CA"); // yyyy-MM-dd en hora local
  const [, mes, dia] = n.fecha.split("-");
  const etiquetaDia = n.fecha === hoy ? "Hoy" : `${dia}/${mes}`;
  return `${etiquetaDia} · ${hhmm(n.horaInicio)} – ${hhmm(n.horaFin)}`;
}

type Props = {
  /** Cambia cada vez que se guarda el horario: se vuelve a consultar para retirar avisos de puentes que ya no existen. */
  recarga: number;
};

/**
 * Avisos de puente disponible (HU4): campana con contador, panel con los avisos y la preferencia,
 * y un aviso emergente cuando llega uno nuevo. El backend crea el aviso al empezar el puente;
 * aquí se consulta periódicamente (todavía no hay push).
 */
export default function Notificaciones({ recarga }: Props) {
  const [lista, setLista] = useState<Notificacion[]>([]);
  const [activas, setActivas] = useState<boolean | null>(null); // null = aún no se sabe
  const [guardandoConfig, setGuardandoConfig] = useState(false);
  const [abierto, setAbierto] = useState(false);
  const [emergente, setEmergente] = useState<Notificacion | null>(null);
  const [error, setError] = useState("");
  const yaMostradas = useRef(new Set<number>()); // para no repetir el aviso emergente
  const contenedor = useRef<HTMLDivElement>(null);

  const consultar = useCallback(async (sigueActivo: () => boolean) => {
    try {
      const datos = await listarNotificaciones();
      if (!sigueActivo()) return;
      setLista(datos);
      setError("");
      // Llegan de la más reciente a la más antigua: se muestra la primera sin leer que no se haya mostrado.
      const nueva = datos.find((n) => !n.leida && !yaMostradas.current.has(n.id));
      datos.forEach((n) => yaMostradas.current.add(n.id));
      // Si el puente dejó de existir, su aviso ya no viene en la lista y se retira.
      setEmergente((actual) => nueva ?? (actual && datos.some((n) => n.id === actual.id) ? actual : null));
    } catch (e) {
      if (sigueActivo()) setError((e as Error).message);
    }
  }, []);

  // Consulta periódica mientras la pestaña está visible, y al volver a ella.
  useEffect(() => {
    let activo = true;
    const sigueActivo = () => activo;
    const revisar = () => {
      if (document.visibilityState === "visible") void consultar(sigueActivo); // consultar maneja sus errores
    };
    revisar();
    const intervalo = setInterval(revisar, INTERVALO_CONSULTA_MS);
    document.addEventListener("visibilitychange", revisar);
    return () => {
      activo = false;
      clearInterval(intervalo);
      document.removeEventListener("visibilitychange", revisar);
    };
  }, [consultar, recarga]);

  useEffect(() => {
    let activo = true;
    obtenerConfiguracion()
      .then((c) => activo && setActivas(c.notificacionesActivas))
      .catch((e: Error) => activo && setError(e.message));
    return () => { activo = false; };
  }, []);

  // El panel se cierra con Escape o al hacer clic fuera.
  useEffect(() => {
    if (!abierto) return;
    const alTeclear = (e: KeyboardEvent) => e.key === "Escape" && setAbierto(false);
    const alClic = (e: MouseEvent) => {
      if (!contenedor.current?.contains(e.target as Node)) setAbierto(false);
    };
    window.addEventListener("keydown", alTeclear);
    document.addEventListener("mousedown", alClic);
    return () => {
      window.removeEventListener("keydown", alTeclear);
      document.removeEventListener("mousedown", alClic);
    };
  }, [abierto]);

  const leer = async (id: number) => {
    setLista((l) => l.map((n) => (n.id === id ? { ...n, leida: true } : n)));
    setEmergente((actual) => (actual?.id === id ? null : actual));
    try {
      await marcarLeida(id);
    } catch {
      // Sin efecto visible: la próxima consulta trae el estado real.
    }
  };

  const cambiarPreferencia = async () => {
    if (activas === null) return;
    const nueva = !activas;
    setActivas(nueva); // se refleja al instante; si el servidor falla, se revierte
    if (!nueva) setEmergente(null);
    setGuardandoConfig(true);
    setError("");
    try {
      const c = await guardarConfiguracion({ notificacionesActivas: nueva });
      setActivas(c.notificacionesActivas);
    } catch (e) {
      setActivas(!nueva);
      setError((e as Error).message);
    } finally {
      setGuardandoConfig(false);
    }
  };

  const noLeidas = lista.filter((n) => !n.leida).length;

  return (
    <div className={estilos.contenedor} ref={contenedor}>
      <button
        type="button"
        className={estilos.campana}
        aria-expanded={abierto}
        aria-controls="notificaciones-panel"
        aria-label={noLeidas ? `Notificaciones, ${noLeidas} sin leer` : "Notificaciones"}
        onClick={() => setAbierto((a) => !a)}
      >
        <IconoCampana />
        {noLeidas > 0 && <span className={estilos.contador} aria-hidden="true">{noLeidas > 9 ? "9+" : noLeidas}</span>}
      </button>

      {abierto && (
        <section id="notificaciones-panel" className={estilos.panel} aria-labelledby="notificaciones-titulo">
          <h2 id="notificaciones-titulo" className={estilos.titulo}>Notificaciones</h2>

          <label className={estilos.preferencia}>
            <input
              type="checkbox"
              role="switch"
              checked={activas ?? false}
              disabled={activas === null || guardandoConfig}
              onChange={cambiarPreferencia}
            />
            <span>Avisarme cuando empiece un puente</span>
          </label>
          {activas === false && (
            <p className={estilos.nota}>No vas a recibir avisos, pero tus puentes se siguen detectando en tu horario.</p>
          )}

          {error && <p className={estilos.error} role="alert">{error}</p>}

          {lista.length === 0 ? (
            <p className={estilos.vacio}>Sin avisos por ahora. Te vamos a avisar apenas empiece uno de tus puentes.</p>
          ) : (
            <ul className={estilos.lista}>
              {lista.map((n) => (
                <li key={n.id} className={n.leida ? estilos.item : `${estilos.item} ${estilos.nueva}`}>
                  <p className={estilos.mensaje}>{n.mensaje}</p>
                  <div className={estilos.pie}>
                    <span>{cuando(n)}</span>
                    {!n.leida && (
                      <button type="button" className={estilos.enlace} onClick={() => leer(n.id)}>
                        Marcar como leída
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}

      {emergente && (
        <div className={estilos.emergente} role="alert">
          <span className={estilos.emergenteIcono}><IconoRayo /></span>
          <div className={estilos.emergenteTexto}>
            <strong>¡Puente disponible!</strong>
            {emergente.duracionMinutos !== null && emergente.horaInicio && emergente.horaFin ? (
              <span>
                Tienes <b>{formatearDuracion(emergente.duracionMinutos)}</b> libres, de {hhmm(emergente.horaInicio)} a {hhmm(emergente.horaFin)}.
              </span>
            ) : (
              <span>{emergente.mensaje}</span>
            )}
          </div>
          <button type="button" className={estilos.entendido} onClick={() => leer(emergente.id)}>Entendido</button>
        </div>
      )}
    </div>
  );
}

"use client";

import { useCallback, useEffect, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { motion, useReducedMotion } from "motion/react";
import { SALIDA } from "@/components/app/Movimiento";
import { EstadoParticipacion, Participante, Participantes, listarParticipantes } from "@/lib/comunidades";
import { iniciales } from "@/lib/iniciales";
import { useEventoEnVivo } from "@/lib/tiempoReal";
import type { EventoComunidad } from "@/lib/useComunidadesRealtime";
import estilos from "./ParticipantesComunidad.module.css";

const sinSuscripcion = () => () => {};

const GRUPOS: { estado: EstadoParticipacion; titulo: string }[] = [
  { estado: "CONFIRMADO", titulo: "Van" },
  { estado: "PENDIENTE", titulo: "Sin responder" },
  { estado: "RECHAZADO", titulo: "No van" },
];

type Props = {
  comunidadId: number;
  actividad: string;
  miNombre: string;
  onCerrar: () => void;
};

export default function ParticipantesComunidad({ comunidadId, actividad, miNombre, onCerrar }: Props) {
  const reducir = useReducedMotion();
  const enCliente = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  const [datos, setDatos] = useState<Participantes | null>(null);
  const [error, setError] = useState("");

  const cargar = useCallback(() => {
    let activo = true;
    listarParticipantes(comunidadId)
      .then((d) => activo && (setDatos(d), setError("")))
      .catch((e: Error) => activo && setError(e.message || "No pudimos cargar la lista. Inténtalo de nuevo."));
    return () => {
      activo = false;
    };
  }, [comunidadId]);

  useEffect(() => cargar(), [cargar]);

  // HU18: si alguien responde mientras la lista está abierta, se actualiza sola.
  useEventoEnVivo("comunidadActualizada", (evento: EventoComunidad) => {
    if (evento.comunidadId === comunidadId && evento.estado === "ACTIVA") cargar();
  });

  useEffect(() => {
    const alTeclear = (e: KeyboardEvent) => e.key === "Escape" && onCerrar();
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [onCerrar]);

  if (!enCliente) return null;

  const avance = datos ? Math.min(1, datos.confirmados / datos.maxParticipantes) : 0;
  const libres = datos ? Math.max(0, datos.maxParticipantes - datos.confirmados) : 0;

  const fila = (p: Participante) => (
    <li key={`${p.estado}-${p.nombre}-${p.esCreador}`} className={estilos.fila} data-estado={p.estado}>
      <span className={p.esTu ? estilos.avatarTu : estilos.avatar} aria-hidden="true">
        {iniciales(p.esTu ? miNombre : p.nombre)}
      </span>
      <span className={estilos.quien}>
        <strong>{p.nombre}</strong>
        {(p.carrera || p.esCreador) && (
          <small>{[p.esCreador ? "Creó la actividad" : null, p.carrera].filter(Boolean).join(" · ")}</small>
        )}
      </span>
    </li>
  );

  return createPortal(
    <motion.div
      className="hz-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && onCerrar()}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
    >
      <motion.div
        className={`hz-modal ${estilos.modal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="titulo-participantes"
        aria-busy={!datos && !error}
        initial={reducir ? false : { opacity: 0, scale: 0.95, y: 24 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={reducir ? { opacity: 0 } : { opacity: 0, scale: 0.97, y: 12 }}
        transition={{ type: "spring", stiffness: 380, damping: 32 }}
      >
        <header className={estilos.cabecera}>
          <span className={estilos.etiqueta}>Quiénes van</span>
          <h2 id="titulo-participantes">{datos?.actividad ?? actividad}</h2>
          {datos && (
            <p className={estilos.resumen}>
              Hoy, {datos.horaInicio}–{datos.horaFin} · {datos.confirmados} de {datos.maxParticipantes} van
              {libres > 0 ? ` · ${libres === 1 ? "queda 1 lugar" : `quedan ${libres} lugares`}` : " · grupo completo"}
            </p>
          )}
          <div className={estilos.barra} aria-hidden="true">
            <motion.span
              className={estilos.relleno}
              initial={reducir ? false : { transform: "scaleX(0)" }}
              animate={{ transform: `scaleX(${avance})` }}
              transition={{ duration: 0.6, ease: SALIDA }}
            />
          </div>
        </header>

        {error && (
          <div className={estilos.error} role="alert">
            <span>{error}</span>
            <button type="button" className="hz-btn" onClick={() => cargar()}>
              Reintentar
            </button>
          </div>
        )}

        {!datos && !error && (
          <ul className={estilos.lista} aria-hidden="true">
            {[0, 1, 2].map((i) => (
              <li key={i} className={estilos.esqueleto} />
            ))}
          </ul>
        )}

        {datos &&
          GRUPOS.map(({ estado, titulo }) => {
            const grupo = datos.participantes.filter((p) => p.estado === estado);
            if (grupo.length === 0) return null;
            return (
              <section key={estado} className={estilos.grupo} aria-label={titulo}>
                <h3>
                  {titulo} <span>{grupo.length}</span>
                </h3>
                <ul className={estilos.lista}>{grupo.map(fila)}</ul>
              </section>
            );
          })}

        {datos && !datos.esMia && <p className={estilos.nota}>Solo ves a quienes ya confirmaron.</p>}

        <div className="hz-acciones">
          <span className="hz-espacio" />
          <button type="button" className="hz-btn hz-btn-primary" onClick={onCerrar} autoFocus>
            Listo
          </button>
        </div>
      </motion.div>
    </motion.div>,
    document.body,
  );
}

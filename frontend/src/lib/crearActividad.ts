import { pedirApi } from "./api";
import type { ActividadExtraida } from "./actividades";

/** Bolivia es UTC-4 todo el año (sin horario de verano). */
const DESFASE_BOLIVIA_HORAS = -4;

/** La comunidad que devuelve el servidor al crearla. */
export type ComunidadCreada = {
  id: number;
  actividad: string;
  texto: string;
  inicio: string; // ISO en UTC
  fin: string;
  maxParticipantes: number;
  estado: string;
};

/** Una actividad que el servidor entendió por completo: ningún dato queda en null. */
export type ActividadCompleta = { actividad: string; horaInicio: string; duracionMin: number; maxParticipantes: number };

/** Devuelve la actividad si tiene todos los datos; si falta alguno, `null`. */
export function comoCompleta(a: ActividadExtraida): ActividadCompleta | null {
  if (a.actividad === null || a.horaInicio === null || a.duracionMin === null || a.maxParticipantes === null) return null;
  return { actividad: a.actividad, horaInicio: a.horaInicio, duracionMin: a.duracionMin, maxParticipantes: a.maxParticipantes };
}

/**
 * Pasa «HH:mm de hoy en Bolivia» a inicio y fin en UTC. La fecha de hoy se toma de la hora de Bolivia, no de la del navegador,
 * para que el resultado sea el mismo desde cualquier zona horaria.
 */
export function inicioYFinUtc(horaInicio: string, duracionMin: number, ahora: Date = new Date()): { inicio: string; fin: string } {
  const [horas, minutos] = horaInicio.split(":").map(Number);
  const hoyEnBolivia = new Date(ahora.getTime() + DESFASE_BOLIVIA_HORAS * 3_600_000);
  const inicioMs = Date.UTC(hoyEnBolivia.getUTCFullYear(), hoyEnBolivia.getUTCMonth(), hoyEnBolivia.getUTCDate(), horas - DESFASE_BOLIVIA_HORAS, minutos);
  return { inicio: new Date(inicioMs).toISOString(), fin: new Date(inicioMs + duracionMin * 60_000).toISOString() };
}

/**
 * Crea la actividad (HU10) cuando el estudiante confirma el resumen. `consultaId` es el de la extracción (HU11): el servidor la
 * guarda como `log_id` de la comunidad y no deja crear dos veces la misma.
 */
export const crearActividad = (actividad: ActividadCompleta, texto: string, consultaId: number, ahora: Date = new Date()) => {
  const { inicio, fin } = inicioYFinUtc(actividad.horaInicio, actividad.duracionMin, ahora);
  return pedirApi<ComunidadCreada>("/api/comunidades", {
    method: "POST",
    body: JSON.stringify({ texto, actividad: actividad.actividad, inicio, fin, maxParticipantes: actividad.maxParticipantes, consultaId }),
  });
};

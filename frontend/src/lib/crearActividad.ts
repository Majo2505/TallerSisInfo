import { pedirApi } from "./api";
import type { ActividadExtraida } from "./actividades";
import type { Invitacion } from "./comunidades";

/** La actividad que devuelve el servidor al crearla. Las horas son de hoy, en hora de Bolivia. */
export type ActividadCreada = {
  id: number;
  actividad: string;
  horaInicio: string; // "HH:mm"
  horaFin: string;
  duracionMin: number;
  maxParticipantes: number;
  estado: string;
  invitacion?: Invitacion | null; // HU13: a quiénes se invitó automáticamente
};

/** Una actividad que el servidor entendió por completo: ningún dato queda en null. */
export type ActividadCompleta = { actividad: string; horaInicio: string; duracionMin: number; maxParticipantes: number };

/** Devuelve la actividad si tiene todos los datos; si falta alguno, `null`. */
export function comoCompleta(a: ActividadExtraida): ActividadCompleta | null {
  if (a.actividad === null || a.horaInicio === null || a.duracionMin === null || a.maxParticipantes === null) return null;
  return { actividad: a.actividad, horaInicio: a.horaInicio, duracionMin: a.duracionMin, maxParticipantes: a.maxParticipantes };
}

/**
 * Crea la actividad (HU10) cuando el estudiante confirma el resumen. `consultaId` es el de la extracción (HU11): el servidor
 * vuelve a validar los datos, la guarda como `log_id` de la comunidad y no deja crear dos veces la misma.
 */
export const crearActividad = (actividad: ActividadCompleta, texto: string, consultaId: number) =>
  pedirApi<ActividadCreada>("/api/actividades", {
    method: "POST",
    body: JSON.stringify({ texto, ...actividad, consultaId }),
  });

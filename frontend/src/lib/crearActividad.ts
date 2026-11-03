import { pedirApi } from "./api";
import type { ActividadExtraida } from "./actividades";
import type { Invitacion } from "./comunidades";

export type ActividadCreada = {
  id: number;
  actividad: string;
  horaInicio: string;
  horaFin: string;
  duracionMin: number;
  maxParticipantes: number;
  estado: string;
  invitacion?: Invitacion | null;
};

export type ActividadCompleta = { actividad: string; horaInicio: string; duracionMin: number; maxParticipantes: number };

export function comoCompleta(a: ActividadExtraida): ActividadCompleta | null {
  if (a.actividad === null || a.horaInicio === null || a.duracionMin === null || a.maxParticipantes === null) return null;
  return { actividad: a.actividad, horaInicio: a.horaInicio, duracionMin: a.duracionMin, maxParticipantes: a.maxParticipantes };
}

export const crearActividad = (actividad: ActividadCompleta, texto: string, consultaId: number) =>
  pedirApi<ActividadCreada>("/api/actividades", {
    method: "POST",
    body: JSON.stringify({ texto, ...actividad, consultaId }),
  });

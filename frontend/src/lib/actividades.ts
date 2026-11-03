import { pedirApi } from "./api";

/** HU11: campos que el servidor extrae del texto libre. */
export type CampoActividad = "actividad" | "horaInicio" | "duracionMin" | "maxParticipantes";

/** Datos entendidos y válidos. Un campo en null es un dato que falta (ver `faltantes`). */
export type ActividadExtraida = {
  actividad: string | null;
  horaInicio: string | null; // "HH:mm", hoy, hora de Bolivia
  duracionMin: number | null;
  maxParticipantes: number | null; // contando a quien la crea
};

export type RespuestaExtraccion = {
  consultaId: number; // registro de auditoría; HU10 lo guarda como log_id de la comunidad
  estado: "COMPLETO" | "INCOMPLETO" | "NO_ENTENDIDO";
  mensaje: string | null; // la pregunta por el primer dato faltante, o el aviso de que no se entendió
  actividad: ActividadExtraida;
  faltantes: CampoActividad[];
  supuestos: CampoActividad[]; // completados por el sistema (hoy solo la hora: «ahora»)
};

/**
 * Extrae la actividad del texto libre. Para responder la pregunta por un dato faltante se manda solo la respuesta
 * en `texto` y lo ya entendido en `anterior`: el estudiante no reescribe toda la descripción.
 */
export const extraerActividad = (texto: string, anterior: ActividadExtraida | null = null) =>
  pedirApi<RespuestaExtraccion>("/api/actividades/extraer", {
    method: "POST",
    body: JSON.stringify({ texto, anterior }),
  });

/** 60 → «1 hora», 90 → «1 h 30 min», 45 → «45 min». */
export function formatearDuracion(minutos: number): string {
  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;
  if (horas === 0) return `${resto} min`;
  if (resto === 0) return horas === 1 ? "1 hora" : `${horas} horas`;
  return `${horas} h ${resto} min`;
}

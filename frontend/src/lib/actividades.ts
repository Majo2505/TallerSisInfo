import { pedirApi } from "./api";

export type CampoActividad = "actividad" | "horaInicio" | "duracionMin" | "maxParticipantes";

export type ActividadExtraida = {
  actividad: string | null;
  horaInicio: string | null;
  duracionMin: number | null;
  maxParticipantes: number | null;
};

export type RespuestaExtraccion = {
  consultaId: number;
  estado: "COMPLETO" | "INCOMPLETO" | "NO_ENTENDIDO";
  mensaje: string | null;
  actividad: ActividadExtraida;
  faltantes: CampoActividad[];
  supuestos: CampoActividad[];
};

/** `consultaAnteriorId`: el `consultaId` de la respuesta anterior, para que la auditoría vea los turnos como una sola consulta. */
export const extraerActividad = (texto: string, anterior: ActividadExtraida | null = null, consultaAnteriorId: number | null = null) =>
  pedirApi<RespuestaExtraccion>("/api/actividades/extraer", {
    method: "POST",
    body: JSON.stringify({ texto, anterior, consultaAnteriorId }),
  });

export const revisarActividad = (texto: string, actividad: ActividadExtraida, consultaAnteriorId: number | null = null) =>
  pedirApi<RespuestaExtraccion>("/api/actividades/revisar", {
    method: "POST",
    body: JSON.stringify({ texto, actividad, consultaAnteriorId }),
  });

export function formatearDuracion(minutos: number): string {
  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;
  if (horas === 0) return `${resto} min`;
  if (resto === 0) return horas === 1 ? "1 hora" : `${horas} horas`;
  return `${horas} h ${resto} min`;
}

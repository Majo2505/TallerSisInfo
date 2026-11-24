import { pedirApi } from "./api";

export type Notificacion = {
  id: number;
  tipo:
    | "PUENTE_DISPONIBLE"
    | "DESPUES_DE_CLASES"
    | "INVITACION_COMUNIDAD"
    | "COMUNIDAD_CERRADA"
    // Una conexión de «Libres contigo» te invita a un evento de hoy (migración 032). Trae `eventoId`.
    | "INVITACION_EVENTO"
    // Para quien creó la comunidad (migración 031): alguien se sumó o se bajó. Uno por comunidad, se reemplaza.
    | "ASISTENCIA_COMUNIDAD"
    | "SANCION_ADVERTENCIA"
    | "CONEXION_SOLICITADA"
    | "CONEXION_ACEPTADA"
    | "REPORTE_RESUELTO"
    | "EVENTO_APROBADO"
    | "EVENTO_RECHAZADO"
    // HU26: el administrador bajó un evento ya publicado. El mensaje trae el motivo.
    | "EVENTO_BAJADO"
    // Para el administrador (migración 030): quedan leídos para todos los admins cuando uno los resuelve.
    | "REPORTE_NUEVO"
    | "EVENTO_POR_REVISAR"
    // HU32 (migración 033), para el administrador: alertas sobre un estudiante. Traen `usuarioObjetivoId`.
    | "ALERTA_USUARIO_SOSPECHOSO"
    | "ALERTA_REPORTES_ACUMULADOS"
    // HU35 (migración 034): el admin quitó o restauró una comunidad en la que estabas. Traen `comunidadId`.
    | "COMUNIDAD_BAJADA"
    | "COMUNIDAD_RESTAURADA";
  mensaje: string;
  fecha: string | null;
  horaInicio: string | null;
  horaFin: string | null;
  duracionMinutos: number | null;
  leida: boolean;
  creadaEn: string;
  comunidadId?: number | null;
  /** HU27: en REPORTE_RESUELTO, el reporte revisado (la campanita lleva a «Mis reportes»); en REPORTE_NUEVO, el reporte por revisar. */
  reporteId?: number | null;
  /** En los avisos de eventos (por ejemplo INVITACION_EVENTO): la campanita lleva al detalle del evento. */
  eventoId?: number | null;
  /** HU32: en las alertas al admin, el estudiante del que se avisa (la campanita lleva a Admin → Usuarios). */
  usuarioObjetivoId?: number | null;
};

export type ConfiguracionNotificaciones = { notificacionesActivas: boolean };

export const INTERVALO_CONSULTA_MS = 15_000;

export const listarNotificaciones = () => pedirApi<Notificacion[]>("/api/notificaciones");
export const marcarLeida = (id: number) => pedirApi<void>(`/api/notificaciones/${id}/leida`, { method: "PATCH" });
export const obtenerConfiguracion = () => pedirApi<ConfiguracionNotificaciones>("/api/notificaciones/configuracion");
export const guardarConfiguracion = (c: ConfiguracionNotificaciones) =>
  pedirApi<ConfiguracionNotificaciones>("/api/notificaciones/configuracion", { method: "PUT", body: JSON.stringify(c) });

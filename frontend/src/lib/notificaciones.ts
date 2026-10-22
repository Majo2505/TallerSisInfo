import { pedirApi } from "./api";

export type Notificacion = {
  id: number;
  tipo: "PUENTE_DISPONIBLE" | "INVITACION_COMUNIDAD" | "COMUNIDAD_CERRADA";
  mensaje: string;
  fecha: string | null; // "yyyy-MM-dd" (hora de Bolivia); null si no es aviso de puente
  horaInicio: string | null; // "HH:mm:ss"
  horaFin: string | null;
  duracionMinutos: number | null;
  leida: boolean;
  creadaEn: string; // ISO en UTC
};

export type ConfiguracionNotificaciones = { notificacionesActivas: boolean };

// El backend crea el aviso al empezar el puente; sin push todavía, se consulta cada tanto.
export const INTERVALO_CONSULTA_MS = 15_000;

export const listarNotificaciones = () => pedirApi<Notificacion[]>("/api/notificaciones");
export const marcarLeida = (id: number) => pedirApi<void>(`/api/notificaciones/${id}/leida`, { method: "PATCH" });
export const obtenerConfiguracion = () => pedirApi<ConfiguracionNotificaciones>("/api/notificaciones/configuracion");
export const guardarConfiguracion = (c: ConfiguracionNotificaciones) =>
  pedirApi<ConfiguracionNotificaciones>("/api/notificaciones/configuracion", { method: "PUT", body: JSON.stringify(c) });

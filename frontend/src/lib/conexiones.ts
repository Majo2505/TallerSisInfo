import { pedirApi } from "./api";

/** HU12: saludos entre compañeros de «Libres contigo». */

export type EstadoConexion = "PENDIENTE" | "ACEPTADA" | "RECHAZADA";

export type ConexionResumen = { id: number; estado: EstadoConexion; enviadaPorMi: boolean };

/** Antes de aceptarse, solo el nombre corto y sin correo; aceptada, nombre completo y correo. */
export type Conexion = {
  id: number;
  estado: EstadoConexion;
  enviadaPorMi: boolean;
  nombre: string;
  carrera: string | null;
  semestre: number | null;
  /** La del test: es la misma vista desde los dos lados. */
  afinidad: number;
  enComun: string[];
  diaSemana: number;
  horaInicio: string;
  horaFin: string;
  correo: string | null;
  creadaEn: string;
};

export type Conexiones = { recibidas: Conexion[]; enviadas: Conexion[]; aceptadas: Conexion[] };

export const listarConexiones = () => pedirApi<Conexiones>("/api/conexiones");

export const saludar = (usuarioId: number, puenteId: number) =>
  pedirApi<Conexion>("/api/conexiones", { method: "POST", body: JSON.stringify({ usuarioId, puenteId }) });

export const responderSaludo = (id: number, aceptar: boolean) =>
  pedirApi<Conexion>(`/api/conexiones/${id}`, { method: "PUT", body: JSON.stringify({ aceptar }) });

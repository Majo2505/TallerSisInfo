import { pedirApi } from "./api";
import { ConexionResumen } from "./conexiones";
import { Puente } from "./puentes";

export type ConfiguracionMatching = { visible: boolean; desde: string | null };

/** Coincide contigo en tiempo libre y en el test. `nombre` es el corto («Ana Q.»): el completo, al conectar. */
export type CompaneroCompatible = {
  usuarioId: number;
  nombre: string;
  carrera: string | null;
  semestre: number | null;
  horaInicio: string;
  horaFin: string;
  minutosEnComun: number;
  porcentaje: number;
  franjasCompartidas: number;
  /** Qué tanto se parecen según el test (0–100). */
  afinidad: number;
  /** Lo que se muestra: 60 % test y 40 % tiempo libre. */
  compatibilidad: number;
  enComun: string[];
  conexion: ConexionResumen | null;
};

export type EstadoMatching = "OK" | "SIN_RESULTADOS" | "SIN_ZONA" | "NO_VISIBLE" | "SIN_TEST";

export type ResultadoMatching = {
  estado: EstadoMatching;
  mensaje: string | null;
  puente: Puente;
  minutosPuente: number;
  companeros: CompaneroCompatible[];
};

export const obtenerConfiguracionMatching = () => pedirApi<ConfiguracionMatching>("/api/matching/configuracion");
export const guardarConfiguracionMatching = (visible: boolean) =>
  pedirApi<ConfiguracionMatching>("/api/matching/configuracion", { method: "PUT", body: JSON.stringify({ visible }) });
export const buscarCompaneros = (puenteId: number) => pedirApi<ResultadoMatching>(`/api/matching?puenteId=${puenteId}`);

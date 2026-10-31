import { pedirApi } from "./api";
import { Puente } from "./puentes";

/**
 * Matching de compañeros con puente compatible (HU12). El backend cruza tu puente con los de tu sede (mismo día,
 * 30 minutos o más en común) y los devuelve ordenados por compatibilidad. Solo aparece quien aceptó aparecer.
 */
export type ConfiguracionMatching = { visible: boolean; desde: string | null };

export type CompaneroCompatible = {
  nombre: string;
  carrera: string | null;
  semestre: number | null;
  horaInicio: string; // tramo en común, "HH:mm:ss"
  horaFin: string;
  minutosEnComun: number;
  porcentaje: number; // qué parte de tu puente cubre, 1 a 100
  franjasCompartidas: number; // en cuántos de tus puentes de la semana coinciden
};

export type EstadoMatching = "OK" | "SIN_RESULTADOS" | "SIN_ZONA" | "NO_VISIBLE";

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

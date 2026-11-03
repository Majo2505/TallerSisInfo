import { pedirApi } from "./api";
import { Puente } from "./puentes";

export type ConfiguracionMatching = { visible: boolean; desde: string | null };

export type CompaneroCompatible = {
  nombre: string;
  carrera: string | null;
  semestre: number | null;
  horaInicio: string;
  horaFin: string;
  minutosEnComun: number;
  porcentaje: number;
  franjasCompartidas: number;
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

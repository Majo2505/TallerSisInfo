import { pedirApi } from "./api";

/** Un lugar verificado del catálogo que cabe en el puente. Distancia y tiempos salen del catálogo, no de la IA. */
export type LugarViable = {
  lugarId: number;
  nombre: string;
  categoria: string;
  descripcion: string | null;
  distanciaM: number;
  tiempoTrasladoMin: number; // ida y vuelta
  duracionEstimadaMin: number;
};

export type ResultadoLugares = {
  estado: "OK" | "SIN_RESULTADOS" | "SIN_ZONA";
  mensaje: string | null; // solo si el estado no es OK
  zona: { id: number; nombre: string } | null;
  radioAplicadoM: number | null;
  minutosDisponibles: number;
  lugares: LugarViable[];
};

export const buscarLugaresViables = (puenteId: number) =>
  pedirApi<ResultadoLugares>(`/api/lugares/viables?puenteId=${puenteId}`);

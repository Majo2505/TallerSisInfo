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

const MENSAJE_SIN_RESULTADOS = "No encontramos opciones cercanas para este tiempo disponible.";

/** HU8 (criterio 4): un lugar sin alguna de sus métricas no se muestra. */
export const lugarTieneMetricasCompletas = (l: LugarViable) =>
  Boolean(l.nombre?.trim()) &&
  Boolean(l.categoria?.trim()) &&
  Number.isFinite(l.distanciaM) && l.distanciaM >= 0 &&
  Number.isFinite(l.tiempoTrasladoMin) && l.tiempoTrasladoMin >= 0 &&
  Number.isFinite(l.duracionEstimadaMin) && l.duracionEstimadaMin > 0;

/** Deja solo los lugares completos; si no queda ninguno, el resultado pasa a SIN_RESULTADOS. */
const soloConMetricas = (r: ResultadoLugares): ResultadoLugares => {
  if (r.estado !== "OK") return r;
  const lugares = r.lugares.filter(lugarTieneMetricasCompletas);
  return lugares.length > 0
    ? { ...r, lugares }
    : { ...r, estado: "SIN_RESULTADOS", mensaje: MENSAJE_SIN_RESULTADOS, lugares: [] };
};

export const buscarLugaresViables = (puenteId: number) =>
  pedirApi<ResultadoLugares>(`/api/lugares/viables?puenteId=${puenteId}`).then(soloConMetricas);
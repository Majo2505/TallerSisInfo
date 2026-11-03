import { pedirApi } from "./api";

/** Todos los datos salen del catálogo; del asistente solo sale `explicacion` (HU7). */
export type Recomendacion = {
  id: number;
  lugarId: number;
  nombre: string;
  categoria: string;
  distanciaM: number;
  tiempoTrasladoMin: number; // ida y vuelta
  duracionEstimadaMin: number;
  explicacion: string;
};

export type RespuestaRecomendacion = {
  consultaId: number;
  estado: "OK" | "SIN_RESULTADOS";
  mensaje: string | null; // solo con SIN_RESULTADOS
  recomendaciones: Recomendacion[];
};

// Con un LLM local la respuesta puede tardar bastante; el backend corta a los LLM_TIMEOUT_SECONDS y responde 503.
const MENSAJE_SIN_RESULTADOS = "No encontramos opciones cercanas para este tiempo disponible.";

/** HU8 (criterio 4): una recomendación sin alguna de sus métricas no se muestra. */
export const tieneMetricasCompletas = (r: Recomendacion) =>
  Boolean(r.nombre?.trim()) &&
  Boolean(r.categoria?.trim()) &&
  Number.isFinite(r.distanciaM) && r.distanciaM >= 0 &&
  Number.isFinite(r.tiempoTrasladoMin) && r.tiempoTrasladoMin >= 0 &&
  Number.isFinite(r.duracionEstimadaMin) && r.duracionEstimadaMin > 0;

/** Deja solo las recomendaciones completas; si no queda ninguna, la respuesta pasa a SIN_RESULTADOS. */
const soloConMetricas = (resp: RespuestaRecomendacion): RespuestaRecomendacion => {
  if (resp.estado !== "OK") return resp;
  const recomendaciones = resp.recomendaciones.filter(tieneMetricasCompletas);
  return recomendaciones.length > 0
    ? { ...resp, recomendaciones }
    : { ...resp, estado: "SIN_RESULTADOS", mensaje: MENSAJE_SIN_RESULTADOS, recomendaciones: [] };
};

export const pedirRecomendaciones = (puenteId: number, entrada?: string) =>
  pedirApi<RespuestaRecomendacion>("/api/recomendaciones", {
    method: "POST",
    body: JSON.stringify({ puenteId, entrada }),
  }).then(soloConMetricas);
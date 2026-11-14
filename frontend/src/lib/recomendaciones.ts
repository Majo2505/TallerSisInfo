import { pedirApi } from "./api";

export type Recomendacion = {
  id: number;
  lugarId: number;
  nombre: string;
  categoria: string;
  distanciaM: number;
  tiempoTrasladoMin: number;
  duracionEstimadaMin: number;
  explicacion: string;
  feedback: "LIKE" | "DISLIKE" | null;
  fotoUrl?: string | null;
  /** De 1 a 5, a partir de los votos de los estudiantes; null si nadie votó. */
  puntuacion?: number | null;
  votos?: number;
  /** Verificado por el equipo de BreakU. */
  verificado?: boolean;
};

export type RespuestaRecomendacion = {
  consultaId: number;
  estado: "OK" | "SIN_RESULTADOS";
  mensaje: string | null;
  recomendaciones: Recomendacion[];
};

const MENSAJE_SIN_RESULTADOS = "No encontramos opciones cercanas para este tiempo disponible.";

export const tieneMetricasCompletas = (r: Recomendacion) =>
  Boolean(r.nombre?.trim()) &&
  Boolean(r.categoria?.trim()) &&
  Number.isFinite(r.distanciaM) && r.distanciaM >= 0 &&
  Number.isFinite(r.tiempoTrasladoMin) && r.tiempoTrasladoMin >= 0 &&
  Number.isFinite(r.duracionEstimadaMin) && r.duracionEstimadaMin > 0;

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

export type Valoracion = "LIKE" | "DISLIKE";
export type FeedbackGuardado = { recomendacionId: number; feedback: Valoracion; fechaFeedback: string };

export const guardarFeedback = (id: number, feedback: Valoracion) =>
  pedirApi<FeedbackGuardado>(`/api/recomendaciones/${id}/feedback`, {
    method: "PATCH",
    body: JSON.stringify({ feedback }),
  });

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
  feedback: "LIKE" | "DISLIKE" | null;
};

export type RespuestaRecomendacion = {
  consultaId: number;
  estado: "OK" | "SIN_RESULTADOS";
  mensaje: string | null; // solo con SIN_RESULTADOS
  recomendaciones: Recomendacion[];
};

// Con un LLM local la respuesta puede tardar bastante; el backend corta a los LLM_TIMEOUT_SECONDS y responde 503.
export const pedirRecomendaciones = (puenteId: number, entrada?: string) =>
  pedirApi<RespuestaRecomendacion>("/api/recomendaciones", {
    method: "POST",
    body: JSON.stringify({ puenteId, entrada }),
  });

/** HU9: una sola valoración por recomendación; votar de nuevo reemplaza la anterior. */
export type Valoracion = "LIKE" | "DISLIKE";
export type FeedbackGuardado = { recomendacionId: number; feedback: Valoracion; fechaFeedback: string };

export const guardarFeedback = (id: number, feedback: Valoracion) =>
  pedirApi<FeedbackGuardado>(`/api/recomendaciones/${id}/feedback`, {
    method: "PATCH",
    body: JSON.stringify({ feedback }),
  });

import { pedirApi } from "./api";

export type EventoRecomendado = {
  id: number;
  titulo: string;
  descripcion: string;
  fecha: string;
  inicio: string;
  fin: string;
  lugar: string | null;
  fuenteUrl: string;
};

export const formatearFechaEvento = (fecha: string): string =>
  new Date(`${fecha}T00:00:00`).toLocaleDateString("es-BO", { weekday: "short", day: "numeric", month: "short" });

export const listarEventosRecomendados = () =>
  pedirApi<EventoRecomendado[]>("/api/eventos/recomendados");

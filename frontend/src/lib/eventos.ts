import { pedirApi } from "./api";

export type EventoRecomendado = {
  id: number;
  titulo: string;
  descripcion: string;
  inicio: string;
  fin: string;
  lugar: string | null;
  fuenteUrl: string;
};

export const listarEventosRecomendados = () =>
  pedirApi<EventoRecomendado[]>("/api/eventos/recomendados");

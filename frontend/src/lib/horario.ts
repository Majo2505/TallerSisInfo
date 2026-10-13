import { pedirApi } from "./api";

export type Bloque = {
  id: number;
  materia: string;
  diaSemana: number;
  horaInicio: string; // "HH:mm"
  horaFin: string;
  aula: string | null;
  origen: string;
};
export type BloqueInput = Omit<Bloque, "id" | "origen">;

export const listarHorario = () => pedirApi<Bloque[]>("/api/horario");
export const crearBloque = (b: BloqueInput) => pedirApi<Bloque>("/api/horario", { method: "POST", body: JSON.stringify(b) });
export const editarBloque = (id: number, b: BloqueInput) => pedirApi<Bloque>(`/api/horario/${id}`, { method: "PUT", body: JSON.stringify(b) });
export const eliminarBloque = (id: number) => pedirApi<void>(`/api/horario/${id}`, { method: "DELETE" });

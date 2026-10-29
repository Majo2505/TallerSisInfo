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

/** Foto del horario (HU2): el backend la lee con un modelo de visión y devuelve las clases sin guardarlas. */
export type ClaseLeida = BloqueInput;
export type LecturaFoto = { clases: ClaseLeida[]; descartadas: number };
export type ResultadoLote = { guardados: Bloque[]; rechazados: { indice: number; materia: string; motivo: string }[] };

export const FOTO_BYTES_MAXIMOS = 8 * 1024 * 1024;
export const FOTO_TIPOS = ["image/jpeg", "image/png", "image/webp"];

export function leerFotoHorario(foto: File) {
  const datos = new FormData();
  datos.append("foto", foto);
  return pedirApi<LecturaFoto>("/api/horario/foto", { method: "POST", body: datos });
}

/** Guarda las clases confirmadas con origen FOTO; las que chocan vuelven en `rechazados`. */
export const guardarLote = (bloques: BloqueInput[]) =>
  pedirApi<ResultadoLote>("/api/horario/lote", { method: "POST", body: JSON.stringify({ bloques }) });

import { pedirApi } from "./api";

/** HU12: el test de afinidad de «Libres contigo». Las preguntas vienen del backend (Domain/ReglasAfinidad.cs). */

export type OpcionTest = { id: string; texto: string };

export type PreguntaTest = {
  id: string;
  bloque: string;
  texto: string;
  tipo: "UNICA" | "MULTIPLE";
  maximo: number;
  opcional: boolean;
  opciones: OpcionTest[];
};

export type RespuestasTest = Record<string, string[]>;

export type TestAfinidad = {
  version: number;
  minutosEstimados: number;
  preguntas: PreguntaTest[];
  completado: boolean;
  respuestas: RespuestasTest | null;
  actualizadoEn: string | null;
};

export const obtenerTest = () => pedirApi<TestAfinidad>("/api/afinidad/test");

export const guardarTest = (respuestas: RespuestasTest) =>
  pedirApi<TestAfinidad>("/api/afinidad/test", { method: "PUT", body: JSON.stringify({ respuestas }) });

/** Una pregunta está lista si es opcional o tiene entre 1 y su máximo de opciones. */
export function preguntaLista(p: PreguntaTest, elegidas: string[] | undefined): boolean {
  const n = elegidas?.length ?? 0;
  if (n === 0) return p.opcional;
  return p.tipo === "UNICA" ? n === 1 : n <= p.maximo;
}

/** Elegir o quitar una opción: en las de una sola, reemplaza; en las de varias, no pasa del máximo. */
export function alternar(p: PreguntaTest, elegidas: string[], opcion: string): string[] {
  if (elegidas.includes(opcion)) return elegidas.filter((o) => o !== opcion);
  if (p.tipo === "UNICA") return [opcion];
  return elegidas.length >= p.maximo ? elegidas : [...elegidas, opcion];
}

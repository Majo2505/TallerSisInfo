import { pedirApi } from "./api";

export type TipoPuente = "ENTRE_CLASES" | "DESPUES_DE_CLASES";

export type Puente = {
  id: number;
  diaSemana: number;
  horaInicio: string;
  horaFin: string;
  tipo: TipoPuente;
};

const DEMO = process.env.NEXT_PUBLIC_PUENTES_DEMO === "true";
const PUENTES_DEMO: Puente[] = [
  { id: 1, diaSemana: 1, horaInicio: "09:00:00", horaFin: "11:00:00", tipo: "ENTRE_CLASES" },
  { id: 2, diaSemana: 1, horaInicio: "13:00:00", horaFin: "15:00:00", tipo: "ENTRE_CLASES" },
  { id: 4, diaSemana: 1, horaInicio: "17:00:00", horaFin: "21:00:00", tipo: "DESPUES_DE_CLASES" },
  { id: 3, diaSemana: 3, horaInicio: "10:30:00", horaFin: "11:30:00", tipo: "ENTRE_CLASES" },
];

export async function listarPuentes(): Promise<Puente[]> {
  if (DEMO) return PUENTES_DEMO;
  return pedirApi<Puente[]>("/api/puentes");
}

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};
export const duracionMin = (p: Puente) => aMin(p.horaFin) - aMin(p.horaInicio);
export const hhmm = (h: string) => h.slice(0, 5);

/** Tiempo libre desde la última clase del día hasta las 21:00 (no es un puente entre dos clases). */
export const esDespuesDeClases = (p: Puente) => p.tipo === "DESPUES_DE_CLASES";
export const soloEntreClases = (puentes: Puente[]) => puentes.filter((p) => !esDespuesDeClases(p));
/** «tu puente» o «tu tiempo después de clases», para armar frases. */
export const tuTiempo = (p?: Puente) => (p && esDespuesDeClases(p) ? "tu tiempo después de clases" : "tu puente");
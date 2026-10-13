import { pedirApi } from "./api";

export type Puente = {
  id: number;
  diaSemana: number; // 1 = lunes ... 6 = sábado
  horaInicio: string; // "HH:mm:ss"
  horaFin: string; // "HH:mm:ss"
};

// Modo demo (solo desarrollo): con NEXT_PUBLIC_PUENTES_DEMO=true en .env.local se usan estos datos
// de ejemplo y no se llama a la API, así que no hace falta backend ni login.
const DEMO = process.env.NEXT_PUBLIC_PUENTES_DEMO === "true";
const PUENTES_DEMO: Puente[] = [
  { id: 1, diaSemana: 1, horaInicio: "09:00:00", horaFin: "11:00:00" },
  { id: 2, diaSemana: 1, horaInicio: "13:00:00", horaFin: "15:00:00" },
  { id: 3, diaSemana: 3, horaInicio: "10:30:00", horaFin: "11:30:00" },
];

export async function listarPuentes(): Promise<Puente[]> {
  if (DEMO) return PUENTES_DEMO;
  return pedirApi<Puente[]>("/api/puentes");
}

// El contrato no incluye la duración: se calcula aquí.
const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};
export const duracionMin = (p: Puente) => aMin(p.horaFin) - aMin(p.horaInicio);
export const hhmm = (h: string) => h.slice(0, 5);
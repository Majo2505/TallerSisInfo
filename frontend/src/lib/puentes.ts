export type Puente = {
  id: number;
  diaSemana: number; // 1 = lunes ... 6 = sábado
  horaInicio: string; // "HH:mm:ss"
  horaFin: string; // "HH:mm:ss"
};

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

// Igual que en lib/horario.ts: el lugar donde se guarda el JWT lo define HU1. Cuando se defina, solo cambia esta función.
function token(): string | null {
  return typeof window === "undefined" ? null : localStorage.getItem("token");
}

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

  let res: Response;
  try {
    res = await fetch(`${API}/api/puentes`, {
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token() ?? ""}` },
    });
  } catch {
    throw new Error("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }
  if (res.status === 401) throw new Error("Tu sesión expiró. Inicia sesión nuevamente.");
  if (!res.ok) throw new Error(`Ocurrió un error inesperado (${res.status}).`);
  return (await res.json()) as Puente[];
}

// El contrato no incluye la duración: se calcula aquí.
const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};
export const duracionMin = (p: Puente) => aMin(p.horaFin) - aMin(p.horaInicio);
export const hhmm = (h: string) => h.slice(0, 5);
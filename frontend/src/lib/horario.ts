import { obtenerToken } from "@/services/authService";

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

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

// Pendiente: dónde se guarda el JWT lo decide el equipo (Anexo D, punto 10). Por ahora lo guarda el login en memoria.
function token(): string | null {
  return obtenerToken();
}

async function pedir<T>(ruta: string, init: RequestInit = {}): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`${API}/api/horario${ruta}`, {
      ...init,
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token() ?? ""}`, ...init.headers },
    });
  } catch {
    throw new Error("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }
  if (res.status === 204) return undefined as T;
  if (res.status === 401) throw new Error("Tu sesión expiró. Inicia sesión nuevamente.");
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error(data?.mensaje ?? `Ocurrió un error inesperado (${res.status}).`);
  return data as T;
}

export const listarHorario = () => pedir<Bloque[]>("");
export const crearBloque = (b: BloqueInput) => pedir<Bloque>("", { method: "POST", body: JSON.stringify(b) });
export const editarBloque = (id: number, b: BloqueInput) => pedir<Bloque>(`/${id}`, { method: "PUT", body: JSON.stringify(b) });
export const eliminarBloque = (id: number) => pedir<void>(`/${id}`, { method: "DELETE" });

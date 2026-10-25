import { obtenerToken } from "@/services/authService";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export type Zona = { id: number; nombre: string };
export type EstadoUbicacion = {
  consentimientoActivo: boolean;
  zonaActual: Zona | null;
};

async function pedir<T>(ruta: string, init: RequestInit = {}): Promise<T> {
  const token = obtenerToken();
  if (!token) throw new Error("Tu sesión expiró. Inicia sesión nuevamente.");

  let res: Response;
  try {
    res = await fetch(`${API}/api/ubicacion${ruta}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        ...init.headers,
      },
    });
  } catch {
    throw new Error("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }

  if (res.status === 401) throw new Error("Tu sesión expiró. Inicia sesión nuevamente.");
  if (res.status === 204) return undefined as T;

  const data = await res.json().catch(() => null);
  if (!res.ok) {
    throw new Error(data?.detail ?? `Ocurrió un error inesperado (${res.status}).`);
  }
  return data as T;
}

export const obtenerEstadoUbicacion = () => pedir<EstadoUbicacion>("");
export const listarZonas = () => pedir<Zona[]>("/zonas");
export const compartirUbicacion = (latitud: number, longitud: number) =>
  pedir<EstadoUbicacion>("", {
    method: "PUT",
    body: JSON.stringify({ latitud, longitud }),
  });
export const seleccionarZona = (zonaId: number) =>
  pedir<EstadoUbicacion>("/zona", {
    method: "PUT",
    body: JSON.stringify({ zonaId }),
  });
export const revocarUbicacion = () => pedir<void>("", { method: "DELETE" });

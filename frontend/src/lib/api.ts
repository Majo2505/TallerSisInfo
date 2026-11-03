import { cerrarSesion, obtenerToken } from "@/services/authService";
import { API_URL } from "./config";

export class ErrorApi extends Error {
  constructor(
    mensaje: string,
    readonly codigo?: string,
  ) {
    super(mensaje);
  }
}

export async function pedirApi<T>(ruta: string, init: RequestInit = {}): Promise<T> {
  const token = obtenerToken();
  let res: Response;
  try {
    res = await fetch(`${API_URL}${ruta}`, {
      ...init,
      headers: {
        ...(init.body instanceof FormData ? {} : { "Content-Type": "application/json" }),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...init.headers,
      },
    });
  } catch {
    throw new Error("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }

  if (res.status === 401) {
    cerrarSesion();
    throw new Error("Se venció tu sesión. Inicia sesión de nuevo.");
  }
  if (res.status === 204) return undefined as T;
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new ErrorApi(data?.mensaje ?? `Hubo un error inesperado (${res.status}).`, data?.codigo);
  return data as T;
}

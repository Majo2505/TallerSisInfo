import { cerrarSesion, obtenerToken } from "@/services/authService";
import { API_URL } from "./config";

/**
 * Petición autenticada al backend: agrega el JWT de la sesión actual y unifica el manejo de errores.
 * Si el servidor responde 401 (token vencido o inválido) cierra la sesión; las pantallas protegidas
 * detectan que ya no hay sesión y llevan al login.
 */
export async function pedirApi<T>(ruta: string, init: RequestInit = {}): Promise<T> {
  const token = obtenerToken();
  let res: Response;
  try {
    res = await fetch(`${API_URL}${ruta}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...init.headers,
      },
    });
  } catch {
    throw new Error("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }

  if (res.status === 401) {
    cerrarSesion();
    throw new Error("Tu sesión expiró. Inicia sesión nuevamente.");
  }
  if (res.status === 204) return undefined as T;
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error(data?.mensaje ?? `Ocurrió un error inesperado (${res.status}).`);
  return data as T;
}

import { cerrarSesion, expiracionMs, guardarAvisoLogin, obtenerToken } from "@/services/authService";
import { MENSAJE_CUENTA_RECHAZADA, MENSAJE_SESION_VENCIDA, motivoDelCorte401 } from "./reglasUsuarios";
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
    // HU28: el backend responde 401 igual para el token vencido y para una cuenta sancionada (sin cuerpo): se distinguen
    // por el vencimiento del token. Una sola salida limpia: se cierra la sesión y el login muestra el aviso.
    const corte = token ? motivoDelCorte401(expiracionMs(token), Date.now()) : "VENCIDA";
    cerrarSesion();
    if (corte === "CUENTA_RECHAZADA") {
      guardarAvisoLogin(MENSAJE_CUENTA_RECHAZADA);
      throw new Error(MENSAJE_CUENTA_RECHAZADA);
    }
    throw new Error(MENSAJE_SESION_VENCIDA);
  }
  if (res.status === 204) return undefined as T;
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new ErrorApi(data?.mensaje ?? `Hubo un error inesperado (${res.status}).`, data?.codigo);
  return data as T;
}

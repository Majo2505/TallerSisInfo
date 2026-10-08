const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export type UsuarioSesion = { id: number; nombre: string; correo: string };
export type Sesion = { token: string; usuario: UsuarioSesion };

// Pendiente (decide el equipo): dónde se guarda el JWT. Por ahora vive solo en memoria:
// no persiste al recargar la página ni al escribir la URL a mano (hay que volver a iniciar sesión).
let sesion: Sesion | null = null;
const oyentes = new Set<() => void>();

function avisar() {
  oyentes.forEach((oyente) => oyente());
}

export function suscribirSesion(oyente: () => void): () => void {
  oyentes.add(oyente);
  return () => {
    oyentes.delete(oyente);
  };
}

export const obtenerSesion = (): Sesion | null => sesion;
export const obtenerToken = (): string | null => sesion?.token ?? null;

export function cerrarSesion() {
  sesion = null;
  avisar();
}

/** Error con un mensaje listo para mostrar al usuario. */
export class ErrorAuth extends Error {}

type RespuestaLogin = { token: string; userId: number; nombre: string; email: string };

/**
 * POST /api/auth/login. Hoy el backend recibe { email, password } y responde { token, userId, nombre, email }.
 * Pendiente: el contrato (docs/api-contract.md) propone { correo, password } y una respuesta con `usuario`,
 * y errores en ProblemDetails. Cuando el backend se alinee, solo cambia este archivo.
 */
export async function iniciarSesion(correo: string, password: string): Promise<Sesion> {
  let res: Response;
  try {
    res = await fetch(`${API}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email: correo, password }),
    });
  } catch {
    throw new ErrorAuth("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }

  if (res.status === 401) {
    // Mismo mensaje para correo inexistente y contraseña incorrecta: no revela si el correo existe.
    throw new ErrorAuth("Correo o contraseña incorrectos");
  }
  const datos = await res.json().catch(() => null);
  if (!res.ok) {
    // Hoy el backend responde { message }; el contrato propone ProblemDetails ({ detail }). Se acepta cualquiera de los dos.
    throw new ErrorAuth(datos?.detail ?? datos?.message ?? "No pudimos iniciar sesión. Inténtalo de nuevo en unos minutos.");
  }

  const r = datos as RespuestaLogin;
  sesion = { token: r.token, usuario: { id: r.userId, nombre: r.nombre, correo: r.email } };
  avisar();
  return sesion;
}

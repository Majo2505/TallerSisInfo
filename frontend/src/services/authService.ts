declare const process: {
  env: {
    NEXT_PUBLIC_API_URL?: string;
  };
};

const API_URL =
  (typeof process !== 'undefined' && process.env?.NEXT_PUBLIC_API_URL) ||
  'http://localhost:5000/api/auth';

const TOKEN_KEY = 'jwtToken';
const USER_KEY = 'userData';

export interface AuthResponse {
  token: string;
  userId: number;
  nombre: string;
  email: string;
}

export const authService = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.message || 'Credenciales inválidas.');
    }

    if (typeof window !== 'undefined') {
      sessionStorage.setItem(TOKEN_KEY, data.token);
      sessionStorage.setItem(USER_KEY, JSON.stringify(data));
    }

    return data;
  },

  async register(nombre: string, email: string, password: string): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nombre, email, password }),
    });

    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.message || 'Error durante el registro.');
    }

    if (typeof window !== 'undefined') {
      sessionStorage.setItem(TOKEN_KEY, data.token);
      sessionStorage.setItem(USER_KEY, JSON.stringify(data));
    }

    return data;
  },

  logout(): void {
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(USER_KEY);
    }
  },

  getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return sessionStorage.getItem(TOKEN_KEY);
  },

  getUser(): AuthResponse | null {
    if (typeof window === 'undefined') return null;
    const user = sessionStorage.getItem(USER_KEY);
    try {
      return user ? JSON.parse(user) : null;
    } catch {
      return null;
    }
  },

  isAuthenticated(): boolean {
    return !!this.getToken();
  },
};
const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export type UsuarioSesion = { id: number; nombre: string; correo: string };
export type Sesion = { token: string; usuario: UsuarioSesion };

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

type RespuestaAuth = { token: string; userId: number; nombre: string; email: string };

/**
 * Llama a POST /api/auth/{ruta} y guarda la sesión en memoria.
 * Hoy el backend recibe { email, password } (y { nombre } al registrar) y responde { token, userId, nombre, email }.
 * Pendiente: el contrato (docs/api-contract.md) propone { correo, password } y una respuesta con `usuario`,
 * y errores en ProblemDetails. Cuando el backend se alinee, solo cambia este archivo.
 */
async function autenticar(ruta: "login" | "register", cuerpo: object): Promise<Sesion> {
  let res: Response;
  try {
    res = await fetch(`${API}/api/auth/${ruta}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(cuerpo),
    });
  } catch {
    throw new ErrorAuth("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }

  if (ruta === "login" && res.status === 401) {
    // Mismo mensaje para correo inexistente y contraseña incorrecta: no revela si el correo existe.
    throw new ErrorAuth("Correo o contraseña incorrectos");
  }
  const datos = await res.json().catch(() => null);
  if (!res.ok) {
    // Hoy el backend responde { message }; el contrato propone ProblemDetails ({ detail }). Se acepta cualquiera de los dos.
    throw new ErrorAuth(datos?.detail ?? datos?.message ?? "No pudimos completar la solicitud. Inténtalo de nuevo en unos minutos.");
  }

  const r = datos as RespuestaAuth;
  sesion = { token: r.token, usuario: { id: r.userId, nombre: r.nombre, correo: r.email } };
  avisar();
  return sesion;
}

export const iniciarSesion = (correo: string, password: string) =>
  autenticar("login", { email: correo, password });

export const registrarse = (nombre: string, correo: string, password: string) =>
  autenticar("register", { nombre, email: correo, password });

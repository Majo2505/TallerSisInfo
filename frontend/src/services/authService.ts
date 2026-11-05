import { API_URL } from "@/lib/config";

export type Rol = "ESTUDIANTE" | "ADMIN";
export type UsuarioSesion = { id: number; nombre: string; correo: string; rol?: Rol };
export type Sesion = { token: string; usuario: UsuarioSesion };

const CLAVE_SESION = "breaku.sesion";

let sesion: Sesion | null = null;
let cargada = false;
const oyentes = new Set<() => void>();

function avisar() {
  oyentes.forEach((oyente) => oyente());
}

function expiracionMs(token: string): number | null {
  try {
    const payload = token.split(".")[1];
    const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
    const { exp } = JSON.parse(json) as { exp?: unknown };
    return typeof exp === "number" ? exp * 1000 : null;
  } catch {
    return null;
  }
}

function tokenVigente(token: string): boolean {
  const exp = expiracionMs(token);
  return exp === null || exp > Date.now();
}

function leerAlmacen(): Sesion | null {
  try {
    const crudo = window.sessionStorage.getItem(CLAVE_SESION);
    if (!crudo) return null;
    const guardada = JSON.parse(crudo) as Sesion;
    if (!guardada?.token || !guardada?.usuario || !tokenVigente(guardada.token)) {
      window.sessionStorage.removeItem(CLAVE_SESION);
      return null;
    }
    return guardada;
  } catch {
    return null;
  }
}

function escribirAlmacen(nueva: Sesion | null) {
  try {
    if (nueva) window.sessionStorage.setItem(CLAVE_SESION, JSON.stringify(nueva));
    else window.sessionStorage.removeItem(CLAVE_SESION);
  } catch {
  }
}

function establecerSesion(nueva: Sesion | null) {
  sesion = nueva;
  cargada = true;
  escribirAlmacen(nueva);
  avisar();
}

export function suscribirSesion(oyente: () => void): () => void {
  oyentes.add(oyente);
  return () => {
    oyentes.delete(oyente);
  };
}

export function obtenerSesion(): Sesion | null {
  if (!cargada && typeof window !== "undefined") {
    sesion = leerAlmacen();
    cargada = true;
  }
  return sesion;
}

export const obtenerToken = (): string | null => obtenerSesion()?.token ?? null;

export const esAdmin = (s: Sesion | null): boolean => s?.usuario.rol === "ADMIN";

export const rutaDeEntrada = (s: Sesion | null): string => (esAdmin(s) ? "/admin/inicio" : "/inicio");

export function actualizarNombreSesion(nombre: string) {
  const actual = obtenerSesion();
  if (actual) establecerSesion({ ...actual, usuario: { ...actual.usuario, nombre } });
}

export function cerrarSesion() {
  establecerSesion(null);
}

export class ErrorAuth extends Error {}

type RespuestaAuth = { token: string; userId: number; nombre: string; email: string; rol?: Rol };

async function autenticar(ruta: "login" | "register" | "google", cuerpo: object, rolRequerido?: Rol): Promise<Sesion> {
  let res: Response;
  try {
    res = await fetch(`${API_URL}/api/auth/${ruta}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(cuerpo),
    });
  } catch {
    throw new ErrorAuth("No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.");
  }

  if (ruta === "login" && res.status === 401) {
    throw new ErrorAuth("Correo o contraseña incorrectos");
  }
  const datos = await res.json().catch(() => null);
  if (!res.ok) {
    throw new ErrorAuth(datos?.detail ?? datos?.message ?? "No pudimos completar la solicitud. Inténtalo de nuevo en unos minutos.");
  }

  const r = datos as RespuestaAuth;
  if (rolRequerido && r.rol !== rolRequerido) throw new ErrorAuth("Esta cuenta no tiene permisos de administración.");
  const nueva: Sesion = { token: r.token, usuario: { id: r.userId, nombre: r.nombre, correo: r.email, rol: r.rol ?? "ESTUDIANTE" } };
  establecerSesion(nueva);
  return nueva;
}

export const iniciarSesion = (correo: string, password: string) =>
  autenticar("login", { email: correo, password });

export const iniciarSesionAdmin = (correo: string, password: string) =>
  autenticar("login", { email: correo, password }, "ADMIN");

export const iniciarSesionConGoogle = (idToken: string) => autenticar("google", { idToken });

export const registrarse = (nombre: string, correo: string, password: string) =>
  autenticar("register", { nombre, email: correo, password });

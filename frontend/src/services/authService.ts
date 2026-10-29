import { API_URL } from "@/lib/config";

export type Rol = "ESTUDIANTE" | "ADMIN";
export type UsuarioSesion = { id: number; nombre: string; correo: string; rol?: Rol };
export type Sesion = { token: string; usuario: UsuarioSesion };

/**
 * La sesión se guarda en sessionStorage: sobrevive a un refresh (F5) y a navegar entre páginas,
 * pero se borra al cerrar la pestaña. Si el equipo prefiere que persista entre pestañas/cierres del
 * navegador, basta con cambiar `window.sessionStorage` por `window.localStorage` en este archivo.
 */
const CLAVE_SESION = "breaku.sesion";

// Copia en memoria de la sesión. IMPORTANTE: `obtenerSesion` debe devolver SIEMPRE la misma referencia
// mientras la sesión no cambie (lo exige useSyncExternalStore), por eso no se parsea el JSON en cada llamada.
let sesion: Sesion | null = null;
let cargada = false;
const oyentes = new Set<() => void>();

function avisar() {
  oyentes.forEach((oyente) => oyente());
}

/** Lee la fecha de expiración (claim `exp`) del JWT, en milisegundos. null si no se puede leer. */
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
    // Almacenamiento no disponible (modo privado estricto, etc.): la sesión dura solo hasta el próximo refresh.
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
  // En el servidor no hay sessionStorage; en el cliente se restaura una sola vez al primer acceso.
  if (!cargada && typeof window !== "undefined") {
    sesion = leerAlmacen();
    cargada = true;
  }
  return sesion;
}

export const obtenerToken = (): string | null => obtenerSesion()?.token ?? null;

/** El rol solo decide qué se muestra; el backend vuelve a comprobarlo en cada endpoint de /api/admin. */
export const esAdmin = (s: Sesion | null): boolean => s?.usuario.rol === "ADMIN";

/** Cada rol tiene su pantalla de entrada. */
export const rutaDeEntrada = (s: Sesion | null): string => (esAdmin(s) ? "/admin" : "/inicio");

/** Tras editar el perfil (HU21): el nombre nuevo se ve al instante en la barra y en el saludo. */
export function actualizarNombreSesion(nombre: string) {
  const actual = obtenerSesion();
  if (actual) establecerSesion({ ...actual, usuario: { ...actual.usuario, nombre } });
}

export function cerrarSesion() {
  establecerSesion(null);
}

/** Error con un mensaje listo para mostrar al usuario. */
export class ErrorAuth extends Error {}

type RespuestaAuth = { token: string; userId: number; nombre: string; email: string; rol?: Rol };

/**
 * Llama a POST /api/auth/{ruta} y guarda la sesión.
 * Hoy el backend recibe { email, password } (y { nombre } al registrar) y responde { token, userId, nombre, email }.
 * Pendiente: el contrato (docs/api-contract.md) propone { correo, password } y una respuesta con `usuario`,
 * y errores en ProblemDetails. Cuando el backend se alinee, solo cambia este archivo.
 */
async function autenticar(ruta: "login" | "register", cuerpo: object, rolRequerido?: Rol): Promise<Sesion> {
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
    // Mismo mensaje para correo inexistente y contraseña incorrecta: no revela si el correo existe.
    throw new ErrorAuth("Correo o contraseña incorrectos");
  }
  const datos = await res.json().catch(() => null);
  if (!res.ok) {
    // Hoy el backend responde { message }; el contrato propone ProblemDetails ({ detail }). Se acepta cualquiera de los dos.
    throw new ErrorAuth(datos?.detail ?? datos?.message ?? "No pudimos completar la solicitud. Inténtalo de nuevo en unos minutos.");
  }

  const r = datos as RespuestaAuth;
  // El acceso de administración no guarda la sesión de una cuenta sin ese rol.
  if (rolRequerido && r.rol !== rolRequerido) throw new ErrorAuth("Esta cuenta no tiene permisos de administración.");
  const nueva: Sesion = { token: r.token, usuario: { id: r.userId, nombre: r.nombre, correo: r.email, rol: r.rol ?? "ESTUDIANTE" } };
  establecerSesion(nueva);
  return nueva;
}

export const iniciarSesion = (correo: string, password: string) =>
  autenticar("login", { email: correo, password });

export const iniciarSesionAdmin = (correo: string, password: string) =>
  autenticar("login", { email: correo, password }, "ADMIN");

export const registrarse = (nombre: string, correo: string, password: string) =>
  autenticar("register", { nombre, email: correo, password });

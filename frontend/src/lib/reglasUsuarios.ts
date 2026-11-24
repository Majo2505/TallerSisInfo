// HU28: reglas de la interfaz de usuarios y sanciones. Funciones puras, sin React ni red, para poder probarlas.

export const ADVERTENCIAS_PARA_SUSPENSION = 3;
export const DIAS_SUSPENSION_POR_DEFECTO = 7;
export const LARGO_MAXIMO_MOTIVO = 500;

export type TipoSancion = "ADVERTENCIA" | "SUSPENSION" | "BAJA";

export const ETIQUETA_TIPO: Record<TipoSancion, string> = {
  ADVERTENCIA: "Advertencia",
  SUSPENSION: "Suspensión",
  BAJA: "Baja",
};

/** Con 3 advertencias vigentes (las levantadas no cuentan) la siguiente sanción sugerida es SUSPENSION. Solo sugiere: decide el admin. */
export const sugiereSuspension = (advertenciasVigentes: number): boolean => advertenciasVigentes >= ADVERTENCIAS_PARA_SUSPENSION;

/** La baja es definitiva: se pide escribir BAJA o el nombre del usuario (sin importar mayúsculas ni espacios de los bordes). */
export function confirmacionBajaValida(escrito: string, nombreUsuario: string): boolean {
  const texto = escrito.trim().toLocaleUpperCase("es");
  if (texto.length === 0) return false;
  const nombre = nombreUsuario.trim().toLocaleUpperCase("es");
  return texto === "BAJA" || (nombre.length > 0 && texto === nombre);
}

/** Texto de ayuda bajo la fecha de fin: vacía = 7 días por defecto (la suspensión nunca es indefinida). */
export function ayudaFechaFin(hasta: string): string {
  return hasta.trim()
    ? "La suspensión termina sola en esa fecha."
    : `Si dejas la fecha vacía, la suspensión durará ${DIAS_SUSPENSION_POR_DEFECTO} días por defecto.`;
}

/** Primer problema del formulario, o null si se puede enviar. `hasta` viene de `CampoFechaHora` («AAAA-MM-DDTHH:mm», hora local). */
export function validarSancion(tipo: TipoSancion, motivo: string, hasta: string, ahora: Date): string | null {
  const limpio = motivo.trim();
  if (limpio.length === 0) return "Escribe el motivo de la sanción.";
  if (limpio.length > LARGO_MAXIMO_MOTIVO) return `El motivo puede tener hasta ${LARGO_MAXIMO_MOTIVO} caracteres.`;
  if (tipo === "SUSPENSION" && hasta.trim()) {
    // El selector de fecha y hora deja el valor a medias («AAAA-MM-DDT» o «THH:mm») hasta que se eligen las dos partes.
    if (/^\d{4}-\d{2}-\d{2}T$|^T\d{2}:\d{2}$/.test(hasta.trim())) return "Elige el día y la hora en que termina la suspensión.";
    const fin = new Date(hasta);
    if (Number.isNaN(fin.getTime())) return "La fecha de fin no es válida.";
    if (fin.getTime() <= ahora.getTime()) return "La fecha de fin tiene que ser posterior a ahora.";
  }
  return null;
}

/** De la fecha local del input a UTC (ISO) para el backend; vacía = sin fecha (el backend pone 7 días). */
export const hastaParaApi = (hasta: string): string | null => (hasta.trim() ? new Date(hasta).toISOString() : null);

export type EstadoUsuario = "ACTIVO" | "SUSPENDIDO" | "BAJA" | "DESACTIVADA";

export const ETIQUETA_ESTADO: Record<EstadoUsuario, string> = {
  ACTIVO: "Activo",
  SUSPENDIDO: "Suspendido",
  BAJA: "Dado de baja",
  DESACTIVADA: "Desactivado",
};

/** Una cuenta dada de baja (o desactivada) ya no recibe sanciones. */
export const aceptaSanciones = (estado: EstadoUsuario): boolean => estado === "ACTIVO" || estado === "SUSPENDIDO";

export type SancionBasica = { tipo: TipoSancion; levantadaEn: string | null; vigente: boolean };

/** Se pueden levantar las advertencias sin levantar y las suspensiones vigentes. La baja no se levanta. */
export const sePuedeLevantar = (s: SancionBasica): boolean =>
  s.levantadaEn === null && (s.tipo === "ADVERTENCIA" || (s.tipo === "SUSPENSION" && s.vigente));

export const MENSAJE_CUENTA_RECHAZADA =
  "Tu cuenta ya no puede usar la plataforma porque fue suspendida o dada de baja. Inicia sesión para ver el motivo.";
export const MENSAJE_SESION_VENCIDA = "Se venció tu sesión. Inicia sesión de nuevo.";

/**
 * El backend corta con 401 tanto el token vencido como el de una cuenta sancionada. Solo el segundo lleva el cuerpo
 * `{ codigo: "CUENTA_SANCIONADA", mensaje }`: devuelve el mensaje (motivo y fecha) o null si fue un 401 normal.
 */
export const CODIGO_CUENTA_SANCIONADA = "CUENTA_SANCIONADA";

export function mensajeDeSesionSancionada(cuerpo: { codigo?: unknown; mensaje?: unknown } | null): string | null {
  if (cuerpo?.codigo !== CODIGO_CUENTA_SANCIONADA) return null;
  return typeof cuerpo.mensaje === "string" && cuerpo.mensaje.trim() ? cuerpo.mensaje : MENSAJE_CUENTA_RECHAZADA;
}

/** A quién se sanciona desde un reporte: el creador de la comunidad o del evento reportado. Null si no hay (lugar, app, objeto borrado). */
export function usuarioParaSancionar(r: { usuarioReportadoId?: number | null; usuarioReportadoNombre?: string | null }): { id: number; nombre: string } | null {
  return typeof r.usuarioReportadoId === "number" ? { id: r.usuarioReportadoId, nombre: r.usuarioReportadoNombre?.trim() || "este usuario" } : null;
}

/** Los reportes de una comunidad o de un evento apuntan a un usuario; los de la app o un lugar no. */
export const reporteDeUsuario = (motivo: string): boolean => motivo === "COMUNIDAD_INAPROPIADA" || motivo === "EVENTO_INAPROPIADO";

/** El mensaje del backend para una cuenta suspendida o dada de baja (403 del login): se muestra tal cual, con su motivo y fecha. */
export function mensajeDeLoginBloqueado(estado: number, cuerpo: { message?: unknown; mensaje?: unknown } | null): string | null {
  if (estado !== 403) return null;
  const texto = cuerpo?.message ?? cuerpo?.mensaje;
  return typeof texto === "string" && texto.trim() ? texto : null;
}

/**
 * HU32: el motivo con el que se abre la advertencia si el estudiante tiene alertas activas: el de la más reciente. Es solo
 * una sugerencia editable; nunca se envía sin que el admin confirme. Sin alertas, vacío.
 */
export const MOTIVO_SUGERIDO: Record<"ALERTA_USUARIO_SOSPECHOSO" | "ALERTA_REPORTES_ACUMULADOS", string> = {
  ALERTA_USUARIO_SOSPECHOSO: "Intentos repetidos bloqueados por seguridad",
  ALERTA_REPORTES_ACUMULADOS: "Reportes acumulados",
};

export function motivoSugeridoDeAlertas(alertas: readonly { tipo: string; fecha: string }[]): string {
  const reciente = [...alertas].sort((a, b) => b.fecha.localeCompare(a.fecha))[0];
  return reciente && reciente.tipo in MOTIVO_SUGERIDO ? MOTIVO_SUGERIDO[reciente.tipo as keyof typeof MOTIVO_SUGERIDO] : "";
}

// HU34-FE: varias fechas del mismo evento leídas de un afiche. Funciones puras (sin React, red ni navegador) para
// probarlas con `node --test`. Las fechas van en hora de Bolivia, «yyyy-MM-ddTHH:mm», igual que el formulario.
import { ahoraBoliviaParaFormulario } from "./reglasEventos.ts";

/** Una fecha del evento tal como la devuelve `POST /api/eventos/extraer` en `sesiones[]`. */
export type SesionFlyer = { inicio: string; fin: string | null };

/** Límite de HU23: eventos creados por día (hora de Bolivia). El admin no tiene límite. */
export const MAXIMO_EVENTOS_POR_DIA = 3;

/**
 * Cuántos eventos más puede crear hoy. Cuenta los de «Mis eventos» creados hoy en Bolivia (también los cancelados,
 * igual que el servidor). `null` = sin límite (admin).
 */
export function cupoDeHoy(eventos: readonly { createdAt: string }[], ahora: Date, admin: boolean): number | null {
  if (admin) return null;
  const hoy = ahoraBoliviaParaFormulario(ahora).slice(0, 10);
  const usados = eventos.filter((e) => {
    const creado = new Date(e.createdAt);
    return !Number.isNaN(creado.getTime()) && ahoraBoliviaParaFormulario(creado).slice(0, 10) === hoy;
  }).length;
  return Math.max(0, MAXIMO_EVENTOS_POR_DIA - usados);
}

/** Se puede publicar: tiene hora de fin y todavía no empezó. */
export const sesionDisponible = (s: SesionFlyer, ahora: string): boolean => s.fin !== null && s.inicio > ahora;

/** Al abrir «revisar» quedan marcadas las fechas disponibles, hasta donde alcance el cupo de hoy. */
export function marcasIniciales(sesiones: readonly SesionFlyer[], cupo: number | null, ahora: string): boolean[] {
  let quedan = cupo ?? Infinity;
  return sesiones.map((s) => {
    if (!sesionDisponible(s, ahora) || quedan <= 0) return false;
    quedan -= 1;
    return true;
  });
}

/** Marca o desmarca una fecha. No deja marcar una que no está disponible ni pasarse del cupo. */
export function alternarMarca(
  marcas: readonly boolean[],
  indice: number,
  sesiones: readonly SesionFlyer[],
  cupo: number | null,
  ahora: string,
): boolean[] {
  const nuevas = [...marcas];
  if (nuevas[indice]) {
    nuevas[indice] = false;
    return nuevas;
  }
  if (!sesiones[indice] || !sesionDisponible(sesiones[indice], ahora)) return nuevas;
  if (cupo !== null && nuevas.filter(Boolean).length >= cupo) return nuevas;
  nuevas[indice] = true;
  return nuevas;
}

export const sesionesMarcadas = (sesiones: readonly SesionFlyer[], marcas: readonly boolean[]): SesionFlyer[] =>
  sesiones.filter((_, i) => marcas[i]);

export const tituloSesiones = (total: number): string => `Detectamos ${total} fechas`;

/** Qué dice la pantalla sobre el límite diario. `null` si no hay límite (admin). */
export function textoCupo(total: number, cupo: number | null): string | null {
  if (cupo === null) return null;
  if (cupo === 0)
    return `Hoy ya creaste ${MAXIMO_EVENTOS_POR_DIA} eventos, el máximo por día. Vuelve mañana para publicar estas fechas.`;
  const cuantos = cupo === 1 ? "1 evento más" : `${cupo} eventos más`;
  if (cupo >= total) return `Hoy puedes crear ${cuantos}: caben todas las fechas.`;
  const elige = cupo === 1 ? "cuál publicar" : "cuáles publicar";
  return `Hoy puedes crear ${cuantos} (máximo ${MAXIMO_EVENTOS_POR_DIA} por día). Elige ${elige}.`;
}

const DIAS = ["dom", "lun", "mar", "mié", "jue", "vie", "sáb"];
const MESES = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic"];

/** «2026-11-17T18:00» + fin → «mar 17 nov · 18:00 – 20:00». Sin Date con zona: no mueve la hora. */
export function textoSesion(s: SesionFlyer): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}:\d{2})/.exec(s.inicio);
  if (!m) return s.inicio;
  const dia = DIAS[new Date(Date.UTC(Number(m[1]), Number(m[2]) - 1, Number(m[3]))).getUTCDay()];
  const fecha = `${dia} ${Number(m[3])} ${MESES[Number(m[2]) - 1]} · ${m[4]}`;
  const fin = s.fin === null ? undefined : /T(\d{2}:\d{2})/.exec(s.fin)?.[1];
  if (!fin) return fecha;
  const otroDia = s.fin !== null && s.fin.slice(0, 10) !== s.inicio.slice(0, 10);
  return otroDia ? `${fecha} – ${fin} (día siguiente)` : `${fecha} – ${fin}`;
}

/** Los mismos datos del formulario con la fecha de una sesión (título, lugar, asistencia y foto no cambian). */
export function camposDeSesion<T extends { inicio: string; fin: string }>(campos: T, sesion: SesionFlyer): T {
  return { ...campos, inicio: sesion.inicio, fin: sesion.fin ?? campos.fin };
}

// ── Resultado de crear un evento por fecha ─────────────────────────

export type TonoResultado = "publicado" | "revision" | "error";
export type ResultadoSesion = { sesion: SesionFlyer; tono: TonoResultado; texto: string };

export function resultadoCreado(sesion: SesionFlyer, estado: string, motivo: string | null): ResultadoSesion {
  if (estado === "PUBLICADO") return { sesion, tono: "publicado", texto: "Publicado." };
  if (estado === "PENDIENTE") return { sesion, tono: "revision", texto: `Quedó en revisión.${motivo ? ` ${motivo}` : ""}` };
  return { sesion, tono: "error", texto: `Se guardó con estado ${estado.toLowerCase()}.` };
}

/** Un 409 de duplicado (HU33) o cualquier otro error muestra el mensaje del servidor; el límite diario, uno propio. */
export function resultadoFallido(sesion: SesionFlyer, codigo: string | undefined, mensaje: string | undefined): ResultadoSesion {
  if (codigo === "LIMITE_DIARIO")
    return { sesion, tono: "error", texto: `No se creó: ya llegaste al máximo de ${MAXIMO_EVENTOS_POR_DIA} eventos por día.` };
  return { sesion, tono: "error", texto: `No se creó: ${mensaje?.trim() || "ocurrió un error inesperado. Inténtalo de nuevo."}` };
}

/** Resumen de una línea: «Se crearon 2 de 3 eventos: 1 publicado y 1 en revisión.» */
export function resumenResultados(resultados: readonly ResultadoSesion[]): string {
  const publicados = resultados.filter((r) => r.tono === "publicado").length;
  const revision = resultados.filter((r) => r.tono === "revision").length;
  const creados = publicados + revision;
  const total = resultados.length;
  if (creados === 0) return total === 1 ? "No se pudo crear el evento." : `No se pudo crear ninguno de los ${total} eventos.`;
  const partes: string[] = [];
  if (publicados > 0) partes.push(`${publicados} ${publicados === 1 ? "publicado" : "publicados"}`);
  if (revision > 0) partes.push(`${revision} en revisión`);
  let cabeza: string;
  if (creados < total) cabeza = `Se ${creados === 1 ? "creó" : "crearon"} ${creados} de ${total} eventos`;
  else if (total === 1) cabeza = "Se creó el evento";
  else cabeza = `Se crearon los ${total} eventos`;
  return `${cabeza}: ${partes.join(" y ")}.`;
}

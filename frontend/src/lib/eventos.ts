import { pedirApi } from "./api";
import { EstadoFlyer, EventoLeido, FLYER_BYTES_MAX, FLYER_TIPOS } from "./reglasFlyer";
import type { SesionFlyer } from "./reglasSesionesFlyer";

export { FLYER_BYTES_MAX, FLYER_TIPOS };
export type { EstadoFlyer, SesionFlyer };

/** Borrador de evento que devuelve el servidor. inicio y fin van en hora de Bolivia, sin zona: «yyyy-MM-ddTHH:mm». */
export type BorradorEvento = EventoLeido;

/** Página 1 del PDF en JPEG; solo existe si el origen fue un PDF y cabe en 2 MB. */
export type ImagenFlyer = { tipoMime: string; contenidoBase64: string; bytes: number };

export type LecturaFlyer = {
  consultaId: number;
  estado: EstadoFlyer;
  mensaje: string | null;
  evento: BorradorEvento;
  faltantes: string[];
  advertencias: string[];
  /** HU34: fechas futuras del mismo evento (máximo 5, ordenadas); la primera es la de `evento`. Una fecha → un elemento. */
  sesiones?: SesionFlyer[];
  imagen: ImagenFlyer | null;
};

export type LugarEvento = { id: number; nombre: string };
export type EventoEnPuente = {
  id: number;
  titulo: string;
  inicio: string;
  fin: string;
  lugar: string;
  asistencia: "FIJA" | "LIBRE";
  minutosPuedeQuedarse: number | null;
  explicacion: string;
};

export type EventoCampus = {
  id: number;
  titulo: string;
  descripcion: string | null;
  inicio: string;
  fin: string;
  lugarId: number | null;
  lugar: string;
  estado: "PENDIENTE" | "PUBLICADO" | "RECHAZADO" | "CANCELADO";
  origen: "FORMULARIO" | "FLYER" | "ADMIN";
  asistencia: "FIJA" | "LIBRE";
  visitaMin: number | null;
  visitaMax: number | null;
  motivo: string | null;
  fotoUrl: string | null;
  createdAt: string;
  /** HU33: aviso del servidor cuando un admin publica un evento que coincide con otro. */
  aviso?: string | null;
};

/** HU26: datos corregidos por el admin al aprobar. Mismos campos que el formulario de HU23, sin la foto. */
export type CambiosEventoApi = {
  titulo: string;
  descripcion: string | null;
  inicio: string;
  fin: string;
  lugarId: number | null;
  lugarTexto: string | null;
  asistencia: "FIJA" | "LIBRE" | "";
  visitaMin: number | null;
  visitaMax: number | null;
};

/** Aprobar o rechazar un pendiente. Con `cambios`, el admin lo edita y lo aprueba en un solo paso. */
export type DecidirEvento = { estado: "PUBLICADO" | "RECHAZADO"; motivo?: string | null; cambios?: CambiosEventoApi | null };

/** HU24: lee un afiche (JPG, PNG, PDF) o un texto pegado. No crea nada: solo devuelve el borrador. */
export function leerFlyer(entrada: File | string): Promise<LecturaFlyer> {
  const datos = new FormData();
  if (typeof entrada === "string") datos.append("texto", entrada);
  else datos.append("archivo", entrada);
  return pedirApi<LecturaFlyer>("/api/eventos/extraer", { method: "POST", body: datos });
}

export const listarLugaresEventos = () => pedirApi<LugarEvento[]>("/api/eventos/lugares");
export const listarEventosEnPuente = () => pedirApi<EventoEnPuente[]>("/api/eventos/en-puente");
export const listarMisEventos = () => pedirApi<EventoCampus[]>("/api/eventos/mios");
export const crearEvento = (datos: FormData) => pedirApi<EventoCampus>("/api/eventos", { method: "POST", body: datos });
export const actualizarEvento = (id: number, datos: FormData) =>
  pedirApi<EventoCampus>(`/api/eventos/${id}`, { method: "PUT", body: datos });
export const cancelarEvento = (id: number) =>
  pedirApi<EventoCampus>(`/api/eventos/${id}/cancelar`, { method: "PATCH" });
export const listarEventosPendientes = () => pedirApi<EventoCampus[]>("/api/admin/eventos/pendientes");
export const moderarEvento = (id: number, decision: DecidirEvento) =>
  pedirApi<void>(`/api/admin/eventos/${id}/moderacion`, {
    method: "PATCH",
    body: JSON.stringify(decision),
  });
/** HU26: los publicados que todavía no terminaron, para poder bajarlos. */
export const listarEventosPublicadosAdmin = () => pedirApi<EventoCampus[]>("/api/admin/eventos/publicados");
/** HU26: historial de rechazados. `fueBajado` = estaba publicado y el admin lo bajó; si no, se rechazó en la revisión. */
export type EventoRechazado = { evento: EventoCampus; fueBajado: boolean; admin: string | null; rechazadoEn: string };
export const listarEventosRechazadosAdmin = () => pedirApi<EventoRechazado[]>("/api/admin/eventos/rechazados");
/** HU26: baja un evento publicado. Queda oculto (RECHAZADO, no se borra) y el autor recibe un aviso con el motivo. */
export const bajarEvento = (id: number, motivo: string) =>
  pedirApi<void>(`/api/admin/eventos/${id}/bajar`, {
    method: "PATCH",
    body: JSON.stringify({ motivo }),
  });

/** HU22: evento publicado que ve cualquier estudiante. No trae estado ni motivo: eso es solo de moderacion. */
export type EventoPublico = {
  id: number;
  titulo: string;
  descripcion: string | null;
  inicio: string;
  fin: string;
  lugarId: number | null;
  lugar: string;
  fueraDelCampus: boolean;
  asistencia: "FIJA" | "LIBRE";
  visitaMin: number | null;
  visitaMax: number | null;
  fotoUrl: string | null;
};

export type RangoEventos = "hoy" | "semana";

export const listarEventosDelCampus = (rango: RangoEventos) =>
  pedirApi<EventoPublico[]>(`/api/eventos?rango=${rango}`);
export const obtenerEventoDelCampus = (id: number) => pedirApi<EventoPublico>(`/api/eventos/${id}`);

export const FOTO_EVENTO_MAX_BYTES = 2 * 1024 * 1024;
const FOTO_EVENTO_TIPOS = ["image/jpeg", "image/png"];

export type CamposFormularioEvento = {
  titulo: string;
  inicio: string;
  fin: string;
  lugarId: string;
  lugarTexto: string;
  /** «» = sin elegir (un borrador de flyer sin asistencia leída): hay que elegirla antes de enviar. */
  asistencia: "FIJA" | "LIBRE" | "";
  visitaMin: string;
  visitaMax: string;
};

export function validarFormularioEvento(campos: CamposFormularioEvento): string | null {
  if (!campos.titulo.trim()) return "Escribe el título del evento.";
  if (campos.titulo.trim().length > 120) return "El título no puede superar 120 caracteres.";
  if (!campos.inicio) return "Indica la fecha y hora de inicio.";
  if (!fechaHoraCompleta(campos.inicio)) return "Completa la fecha y la hora de inicio.";
  if (!campos.fin) return "Indica la fecha y hora de fin.";
  if (!fechaHoraCompleta(campos.fin)) return "Completa la fecha y la hora de fin.";
  if (!campos.lugarId && !campos.lugarTexto.trim()) return "Elige un lugar o escribe dónde será el evento.";
  if (campos.lugarTexto.trim().length > 150) return "El lugar no puede superar 150 caracteres.";
  if (campos.asistencia === "") return "Elige si hay que llegar a la hora o si se puede pasar un rato.";

  if (campos.asistencia === "LIBRE") {
    const minimo = Number(campos.visitaMin);
    const maximo = Number(campos.visitaMax);
    if (!campos.visitaMin || !campos.visitaMax) return "Indica el tiempo mínimo y máximo de visita.";
    if (!Number.isInteger(minimo) || !Number.isInteger(maximo) || minimo < 1 || maximo < minimo || maximo > 825)
      return "El tiempo de visita debe estar entre 1 y 825 minutos y el máximo no puede ser menor al mínimo.";
  }

  return null;
}

export function validarFotoEvento(archivo: Pick<File, "name" | "type" | "size">): string | null {
  const extensionValida = /\.(jpe?g|png)$/i.test(archivo.name);
  if (!FOTO_EVENTO_TIPOS.includes(archivo.type) && !(archivo.type === "" && extensionValida))
    return "La foto del evento debe ser JPG o PNG.";
  if (archivo.size === 0) return "La foto seleccionada está vacía.";
  if (archivo.size > FOTO_EVENTO_MAX_BYTES) return "La foto no puede superar los 2 MB.";
  return null;
}

export function fechaBoliviaParaApi(valor: string): string {
  return valor ? `${valor}:00-04:00` : "";
}

export function fechaBoliviaParaFormulario(valor: string): string {
  return valor.slice(0, 16);
}

export function ahoraBoliviaParaFormulario(ahora = new Date()): string {
  return new Date(ahora.getTime() - 4 * 60 * 60 * 1000).toISOString().slice(0, 16);
}

// Fecha y hora con los selectores de la app (sin el control nativo): el campo guarda «AAAA-MM-DDTHH:mm» y, mientras
// falta una de las dos partes, «AAAA-MM-DDT» o «THH:mm» (así no se pierde lo que ya eligió).
const FECHA_HORA = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/;

export const fechaHoraCompleta = (valor: string): boolean => FECHA_HORA.test(valor);

export function partirFechaHora(valor: string): { fecha: string; hora: string } {
  const [fecha = "", hora = ""] = valor.split("T");
  return { fecha, hora: hora.slice(0, 5) };
}

export function unirFechaHora(fecha: string, hora: string): string {
  return fecha || hora ? `${fecha}T${hora}` : "";
}

const DIAS_CORTOS = ["dom", "lun", "mar", "mié", "jue", "vie", "sáb"];
const MESES_CORTOS = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];

/** «mié 19 nov» (con el año si no es el de `hoy`). Las fechas son de calendario: se leen en UTC para no correrse de día. */
export function etiquetaFecha(fecha: string, hoy: string): string {
  const d = new Date(`${fecha}T00:00:00Z`);
  const base = `${DIAS_CORTOS[d.getUTCDay()]} ${d.getUTCDate()} ${MESES_CORTOS[d.getUTCMonth()]}`;
  return fecha.slice(0, 4) === hoy.slice(0, 4) ? base : `${base} ${fecha.slice(0, 4)}`;
}

/**
 * Días para elegir, desde `hoy` (fecha de Bolivia) y `dias` hacia adelante: «Hoy · mar 17 nov», «Mañana · …», «jue 19 nov».
 * `extra` (la fecha de un evento que se edita o de un afiche) se agrega en orden si queda fuera de la lista.
 */
export function opcionesDeFecha(hoy: string, dias = 120, extra = ""): { valor: string; etiqueta: string }[] {
  const inicio = Date.parse(`${hoy}T00:00:00Z`);
  const dia = (i: number) => new Date(inicio + i * 86_400_000).toISOString().slice(0, 10);
  const fechas = Array.from({ length: dias }, (_, i) => dia(i));
  if (/^\d{4}-\d{2}-\d{2}$/.test(extra) && !fechas.includes(extra)) fechas.push(extra);
  fechas.sort((a, b) => a.localeCompare(b));
  const relativos = new Map([[dia(0), "Hoy"], [dia(1), "Mañana"]]);
  return fechas.map((fecha) => {
    const etiqueta = etiquetaFecha(fecha, hoy);
    const relativo = relativos.get(fecha);
    return { valor: fecha, etiqueta: relativo ? `${relativo} · ${etiqueta}` : etiqueta };
  });
}

/**
 * Por qué el evento pasaría por revisión, igual que `ReglasEvento.DecidirPublicacion` del backend (sin el filtro de
 * contenido, que solo corre en el servidor): lugar fuera del catálogo, horario fuera de 07:15–21:00 o en dos días, o
 * inicio que ya pasó. `ahora` en formato de formulario (hora de Bolivia).
 */
export function motivosDeRevision(inicio: string, fin: string, lugarDelCatalogo: boolean, lugarEscrito: boolean, ahora: string): string[] {
  const motivos: string[] = [];
  if (fechaHoraCompleta(inicio) && inicio <= ahora) motivos.push("La fecha de inicio ya pasó.");
  if (fechaHoraCompleta(inicio) && fechaHoraCompleta(fin)) {
    const a = partirFechaHora(inicio);
    const b = partirFechaHora(fin);
    if (a.fecha !== b.fecha || a.hora < "07:15" || b.hora > "21:00") motivos.push("Es fuera del horario del campus (07:15 a 21:00).");
  }
  if (!lugarDelCatalogo && lugarEscrito) motivos.push("El lugar no está en el catálogo.");
  return motivos;
}

/** «mié 19 nov, 17:00 – 18:30» o, si termina otro día, con las dos fechas. Vacío si falta algo. */
export function rangoLegible(inicio: string, fin: string, hoy: string): string {
  if (!fechaHoraCompleta(inicio)) return "";
  const a = partirFechaHora(inicio);
  const desde = `${etiquetaFecha(a.fecha, hoy)}, ${a.hora}`;
  if (!fechaHoraCompleta(fin)) return desde;
  const b = partirFechaHora(fin);
  return b.fecha === a.fecha ? `${desde} – ${b.hora}` : `${desde} – ${etiquetaFecha(b.fecha, hoy)}, ${b.hora}`;
}

export function formDataEvento(
  campos: CamposFormularioEvento & { descripcion: string },
  foto: File | null,
  nombreLugarSeleccionado?: string,
  /** Solo al crear: FLYER si el formulario salió de un borrador de afiche. Al editar no se envía. */
  origen?: "FORMULARIO" | "FLYER",
  /** Solo al editar: true pide borrar la foto actual del evento (no se envía junto con una foto nueva). */
  quitarFoto = false,
): FormData {
  const datos = new FormData();
  if (origen) datos.set("Origen", origen);
  if (quitarFoto && !foto) datos.set("QuitarFoto", "true");
  datos.set("Titulo", campos.titulo.trim());
  datos.set("Descripcion", campos.descripcion.trim());
  datos.set("Inicio", fechaBoliviaParaApi(campos.inicio));
  datos.set("Fin", fechaBoliviaParaApi(campos.fin));
  datos.set("LugarId", campos.lugarId);
  datos.set("LugarTexto", campos.lugarId ? nombreLugarSeleccionado ?? "" : campos.lugarTexto.trim());
  datos.set("Asistencia", campos.asistencia);
  datos.set("VisitaMin", campos.asistencia === "LIBRE" ? campos.visitaMin : "");
  datos.set("VisitaMax", campos.asistencia === "LIBRE" ? campos.visitaMax : "");
  if (foto) datos.set("Foto", foto);
  return datos;
}

export type EstadoEvento = "PENDIENTE" | "PUBLICADO" | "RECHAZADO" | "CANCELADO";

export type PresentacionEvento = {
  clave: EstadoEvento | "VENCIDO";
  etiqueta: string;
  /** Solo se edita un evento PENDIENTE o PUBLICADO que todavía no empezó. */
  editable: boolean;
  /** Se puede cancelar mientras no esté cancelado ni haya empezado. */
  cancelable: boolean;
};

const ETIQUETAS_EVENTO: Record<EstadoEvento, string> = {
  PENDIENTE: "En revisión",
  PUBLICADO: "Publicado",
  RECHAZADO: "Rechazado",
  CANCELADO: "Cancelado",
};

/**
 * Cómo se muestra un evento propio. Un PENDIENTE cuyo fin ya pasó se ve como «Vencido»:
 * es solo de presentación, el estado en el servidor no cambia.
 */
export function presentacionEvento(
  estado: EstadoEvento,
  inicio: string,
  fin: string,
  ahoraMs: number,
): PresentacionEvento {
  const empezo = new Date(inicio).getTime() <= ahoraMs;
  const vencido = estado === "PENDIENTE" && new Date(fin).getTime() <= ahoraMs;
  return {
    clave: vencido ? "VENCIDO" : estado,
    etiqueta: vencido ? "Vencido" : ETIQUETAS_EVENTO[estado],
    editable: (estado === "PENDIENTE" || estado === "PUBLICADO") && !empezo,
    cancelable: estado !== "CANCELADO" && !empezo,
  };
}

/** HU26: el formulario de «Editar y aprobar» del admin, con los datos actuales del evento. */
export function camposDesdeEvento(evento: {
  titulo: string;
  descripcion: string | null;
  inicio: string;
  fin: string;
  lugarId: number | null;
  lugar: string;
  asistencia: "FIJA" | "LIBRE";
  visitaMin: number | null;
  visitaMax: number | null;
}): CamposFormularioEvento & { descripcion: string } {
  return {
    titulo: evento.titulo,
    descripcion: evento.descripcion ?? "",
    inicio: fechaBoliviaParaFormulario(evento.inicio),
    fin: fechaBoliviaParaFormulario(evento.fin),
    lugarId: evento.lugarId === null ? "" : String(evento.lugarId),
    lugarTexto: evento.lugarId === null ? evento.lugar : "",
    asistencia: evento.asistencia,
    visitaMin: evento.visitaMin === null ? "" : String(evento.visitaMin),
    visitaMax: evento.visitaMax === null ? "" : String(evento.visitaMax),
  };
}

/**
 * HU26: los cambios del admin en JSON para `PATCH /api/admin/eventos/{id}/moderacion` (el formulario del estudiante va en
 * FormData por la foto; aquí no hay foto). Con un lugar del catálogo se manda su nombre como respaldo, como en HU23.
 */
export function cambiosEventoParaApi(campos: CamposFormularioEvento & { descripcion: string }, nombreLugarSeleccionado?: string) {
  const libre = campos.asistencia === "LIBRE";
  return {
    titulo: campos.titulo.trim(),
    descripcion: campos.descripcion.trim() || null,
    inicio: fechaBoliviaParaApi(campos.inicio),
    fin: fechaBoliviaParaApi(campos.fin),
    lugarId: campos.lugarId ? Number(campos.lugarId) : null,
    lugarTexto: campos.lugarId ? nombreLugarSeleccionado ?? null : campos.lugarTexto.trim() || null,
    asistencia: campos.asistencia,
    visitaMin: libre && campos.visitaMin ? Number(campos.visitaMin) : null,
    visitaMax: libre && campos.visitaMax ? Number(campos.visitaMax) : null,
  };
}

/** HU26: el motivo para rechazar o bajar es obligatorio y llega como mucho a 500 caracteres. */
export function validarMotivoModeracion(motivo: string, accion: "rechazar" | "bajar"): string | null {
  const limpio = motivo.trim();
  if (!limpio) return accion === "rechazar" ? "Escribe el motivo del rechazo." : "Escribe el motivo para bajar el evento.";
  if (limpio.length > 500) return "El motivo no puede superar 500 caracteres.";
  return null;
}

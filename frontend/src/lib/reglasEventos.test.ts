import assert from "node:assert/strict";
import { test } from "node:test";
import {
  ahoraBoliviaParaFormulario,
  cambiosEventoParaApi,
  camposDesdeEvento,
  CODIGO_EVENTO_DUPLICADO,
  esMotivoDuplicado,
  mensajeErrorEvento,
  validarMotivoModeracion,
  etiquetaFecha,
  fechaHoraCompleta,
  motivosDeRevision,
  opcionesDeFecha,
  partirFechaHora,
  rangoLegible,
  unirFechaHora,
  fechaBoliviaParaApi,
  formDataEvento,
  presentacionEvento,
  validarFormularioEvento,
  validarFotoEvento,
} from "./reglasEventos.ts";

const CAMPOS = {
  titulo: "Feria de ciencias",
  inicio: "2026-11-17T13:00",
  fin: "2026-11-17T14:00",
  lugarId: "3",
  lugarTexto: "",
  asistencia: "FIJA" as const,
  visitaMin: "",
  visitaMax: "",
};

test("valida los obligatorios del formulario sin bloquear fechas que el servidor enviará a revisión", () => {
  assert.equal(validarFormularioEvento(CAMPOS), null);
  assert.match(validarFormularioEvento({ ...CAMPOS, titulo: " " }) ?? "", /título/i);
  assert.match(validarFormularioEvento({ ...CAMPOS, lugarId: "", lugarTexto: "" }) ?? "", /lugar/i);
});

test("exige un rango coherente de visita solo para asistencia libre", () => {
  assert.match(validarFormularioEvento({ ...CAMPOS, asistencia: "LIBRE" }) ?? "", /tiempo mínimo/i);
  assert.equal(validarFormularioEvento({ ...CAMPOS, asistencia: "LIBRE", visitaMin: "15", visitaMax: "60" }), null);
  assert.match(validarFormularioEvento({ ...CAMPOS, asistencia: "LIBRE", visitaMin: "60", visitaMax: "15" }) ?? "", /tiempo de visita/i);
});

test("adjunta el offset fijo de Bolivia y formatea la hora actual en ese huso", () => {
  assert.equal(fechaBoliviaParaApi(CAMPOS.inicio), "2026-11-17T13:00:00-04:00");
  assert.equal(ahoraBoliviaParaFormulario(new Date("2026-11-17T16:00:00.000Z")), "2026-11-17T12:00");
});

test("comprueba tipo, tamaño y contenido opcional de la foto", () => {
  assert.equal(validarFotoEvento({ name: "afiche.jpg", type: "image/jpeg", size: 10 }), null);
  assert.match(validarFotoEvento({ name: "afiche.gif", type: "image/gif", size: 10 }) ?? "", /JPG o PNG/);
  assert.match(validarFotoEvento({ name: "afiche.png", type: "image/png", size: 2 * 1024 * 1024 + 1 }) ?? "", /2 MB/);
});

test("construye multipart con valores completos y omite fotos vacías", () => {
  const datos = formDataEvento({ ...CAMPOS, descripcion: "Descripción" }, null, "Biblioteca");

  assert.equal(datos.get("Titulo"), CAMPOS.titulo);
  assert.equal(datos.get("Inicio"), "2026-11-17T13:00:00-04:00");
  assert.equal(datos.get("LugarTexto"), "Biblioteca");
  assert.equal(datos.get("Foto"), null);
});

const AHORA = Date.parse("2026-11-17T12:00:00-04:00");
const FUTURO = { inicio: "2026-11-18T10:00:00-04:00", fin: "2026-11-18T12:00:00-04:00" };
const PASADO = { inicio: "2026-11-17T08:00:00-04:00", fin: "2026-11-17T10:00:00-04:00" };

test("solo se puede editar un evento PENDIENTE o PUBLICADO que no empezó", () => {
  assert.equal(presentacionEvento("PENDIENTE", FUTURO.inicio, FUTURO.fin, AHORA).editable, true);
  assert.equal(presentacionEvento("PUBLICADO", FUTURO.inicio, FUTURO.fin, AHORA).editable, true);
  assert.equal(presentacionEvento("RECHAZADO", FUTURO.inicio, FUTURO.fin, AHORA).editable, false);
  assert.equal(presentacionEvento("CANCELADO", FUTURO.inicio, FUTURO.fin, AHORA).editable, false);
});

test("un evento que ya empezó no se edita ni se cancela", () => {
  const p = presentacionEvento("PUBLICADO", "2026-11-17T10:00:00-04:00", "2026-11-17T14:00:00-04:00", AHORA);
  assert.equal(p.editable, false);
  assert.equal(p.cancelable, false);
  assert.equal(p.etiqueta, "Publicado");
});

test("cancelar sigue disponible salvo en CANCELADO o si ya empezó", () => {
  assert.equal(presentacionEvento("RECHAZADO", FUTURO.inicio, FUTURO.fin, AHORA).cancelable, true);
  assert.equal(presentacionEvento("CANCELADO", FUTURO.inicio, FUTURO.fin, AHORA).cancelable, false);
});

test("un PENDIENTE cuyo fin ya pasó se muestra como Vencido", () => {
  const p = presentacionEvento("PENDIENTE", PASADO.inicio, PASADO.fin, AHORA);
  assert.equal(p.clave, "VENCIDO");
  assert.equal(p.etiqueta, "Vencido");
  assert.equal(p.editable, false);
  assert.equal(p.cancelable, false);
});

test("un PENDIENTE vigente sigue en revisión y los demás estados no se marcan Vencido", () => {
  assert.equal(presentacionEvento("PENDIENTE", FUTURO.inicio, FUTURO.fin, AHORA).etiqueta, "En revisión");
  assert.equal(presentacionEvento("PUBLICADO", PASADO.inicio, PASADO.fin, AHORA).clave, "PUBLICADO");
  assert.equal(presentacionEvento("RECHAZADO", PASADO.inicio, PASADO.fin, AHORA).etiqueta, "Rechazado");
});

test("fecha y hora: se arman por partes y solo valen completas", () => {
  assert.equal(unirFechaHora("2026-11-20", ""), "2026-11-20T");
  assert.equal(unirFechaHora("", "17:00"), "T17:00");
  assert.equal(unirFechaHora("", ""), "");
  assert.deepEqual(partirFechaHora("2026-11-20T17:30"), { fecha: "2026-11-20", hora: "17:30" });
  assert.deepEqual(partirFechaHora("T09:05"), { fecha: "", hora: "09:05" });
  assert.equal(fechaHoraCompleta("2026-11-20T17:30"), true);
  assert.equal(fechaHoraCompleta("2026-11-20T"), false);
  assert.equal(fechaHoraCompleta("T17:30"), false);
});

test("el formulario pide completar la fecha o la hora que falta", () => {
  assert.equal(validarFormularioEvento({ ...CAMPOS, inicio: "2026-11-17T" }), "Completa la fecha y la hora de inicio.");
  assert.equal(validarFormularioEvento({ ...CAMPOS, fin: "T14:00" }), "Completa la fecha y la hora de fin.");
});

test("días para elegir: hoy, mañana y los siguientes; la fecha de un evento viejo se agrega en orden", () => {
  const opciones = opcionesDeFecha("2026-11-17", 3);
  assert.deepEqual(opciones.map((o) => o.etiqueta), ["Hoy · mar 17 nov", "Mañana · mié 18 nov", "jue 19 nov"]);
  const conExtra = opcionesDeFecha("2026-11-17", 2, "2026-11-10");
  assert.deepEqual(conExtra.map((o) => o.valor), ["2026-11-10", "2026-11-17", "2026-11-18"]);
  assert.equal(opcionesDeFecha("2026-11-17", 2, "2026-11-18").length, 2);
  assert.equal(etiquetaFecha("2027-01-04", "2026-11-17"), "lun 4 ene 2027");
});

test("rango legible para la vista previa", () => {
  assert.equal(rangoLegible("2026-11-19T17:00", "2026-11-19T18:30", "2026-11-17"), "jue 19 nov, 17:00 – 18:30");
  assert.equal(rangoLegible("2026-11-19T22:00", "2026-11-20T01:00", "2026-11-17"), "jue 19 nov, 22:00 – vie 20 nov, 01:00");
  assert.equal(rangoLegible("2026-11-19T17:00", "", "2026-11-17"), "jue 19 nov, 17:00");
  assert.equal(rangoLegible("2026-11-19T", "", "2026-11-17"), "");
});

test("motivos de revisión, como en el backend", () => {
  const ahora = "2026-11-17T10:00";
  assert.deepEqual(motivosDeRevision("2026-11-18T17:00", "2026-11-18T18:00", true, false, ahora), []);
  assert.deepEqual(motivosDeRevision("2026-11-18T17:00", "2026-11-18T18:00", false, true, ahora), ["El lugar no está en el catálogo."]);
  assert.deepEqual(motivosDeRevision("2026-11-18T07:00", "2026-11-18T08:00", true, false, ahora), ["Es fuera del horario del campus (07:15 a 21:00)."]);
  assert.deepEqual(motivosDeRevision("2026-11-18T20:00", "2026-11-18T21:30", true, false, ahora), ["Es fuera del horario del campus (07:15 a 21:00)."]);
  assert.deepEqual(motivosDeRevision("2026-11-18T20:00", "2026-11-19T09:00", true, false, ahora), ["Es fuera del horario del campus (07:15 a 21:00)."]);
  assert.deepEqual(motivosDeRevision("2026-11-17T09:00", "2026-11-17T11:00", true, false, ahora), ["La fecha de inicio ya pasó."]);
  // Sin lugar todavía (ni del catálogo ni escrito) o con la fecha a medias, no se adelanta nada.
  assert.deepEqual(motivosDeRevision("2026-11-18T", "", false, false, ahora), []);
});

test("HU26: editar y aprobar arranca con los datos actuales del evento", () => {
  const campos = camposDesdeEvento({
    titulo: "Feria",
    descripcion: null,
    inicio: "2026-11-20T10:00:00-04:00",
    fin: "2026-11-20T12:00:00-04:00",
    lugarId: null,
    lugar: "Patio",
    asistencia: "LIBRE",
    visitaMin: 10,
    visitaMax: 30,
  });
  assert.deepEqual(campos, {
    titulo: "Feria",
    descripcion: "",
    inicio: "2026-11-20T10:00",
    fin: "2026-11-20T12:00",
    lugarId: "",
    lugarTexto: "Patio",
    asistencia: "LIBRE",
    visitaMin: "10",
    visitaMax: "30",
  });
});

test("HU26: los cambios del admin van en JSON con la hora de Bolivia y sin visita si es fija", () => {
  const cambios = cambiosEventoParaApi(
    { ...CAMPOS, descripcion: "  ", lugarId: "3", lugarTexto: "", asistencia: "FIJA", visitaMin: "5", visitaMax: "9" },
    "Biblioteca",
  );
  assert.equal(cambios.descripcion, null);
  assert.equal(cambios.lugarId, 3);
  assert.equal(cambios.lugarTexto, "Biblioteca");
  assert.equal(cambios.visitaMin, null);
  assert.equal(cambios.visitaMax, null);
  assert.match(cambios.inicio, /-04:00$/);
});

test("HU26: rechazar o bajar exige un motivo de hasta 500 caracteres", () => {
  assert.equal(validarMotivoModeracion("   ", "rechazar"), "Escribe el motivo del rechazo.");
  assert.equal(validarMotivoModeracion("", "bajar"), "Escribe el motivo para bajar el evento.");
  assert.equal(validarMotivoModeracion("x".repeat(501), "bajar"), "El motivo no puede superar 500 caracteres.");
  assert.equal(validarMotivoModeracion("Es spam", "bajar"), null);
});

test("HU33: el 409 de duplicado explica el rechazo y nombra el evento existente", () => {
  const servidor = "Ya existe un evento muy parecido: «Feria de ciencias» (evento #7). Revisa el título.";
  const mensaje = mensajeErrorEvento(CODIGO_EVENTO_DUPLICADO, servidor);

  assert.match(mensaje, /duplicado/i);
  assert.match(mensaje, /Feria de ciencias/);
  assert.match(mensaje, /#7/);
  assert.equal(mensajeErrorEvento("OTRO", "Mensaje del servidor"), "Mensaje del servidor");
  assert.equal(mensajeErrorEvento(undefined, undefined), "No se pudo guardar el evento. Inténtalo de nuevo.");
});

test("HU33: el motivo de posible duplicado se reconoce como cualquier otro motivo de PENDIENTE", () => {
  assert.equal(esMotivoDuplicado("Posible duplicado de «Feria de ciencias» (17/11/2026)."), true);
  assert.equal(esMotivoDuplicado("  Posible duplicado de «Concierto» (18/11/2026)."), true);
  assert.equal(esMotivoDuplicado("El lugar no figura en el catálogo."), false);
  assert.equal(esMotivoDuplicado(null), false);
});

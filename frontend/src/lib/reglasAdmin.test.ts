import assert from "node:assert/strict";
import { test } from "node:test";
import {
  accionModeracionDeReporte,
  accionValidacionLugar,
  ETIQUETA_ACCION_VALIDACION,
  ETIQUETA_CONFIRMAR_DESVALIDAR,
  mensajeValidacionLugar,
  pideConfirmacionValidacion,
  TEXTO_CONFIRMAR_DESVALIDAR,
  TEXTOS_MODERACION_REPORTE,
  tituloConfirmarDesvalidar,
} from "./reglasAdmin.ts";

const reporte = (r: object) => ({ estado: "PENDIENTE", objetoId: 7, ...r });

test("HU38: «Bajar evento» solo aparece en un reporte pendiente de un evento PUBLICADO", () => {
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "EVENTO", objetoEstado: "PUBLICADO" })), "bajar");
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "EVENTO", objetoEstado: "RECHAZADO" })), null);
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "EVENTO", objetoEstado: "PENDIENTE" })), null);
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "EVENTO" })), null);
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "EVENTO", objetoEstado: "PUBLICADO", estado: "RESUELTO" })), null);
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "EVENTO", objetoEstado: "PUBLICADO", objetoId: null })), null);
});

test("HU35: «Quitar comunidad» sigue saliendo en un reporte pendiente de una comunidad, y los demás reportes no ofrecen nada", () => {
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "COMUNIDAD", objetoEstado: "ACTIVA" })), "quitar");
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "COMUNIDAD", estado: "DESCARTADO" })), null);
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: "LUGAR" })), null);
  assert.equal(accionModeracionDeReporte(reporte({ objetoTipo: null })), null);
});

test("HU38: los textos del formulario de moderación dicen que se baja, se resuelve el reporte y no se sanciona", () => {
  assert.equal(TEXTOS_MODERACION_REPORTE.bajar.boton, "Bajar evento");
  assert.equal(TEXTOS_MODERACION_REPORTE.quitar.boton, "Quitar comunidad");
  for (const t of Object.values(TEXTOS_MODERACION_REPORTE)) {
    assert.match(t.pista, /obligatorio/);
    assert.match(t.ayuda, /no sanciona/);
    assert.match(t.ayuda, /resuelto/);
  }
});

test("HU16: quitar la validación pide confirmación y validar no", () => {
  assert.equal(pideConfirmacionValidacion(accionValidacionLugar(true)), true);
  assert.equal(pideConfirmacionValidacion(accionValidacionLugar(false)), false);
});

test("HU16: la confirmación explica el efecto y nombra el lugar", () => {
  assert.equal(
    TEXTO_CONFIRMAR_DESVALIDAR,
    "Al quitar la validación, los eventos nuevos en este lugar quedarán en revisión.",
  );
  assert.equal(ETIQUETA_CONFIRMAR_DESVALIDAR, "Quitar validación");
  assert.equal(tituloConfirmarDesvalidar("Biblioteca"), "¿Quitar la validación de «Biblioteca»?");
});

test("HU16: el botón cambia según el estado de validación del lugar", () => {
  assert.equal(accionValidacionLugar(false), "validar");
  assert.equal(accionValidacionLugar(true), "desvalidar");
  assert.equal(ETIQUETA_ACCION_VALIDACION.validar, "Validar");
  assert.equal(ETIQUETA_ACCION_VALIDACION.desvalidar, "Desvalidar");
});

test("HU16: el aviso dice qué pasó con la validación en los dos sentidos", () => {
  assert.equal(mensajeValidacionLugar("Biblioteca", true), "Biblioteca: validado.");
  assert.equal(mensajeValidacionLugar("Biblioteca", false), "Biblioteca: sin validar.");
});

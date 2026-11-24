import assert from "node:assert/strict";
import { test } from "node:test";
import {
  accionValidacionLugar,
  ETIQUETA_ACCION_VALIDACION,
  ETIQUETA_CONFIRMAR_DESVALIDAR,
  mensajeValidacionLugar,
  pideConfirmacionValidacion,
  TEXTO_CONFIRMAR_DESVALIDAR,
  tituloConfirmarDesvalidar,
} from "./reglasAdmin.ts";

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

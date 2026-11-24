import assert from "node:assert/strict";
import { test } from "node:test";
import { accionValidacionLugar, ETIQUETA_ACCION_VALIDACION, mensajeValidacionLugar } from "./reglasAdmin.ts";

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

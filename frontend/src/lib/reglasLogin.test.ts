import assert from "node:assert/strict";
import { test } from "node:test";
import { MENSAJE_CAMPOS_LOGIN, validarLogin } from "./reglasLogin.ts";

test("HU1: falta el correo, falta la clave o faltan los dos", () => {
  assert.equal(validarLogin("", "breaku2026"), MENSAJE_CAMPOS_LOGIN);
  assert.equal(validarLogin("   ", "breaku2026"), MENSAJE_CAMPOS_LOGIN);
  assert.equal(validarLogin("ana@ucb.edu.bo", ""), MENSAJE_CAMPOS_LOGIN);
  assert.equal(validarLogin("ana@ucb.edu.bo", "   "), MENSAJE_CAMPOS_LOGIN);
  assert.equal(validarLogin("", ""), MENSAJE_CAMPOS_LOGIN);
});

test("HU1: el mensaje es el literal del criterio de aceptación y con datos no bloquea", () => {
  assert.equal(MENSAJE_CAMPOS_LOGIN, "Por favor, complete todos los campos");
  assert.equal(validarLogin("ana@ucb.edu.bo", "breaku2026"), null);
});

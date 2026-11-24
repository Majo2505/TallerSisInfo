import assert from "node:assert/strict";
import { test } from "node:test";
import { puedeRestaurarComunidad, TEXTO_COMUNIDAD_TERMINADA } from "./reglasComunidadesAdmin.ts";

const AHORA = new Date("2026-11-24T15:00:00Z");

test("HU35: se restaura una comunidad quitada solo si todavía no terminó", () => {
  assert.equal(puedeRestaurarComunidad("2026-11-24T12:30:00-04:00", AHORA), true); // 16:30 UTC, falta 1 h 30 min
  assert.equal(puedeRestaurarComunidad("2026-11-24T11:00:00-04:00", AHORA), false); // termina justo ahora
  assert.equal(puedeRestaurarComunidad("2026-11-24T10:00:00-04:00", AHORA), false); // ya terminó
  assert.equal(puedeRestaurarComunidad("no es una fecha", AHORA), false);
});

test("HU35: el texto de una comunidad terminada explica por qué no hay botón", () => {
  assert.equal(TEXTO_COMUNIDAD_TERMINADA, "Ya terminó: no hay nada que restaurar.");
});

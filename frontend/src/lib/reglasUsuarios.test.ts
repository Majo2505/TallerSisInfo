import assert from "node:assert/strict";
import { test } from "node:test";
import {
  ayudaFechaFin,
  confirmacionBajaValida,
  mensajeDeLoginBloqueado,
  motivoDelCorte401,
  sePuedeLevantar,
  sugiereSuspension,
  validarSancion,
  aceptaSanciones,
  hastaParaApi,
} from "./reglasUsuarios.ts";

const AHORA = new Date("2026-11-13T15:00:00");

test("tres advertencias vigentes sugieren suspensión; con menos, no", () => {
  assert.equal(sugiereSuspension(2), false);
  assert.equal(sugiereSuspension(3), true);
  assert.equal(sugiereSuspension(5), true);
});

test("la baja se confirma escribiendo BAJA o el nombre, sin importar mayúsculas ni espacios en los bordes", () => {
  assert.equal(confirmacionBajaValida("BAJA", "Ana Pérez"), true);
  assert.equal(confirmacionBajaValida("  baja ", "Ana Pérez"), true);
  assert.equal(confirmacionBajaValida("ana pérez", "Ana Pérez"), true);
  assert.equal(confirmacionBajaValida("", "Ana Pérez"), false);
  assert.equal(confirmacionBajaValida("Ana", "Ana Pérez"), false);
  assert.equal(confirmacionBajaValida("   ", "   "), false);
});

test("el motivo es obligatorio y tiene tope de 500 caracteres", () => {
  assert.match(validarSancion("ADVERTENCIA", "   ", "", AHORA) ?? "", /motivo/i);
  assert.match(validarSancion("BAJA", "x".repeat(501), "", AHORA) ?? "", /500/);
  assert.equal(validarSancion("ADVERTENCIA", "Lenguaje ofensivo", "", AHORA), null);
});

test("la suspensión sin fecha vale (7 días por defecto) y con fecha tiene que ser futura", () => {
  assert.equal(validarSancion("SUSPENSION", "Acoso", "", AHORA), null);
  assert.equal(validarSancion("SUSPENSION", "Acoso", "2026-11-20T10:00", AHORA), null);
  assert.match(validarSancion("SUSPENSION", "Acoso", "2026-11-12T10:00", AHORA) ?? "", /posterior/);
  assert.match(validarSancion("SUSPENSION", "Acoso", "no-es-fecha", AHORA) ?? "", /válida/);
});

test("el texto de ayuda dice que sin fecha durará 7 días", () => {
  assert.match(ayudaFechaFin(""), /7 días por defecto/);
  assert.doesNotMatch(ayudaFechaFin("2026-11-20T10:00"), /7 días/);
});

test("la fecha de fin se manda en UTC y vacía va como null", () => {
  assert.equal(hastaParaApi(""), null);
  assert.match(hastaParaApi("2026-11-20T10:00") ?? "", /^2026-11-20T\d{2}:00:00\.000Z$|^2026-11-2[01]T/);
});

test("solo se levantan advertencias sin levantar y suspensiones vigentes; la baja no", () => {
  assert.equal(sePuedeLevantar({ tipo: "ADVERTENCIA", levantadaEn: null, vigente: false }), true);
  assert.equal(sePuedeLevantar({ tipo: "ADVERTENCIA", levantadaEn: "2026-11-01", vigente: false }), false);
  assert.equal(sePuedeLevantar({ tipo: "SUSPENSION", levantadaEn: null, vigente: true }), true);
  assert.equal(sePuedeLevantar({ tipo: "SUSPENSION", levantadaEn: null, vigente: false }), false);
  assert.equal(sePuedeLevantar({ tipo: "BAJA", levantadaEn: null, vigente: true }), false);
});

test("una cuenta dada de baja ya no recibe sanciones", () => {
  assert.equal(aceptaSanciones("ACTIVO"), true);
  assert.equal(aceptaSanciones("SUSPENDIDO"), true);
  assert.equal(aceptaSanciones("BAJA"), false);
});

test("el mensaje de cuenta bloqueada del login se muestra tal como viene (403)", () => {
  const msg = "Tu cuenta está suspendida hasta el 20/11/2026 11:00. Motivo: Acoso";
  assert.equal(mensajeDeLoginBloqueado(403, { message: msg }), msg);
  assert.equal(mensajeDeLoginBloqueado(401, { message: "Credenciales inválidas." }), null);
  assert.equal(mensajeDeLoginBloqueado(403, null), null);
});

test("un 401 con el token sin vencer es por la cuenta; con el token vencido, sesión vencida", () => {
  const ahora = Date.parse("2026-11-13T15:00:00Z");
  assert.equal(motivoDelCorte401(ahora + 3_600_000, ahora), "CUENTA_RECHAZADA");
  assert.equal(motivoDelCorte401(ahora - 1, ahora), "VENCIDA");
  assert.equal(motivoDelCorte401(null, ahora), "CUENTA_RECHAZADA");
});

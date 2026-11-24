import assert from "node:assert/strict";
import { test } from "node:test";
import { CLAVE_AVISO_UBICACION, debeMostrarAvisoUbicacion } from "./reglasUbicacion.ts";

test("HU5: el aviso solo aparece si aún no hay zona ni consentimiento y no se descartó", () => {
  assert.equal(debeMostrarAvisoUbicacion({ consentimientoActivo: false, zonaActual: null }, false), true);
  assert.equal(debeMostrarAvisoUbicacion({ consentimientoActivo: false, zonaActual: null }, true), false);
  assert.equal(debeMostrarAvisoUbicacion({ consentimientoActivo: true, zonaActual: null }, false), false);
  assert.equal(debeMostrarAvisoUbicacion(null, false), false);
});

test("HU5: elegir el campus a mano o descartar el aviso no vuelve a preguntar", () => {
  assert.equal(debeMostrarAvisoUbicacion({ consentimientoActivo: false, zonaActual: { id: 3 } }, false), false);
  assert.equal(debeMostrarAvisoUbicacion({ consentimientoActivo: false, zonaActual: null }, true), false);
  assert.equal(CLAVE_AVISO_UBICACION, "breaku:aviso-ubicacion-visto");
});

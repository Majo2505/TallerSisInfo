import assert from "node:assert/strict";
import { test } from "node:test";
import {
  alternarMarca,
  camposDeSesion,
  cupoDeHoy,
  marcasIniciales,
  resultadoCreado,
  resultadoFallido,
  resumenResultados,
  sesionDisponible,
  sesionesMarcadas,
  textoCupo,
  textoSesion,
  tituloSesiones,
  type SesionFlyer,
} from "./reglasSesionesFlyer.ts";

// 2026-11-17 14:00 UTC = 10:00 en Bolivia
const AHORA = new Date("2026-11-17T14:00:00Z");
const AHORA_FORM = "2026-11-17T10:00";
const s = (dia: number, fin: string | null = "20:00"): SesionFlyer => ({
  inicio: `2026-11-${dia}T18:00`,
  fin: fin === null ? null : `2026-11-${dia}T${fin}`,
});
const tres = [s(18), s(20), s(25)];

test("el cupo cuenta los eventos creados hoy en hora de Bolivia", () => {
  const eventos = [
    { createdAt: "2026-11-17T13:00:00Z" }, // 09:00 Bolivia, hoy
    { createdAt: "2026-11-17T03:30:00Z" }, // 23:30 del 16 en Bolivia: ayer
    { createdAt: "2026-11-16T15:00:00-04:00" },
  ];
  assert.equal(cupoDeHoy(eventos, AHORA, false), 2);
  assert.equal(cupoDeHoy([...eventos, ...eventos, ...eventos], AHORA, false), 0);
  assert.equal(cupoDeHoy(eventos, AHORA, true), null); // admin sin límite
});

test("al abrir «revisar» se marcan las futuras hasta donde alcanza el cupo", () => {
  assert.deepEqual(marcasIniciales(tres, 3, AHORA_FORM), [true, true, true]);
  assert.deepEqual(marcasIniciales(tres, 2, AHORA_FORM), [true, true, false]);
  assert.deepEqual(marcasIniciales(tres, 0, AHORA_FORM), [false, false, false]);
  assert.deepEqual(marcasIniciales(tres, null, AHORA_FORM), [true, true, true]);
  // una fecha que ya empezó o sin hora de fin no se marca
  assert.deepEqual(marcasIniciales([{ inicio: "2026-11-17T09:00", fin: "2026-11-17T11:00" }, s(19, null), s(20)], 3, AHORA_FORM), [false, false, true]);
});

test("se puede desmarcar y volver a marcar sin pasarse del cupo", () => {
  let marcas = marcasIniciales(tres, 2, AHORA_FORM);
  marcas = alternarMarca(marcas, 2, tres, 2, AHORA_FORM);
  assert.deepEqual(marcas, [true, true, false]); // el cupo de 2 ya está lleno
  marcas = alternarMarca(marcas, 0, tres, 2, AHORA_FORM);
  marcas = alternarMarca(marcas, 2, tres, 2, AHORA_FORM);
  assert.deepEqual(marcas, [false, true, true]); // elige cuáles
  assert.deepEqual(sesionesMarcadas(tres, marcas), [s(20), s(25)]);
  assert.deepEqual(alternarMarca([false, false], 0, [s(19, null), s(20)], null, AHORA_FORM), [false, false]);
  assert.equal(sesionDisponible(s(18), AHORA_FORM), true);
});

test("la pantalla dice cuántas fechas caben hoy", () => {
  assert.equal(tituloSesiones(3), "Detectamos 3 fechas");
  assert.equal(textoCupo(3, null), null);
  assert.equal(textoCupo(3, 3), "Hoy puedes crear 3 eventos más: caben todas las fechas.");
  assert.equal(textoCupo(3, 1), "Hoy puedes crear 1 evento más (máximo 3 por día). Elige cuál publicar.");
  assert.equal(textoCupo(3, 2), "Hoy puedes crear 2 eventos más (máximo 3 por día). Elige cuáles publicar.");
  assert.match(textoCupo(2, 0)!, /Vuelve mañana/);
});

test("cada fecha se muestra con día, hora de inicio y de fin", () => {
  assert.equal(textoSesion(s(17)), "mar 17 nov · 18:00 – 20:00");
  assert.equal(textoSesion({ inicio: "2026-12-05T22:00", fin: "2026-12-06T01:00" }), "sáb 5 dic · 22:00 – 01:00 (día siguiente)");
  assert.equal(textoSesion(s(19, null)), "jue 19 nov · 18:00");
});

test("cada evento lleva los mismos datos con la fecha de su sesión", () => {
  const campos = { titulo: "Ciclo ODS", inicio: "2026-11-18T18:00", fin: "2026-11-18T20:00", lugarId: "3" };
  assert.deepEqual(camposDeSesion(campos, s(25)), { ...campos, inicio: "2026-11-25T18:00", fin: "2026-11-25T20:00" });
});

test("el resultado de cada evento tiene un mensaje claro", () => {
  assert.equal(resultadoCreado(s(18), "PUBLICADO", null).tono, "publicado");
  assert.equal(resultadoCreado(s(18), "PENDIENTE", "El lugar no está en el catálogo.").texto, "Quedó en revisión. El lugar no está en el catálogo.");
  assert.equal(resultadoFallido(s(18), "LIMITE_DIARIO", "x").texto, "No se creó: ya llegaste al máximo de 3 eventos por día.");
  // 409 de duplicados (HU33): se muestra lo que dice el servidor
  assert.equal(resultadoFallido(s(18), "EVENTO_DUPLICADO", "Ya existe un evento parecido ese día.").texto, "No se creó: Ya existe un evento parecido ese día.");
  assert.equal(resultadoFallido(s(18), "DUPLICADO", "Ya existe un evento igual ese día.").texto, "No se creó: Ya existe un evento igual ese día.");
  assert.match(resultadoFallido(s(18), "EVENTO_DUPLICADO", undefined).texto, /evento muy parecido/);
  assert.match(resultadoFallido(s(18), undefined, undefined).texto, /error inesperado/);
});

test("el resumen cuenta publicados, en revisión y fallidos", () => {
  const ok = resultadoCreado(s(18), "PUBLICADO", null);
  const rev = resultadoCreado(s(20), "PENDIENTE", null);
  const mal = resultadoFallido(s(25), "LIMITE_DIARIO", undefined);
  assert.equal(resumenResultados([ok, rev, mal]), "Se crearon 2 de 3 eventos: 1 publicado y 1 en revisión.");
  assert.equal(resumenResultados([ok, ok]), "Se crearon los 2 eventos: 2 publicados.");
  assert.equal(resumenResultados([rev, mal]), "Se creó 1 de 2 eventos: 1 en revisión.");
  assert.equal(resumenResultados([mal, mal]), "No se pudo crear ninguno de los 2 eventos.");
});

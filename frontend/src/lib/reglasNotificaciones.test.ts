import assert from "node:assert/strict";
import { test } from "node:test";
import { PRESENTACION_GENERICA, bandejaVacia, presentacionNotificacion, rutaNotificacion } from "./reglasNotificaciones.ts";

test("los avisos del admin tienen su presentación y llevan a donde se resuelven", () => {
  assert.equal(presentacionNotificacion("REPORTE_NUEVO").titulo, "Nuevo reporte");
  assert.equal(presentacionNotificacion("EVENTO_POR_REVISAR").icono, "calendario");
  assert.equal(rutaNotificacion({ tipo: "REPORTE_NUEVO", reporteId: 4 }), "/admin?pestana=reportes");
  assert.equal(rutaNotificacion({ tipo: "EVENTO_POR_REVISAR" }), "/admin/eventos");
});

test("los avisos del estudiante conservan su destino", () => {
  assert.equal(rutaNotificacion({ tipo: "PUENTE_DISPONIBLE", duracionMinutos: 60 }), "/recomendaciones?min=60");
  assert.equal(rutaNotificacion({ tipo: "INVITACION_COMUNIDAD", comunidadId: 7 }), "/comunidades?pestana=descubrir&comunidad=7");
  assert.equal(rutaNotificacion({ tipo: "EVENTO_APROBADO" }), "/eventos?pestana=mios");
  assert.equal(rutaNotificacion({ tipo: "SANCION_ADVERTENCIA" }), null);
});

test("quien creó la comunidad ve quién se sumó o se bajó y va a su comunidad", () => {
  assert.deepEqual(presentacionNotificacion("ASISTENCIA_COMUNIDAD"), {
    titulo: "Cambió quién va a tu grupo",
    clase: "invitacion",
    icono: "personas",
  });
  assert.equal(rutaNotificacion({ tipo: "ASISTENCIA_COMUNIDAD", comunidadId: 12 }), "/comunidades?pestana=descubrir&comunidad=12");
});

test("la invitación de una conexión a un evento lleva a su detalle", () => {
  assert.equal(presentacionNotificacion("INVITACION_EVENTO").icono, "calendario");
  assert.equal(rutaNotificacion({ tipo: "INVITACION_EVENTO", eventoId: 14 }), "/eventos/14");
  assert.equal(rutaNotificacion({ tipo: "INVITACION_EVENTO" }), "/eventos");
});

test("la bandeja vacía explica «Sin leer» y le habla al admin de lo suyo", () => {
  assert.equal(bandejaVacia("sin-leer", false).titulo, "Ya leíste todo");
  assert.match(bandejaVacia("todas", true).detalle, /reporte/);
  assert.match(bandejaVacia("todas", false).detalle, /puentes/);
});

test("un evento aprobado se muestra con su título, ícono de calendario y estilo de éxito", () => {
  assert.deepEqual(presentacionNotificacion("EVENTO_APROBADO"), {
    titulo: "Tu evento fue aprobado",
    clase: "reporte",
    icono: "calendario",
  });
});

test("un evento rechazado se muestra con el estilo de aviso", () => {
  assert.deepEqual(presentacionNotificacion("EVENTO_RECHAZADO"), {
    titulo: "Tu evento fue rechazado",
    clase: "advertencia",
    icono: "calendario",
  });
});

test("la advertencia de sanción conserva su presentación", () => {
  assert.deepEqual(presentacionNotificacion("SANCION_ADVERTENCIA"), {
    titulo: "Advertencia del administrador",
    clase: "advertencia",
    icono: "bandera",
  });
});

test("un tipo desconocido usa la presentación genérica y no lanza error", () => {
  assert.deepEqual(presentacionNotificacion("TIPO_NUEVO_DE_OTRA_HU"), PRESENTACION_GENERICA);
  assert.deepEqual(presentacionNotificacion(""), PRESENTACION_GENERICA);
  assert.equal(PRESENTACION_GENERICA.titulo, "Nuevo aviso");
});

test("los nombres heredados de Object no se toman por tipos conocidos", () => {
  assert.deepEqual(presentacionNotificacion("constructor"), PRESENTACION_GENERICA);
  assert.deepEqual(presentacionNotificacion("toString"), PRESENTACION_GENERICA);
});

test("HU26: el evento bajado por el admin se ve como advertencia y lleva a Mis eventos", () => {
  assert.deepEqual(presentacionNotificacion("EVENTO_BAJADO"), {
    titulo: "Tu evento fue retirado",
    clase: "advertencia",
    icono: "calendario",
  });
  assert.equal(rutaNotificacion({ tipo: "EVENTO_BAJADO", eventoId: 4 }), "/eventos?pestana=mios");
});

test("HU32: las alertas al admin tienen título e ícono propios y llevan al detalle del estudiante", () => {
  assert.deepEqual(presentacionNotificacion("ALERTA_USUARIO_SOSPECHOSO"), {
    titulo: "Intentos bloqueados por seguridad",
    clase: "advertencia",
    icono: "bandera",
  });
  assert.equal(presentacionNotificacion("ALERTA_REPORTES_ACUMULADOS").titulo, "Reportes acumulados");
  assert.equal(
    rutaNotificacion({ tipo: "ALERTA_USUARIO_SOSPECHOSO", usuarioObjetivoId: 7 }),
    "/admin?pestana=usuarios&usuario=7",
  );
  assert.equal(
    rutaNotificacion({ tipo: "ALERTA_REPORTES_ACUMULADOS", usuarioObjetivoId: 9 }),
    "/admin?pestana=usuarios&usuario=9",
  );
  assert.equal(rutaNotificacion({ tipo: "ALERTA_REPORTES_ACUMULADOS" }), "/admin?pestana=alertas");
});

test("HU35: quitar y restaurar una comunidad tienen su presentación y su destino", () => {
  assert.equal(presentacionNotificacion("COMUNIDAD_BAJADA").titulo, "Retiraron una comunidad");
  assert.equal(presentacionNotificacion("COMUNIDAD_BAJADA").clase, "advertencia");
  assert.equal(presentacionNotificacion("COMUNIDAD_RESTAURADA").titulo, "Una comunidad volvió");
  assert.equal(rutaNotificacion({ tipo: "COMUNIDAD_BAJADA", comunidadId: 4 }), null);
  assert.equal(rutaNotificacion({ tipo: "COMUNIDAD_RESTAURADA", comunidadId: 4 }), "/comunidades?pestana=descubrir&comunidad=4");
});


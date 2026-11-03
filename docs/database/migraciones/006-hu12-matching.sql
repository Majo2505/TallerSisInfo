-- HU12: matching de estudiantes con puente compatible. Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · usuario.visible_matching: el estudiante acepta que compañeros de su sede con tiempo libre compatible lo encuentren.
--     Es un dato personal (cuándo está libre), así que arranca en FALSE: nadie aparece sin haberlo aceptado.
--   · usuario.fecha_visible_matching: cuándo lo aceptó. Obligatoria mientras visible_matching = TRUE (misma idea que HU5).
--   · ix_usuario_matching: el cruce filtra a los compañeros por sede y por visibilidad; el lado de los puentes ya usa
--     ix_puente_matching (dia_semana, hora_inicio, hora_fin).
ALTER TABLE usuario
  ADD COLUMN visible_matching       BOOLEAN     NOT NULL DEFAULT FALSE AFTER notificaciones_activas,
  ADD COLUMN fecha_visible_matching DATETIME(6) NULL AFTER visible_matching,
  ADD KEY ix_usuario_matching (zona_actual_id, visible_matching),
  ADD CONSTRAINT ck_usuario_visible_matching CHECK (visible_matching = FALSE OR fecha_visible_matching IS NOT NULL);

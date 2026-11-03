-- Lleva una base creada con la versión anterior de schema.sql a la actual (sede activa y datos del relevamiento de lugares).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · zona.activa: solo las sedes activas aparecen para elegir y reciben recomendaciones. Las inactivas se conservan.
--     Todas las sedes existentes quedan activas; para cambiar de sede se usa seed-dev-cochabamba.sql o un UPDATE.
--   · lugar: campos nuevos del relevamiento, todos opcionales (NULL) salvo recomendable y validado.
--       tipo_lugar, modalidad y nivel_energia: texto; modalidad y nivel_energia con valores separados por coma.
--       duracion_min / duracion_max: rango de tiempo del lugar, en minutos.
--       actividades_sugeridas: lista JSON de textos.
--       min_a_pie_desde_bloque_a: minutos a pie desde el bloque A.
--       recomendable: FALSE para puntos de referencia (campus, parqueo, bloques): se guardan pero nunca se recomiendan.
--       validado: FALSE mientras el equipo no revise los datos. Los lugares que ya existían quedan validados.
ALTER TABLE zona
  ADD COLUMN activa BOOLEAN NOT NULL DEFAULT TRUE AFTER radio_max_m;

ALTER TABLE lugar
  ADD COLUMN tipo_lugar VARCHAR(30) NULL AFTER estado,
  ADD COLUMN modalidad VARCHAR(40) NULL AFTER tipo_lugar,
  ADD COLUMN nivel_energia VARCHAR(40) NULL AFTER modalidad,
  ADD COLUMN duracion_min SMALLINT NULL AFTER nivel_energia,
  ADD COLUMN duracion_max SMALLINT NULL AFTER duracion_min,
  ADD COLUMN actividades_sugeridas TEXT NULL AFTER duracion_max,
  ADD COLUMN min_a_pie_desde_bloque_a SMALLINT NULL AFTER actividades_sugeridas,
  ADD COLUMN recomendable BOOLEAN NOT NULL DEFAULT TRUE AFTER min_a_pie_desde_bloque_a,
  ADD COLUMN validado BOOLEAN NOT NULL DEFAULT TRUE AFTER recomendable,
  ADD KEY ix_lugar_recomendable (recomendable, estado),
  ADD CONSTRAINT ck_lugar_rango_duracion CHECK (duracion_min IS NULL OR duracion_max IS NULL OR duracion_min <= duracion_max);

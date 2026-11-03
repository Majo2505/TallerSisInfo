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

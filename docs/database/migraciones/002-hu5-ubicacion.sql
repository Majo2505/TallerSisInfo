-- Lleva una base creada con la versión anterior de schema.sql a la actual (HU5: ubicación y campus).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name", la migración ya estaba aplicada.
ALTER TABLE usuario
  ADD COLUMN zona_actual_id INT NULL AFTER fecha_consentimiento,
  ADD KEY ix_usuario_zona_actual (zona_actual_id),
  DROP CHECK ck_usuario_consentimiento,
  ADD CONSTRAINT ck_usuario_consentimiento CHECK (
    consentimiento_ubicacion = FALSE OR (fecha_consentimiento IS NOT NULL AND zona_actual_id IS NOT NULL)
  ),
  ADD CONSTRAINT fk_usuario_zona_actual FOREIGN KEY (zona_actual_id) REFERENCES zona (id);

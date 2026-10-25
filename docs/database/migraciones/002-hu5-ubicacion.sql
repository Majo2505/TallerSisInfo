-- Upgrade for databases created with the previous schema.sql.
-- Do not run this after creating a fresh database from the updated schema.sql.
ALTER TABLE usuario
  ADD COLUMN zona_actual_id INT NULL AFTER fecha_consentimiento,
  ADD KEY ix_usuario_zona_actual (zona_actual_id),
  DROP CHECK ck_usuario_consentimiento,
  ADD CONSTRAINT ck_usuario_consentimiento CHECK (
    consentimiento_ubicacion = FALSE OR (fecha_consentimiento IS NOT NULL AND zona_actual_id IS NOT NULL)
  ),
  ADD CONSTRAINT fk_usuario_zona_actual FOREIGN KEY (zona_actual_id) REFERENCES zona (id);

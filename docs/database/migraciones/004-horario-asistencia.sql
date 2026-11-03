ALTER TABLE horario
  ADD COLUMN asiste BOOLEAN NOT NULL DEFAULT TRUE AFTER origen,
  ADD COLUMN autorizacion_choque BOOLEAN NOT NULL DEFAULT FALSE AFTER asiste,
  ADD CONSTRAINT uq_horario_bloque UNIQUE (usuario_id, dia_semana, hora_inicio, materia);

ALTER TABLE horario
  DROP INDEX uq_horario_slot;

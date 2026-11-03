-- Lleva una base creada con la versión anterior de schema.sql a la actual (horario con choques y casilla «asisto»).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · asiste: FALSE cuando el estudiante no asiste a ese bloque; no cuenta como tiempo ocupado.
--   · autorizacion_choque: el estudiante confirmó que su carrera autoriza que dos clases coincidan.
--   · La clave única ya no impide dos materias a la misma hora: ahora incluye la materia.
--     Primero se crea la nueva y después se quita la anterior, porque la clave foránea a usuario necesita un índice.
ALTER TABLE horario
  ADD COLUMN asiste BOOLEAN NOT NULL DEFAULT TRUE AFTER origen,
  ADD COLUMN autorizacion_choque BOOLEAN NOT NULL DEFAULT FALSE AFTER asiste,
  ADD CONSTRAINT uq_horario_bloque UNIQUE (usuario_id, dia_semana, hora_inicio, materia);

ALTER TABLE horario
  DROP INDEX uq_horario_slot;

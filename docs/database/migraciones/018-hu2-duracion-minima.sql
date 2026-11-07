-- HU2: una clase dura al menos 45 minutos. Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate check constraint name 'ck_horario_duracion'", la migración ya estaba aplicada.
--
-- Con el rango de 07:15 a 21:00 (migración 008), esto deja que una clase empiece como muy tarde a las 20:15 y termine
-- como muy temprano a las 08:00.
--
-- Antes de correrla, revisar (debería dar 0 filas; si da alguna, corregir esas clases a mano o la migración falla
-- con "Check constraint 'ck_horario_duracion' is violated"):
--   SELECT id, usuario_id, materia, dia_semana, hora_inicio, hora_fin FROM horario
--   WHERE TIME_TO_SEC(TIMEDIFF(hora_fin, hora_inicio)) < 2700;

ALTER TABLE horario
  ADD CONSTRAINT ck_horario_duracion CHECK (TIME_TO_SEC(TIMEDIFF(hora_fin, hora_inicio)) >= 2700);

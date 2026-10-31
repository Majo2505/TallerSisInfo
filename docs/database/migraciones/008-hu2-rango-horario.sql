-- HU2: las clases de la UCB empiezan desde las 07:15 y terminan, como muy tarde, a las 21:00.
-- Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate check constraint name", la migración ya estaba aplicada.
--
-- Si falla con "Check constraint 'ck_horario_rango' is violated", hay clases guardadas fuera del rango.
-- Para verlas:
--   SELECT id, usuario_id, materia, dia_semana, hora_inicio, hora_fin FROM horario
--   WHERE hora_inicio < '07:15:00' OR hora_fin > '21:00:00';
-- Corrígelas (o bórralas) desde la app o con un UPDATE, y vuelve a ejecutar este script.
ALTER TABLE horario
  ADD CONSTRAINT ck_horario_rango CHECK (hora_inicio >= '07:15:00' AND hora_fin <= '21:00:00');

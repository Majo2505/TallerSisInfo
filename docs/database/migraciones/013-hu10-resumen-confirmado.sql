-- HU10: lo que se crea es exactamente lo que el estudiante confirmó. Lleva una base creada con la versión anterior de
-- schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'datos_extraidos'", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · log_auditoria_rag.datos_extraidos: el resumen validado de cada extracción (HU11), con la IA o corregido a mano:
--     {"actividad": "jugar cartas", "horaInicio": "16:00", "duracionMin": 60, "maxParticipantes": 4}.
--     POST /api/actividades crea la actividad con ESTOS datos: antes usaba los que mandaba el navegador, así que con el
--     consultaId de un resumen se podía crear otra actividad distinta. Solo en extracciones (ck_log_datos_tipo).
--   · comunidad_efimera: las reglas de la extracción como última barrera, igual que ck_horario_rango en HU2:
--       ck_com_duracion  de 10 a 240 minutos;
--       ck_com_horario   de 07:15 a 21:00 en hora de Bolivia (inicio y fin se guardan en UTC, Bolivia es UTC-4) y el mismo día.
--
-- Antes de correrla, esta consulta debe dar 0 filas (actividades que no cumplen las reglas nuevas):
--   SELECT id, inicio, fin FROM comunidad_efimera
--   WHERE TIMESTAMPDIFF(MINUTE, inicio, fin) NOT BETWEEN 10 AND 240
--      OR TIME(inicio - INTERVAL 4 HOUR) < '07:15:00' OR TIME(fin - INTERVAL 4 HOUR) > '21:00:00'
--      OR DATE(inicio - INTERVAL 4 HOUR) <> DATE(fin - INTERVAL 4 HOUR);

ALTER TABLE log_auditoria_rag
  ADD COLUMN datos_extraidos JSON NULL AFTER motivo_rechazo,
  ADD CONSTRAINT ck_log_datos_tipo CHECK (datos_extraidos IS NULL OR tipo = 'EXTRACCION_COMUNIDAD');

ALTER TABLE comunidad_efimera
  ADD CONSTRAINT ck_com_duracion CHECK (TIMESTAMPDIFF(MINUTE, inicio, fin) BETWEEN 10 AND 240),
  ADD CONSTRAINT ck_com_horario  CHECK (TIME(inicio - INTERVAL 4 HOUR) >= '07:15:00'
                                        AND TIME(fin - INTERVAL 4 HOUR) <= '21:00:00'
                                        AND DATE(inicio - INTERVAL 4 HOUR) = DATE(fin - INTERVAL 4 HOUR));

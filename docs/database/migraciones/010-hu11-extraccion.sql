-- HU11 (#61): extracción estructurada de actividades. Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
--
-- Cada extracción queda en log_auditoria_rag con tipo EXTRACCION_COMUNIDAD (ya permitido por ck_log_tipo).
-- Se agrega el estado INCOMPLETO: se entendió la actividad, pero falta algún dato y se le pregunta al estudiante.
-- Requiere MySQL 8.0.19 o superior (DROP CHECK).

ALTER TABLE log_auditoria_rag
  DROP CHECK ck_log_estado,
  ADD CONSTRAINT ck_log_estado CHECK (estado IN ('OK','SIN_RESULTADOS','RECHAZADO_SEGURIDAD','ERROR','INCOMPLETO'));

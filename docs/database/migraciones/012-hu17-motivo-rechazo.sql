-- HU17: motivo del rechazo de seguridad en la auditoría del asistente. Lleva una base creada con la versión anterior
-- de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'motivo_rechazo'", la migración ya estaba aplicada.
--
-- Hasta ahora, una consulta rechazada solo quedaba con estado RECHAZADO_SEGURIDAD: el admin no podía distinguir un intento
-- de prompt injection de alguien que pidió «una chela». Ahora se guarda la categoría del guardrail que la rechazó:
--   · INYECCION            intentó cambiar las instrucciones del asistente o ver su prompt.
--   · FUERA_DE_ALCANCE     pidió datos internos (contraseñas, tokens, código, datos personales).
--   · BIENESTAR            mencionó riesgo para sí mismo: se le deriva a bienestar universitario.
--   · FUERA_DE_REGLAMENTO  alcohol, drogas, apuestas, contenido sexual, violencia, deshonestidad académica.
--   · FUERA_DE_CONTEXTO    la IA respondió con lugares o textos que no salen del catálogo (se descartó su respuesta).
-- Los rechazos anteriores a esta migración quedan sin motivo (NULL): no hay forma de saber cuál fue.

ALTER TABLE log_auditoria_rag
  ADD COLUMN motivo_rechazo VARCHAR(20) NULL AFTER estado,
  ADD KEY ix_log_motivo (motivo_rechazo, created_at),
  ADD CONSTRAINT ck_log_motivo CHECK (motivo_rechazo IN ('INYECCION','FUERA_DE_ALCANCE','BIENESTAR','FUERA_DE_REGLAMENTO','FUERA_DE_CONTEXTO')),
  ADD CONSTRAINT ck_log_motivo_estado CHECK (motivo_rechazo IS NULL OR estado = 'RECHAZADO_SEGURIDAD');

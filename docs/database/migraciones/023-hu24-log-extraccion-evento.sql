SET NAMES utf8mb4;

-- Leer un flyer o un texto y devolver un borrador de evento (HU24). Lleva una base creada con la versión anterior de
-- schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Es seguro correrla dos veces: solo vuelve a poner los mismos CHECK.
--
-- Qué cambia (se amplía, no se restringe):
--   · ck_log_tipo: log_auditoria_rag admite el tipo EXTRACCION_EVENTO, para auditar cada lectura de un flyer.
--   · ck_log_datos_tipo: datos_extraidos (el borrador validado) puede guardarse también con EXTRACCION_EVENTO.

ALTER TABLE log_auditoria_rag
  DROP CHECK ck_log_tipo,
  DROP CHECK ck_log_datos_tipo,
  ADD CONSTRAINT ck_log_tipo       CHECK (tipo IN ('RECOMENDACION','EXTRACCION_COMUNIDAD','EXTRACCION_EVENTO')),
  ADD CONSTRAINT ck_log_datos_tipo CHECK (datos_extraidos IS NULL OR tipo IN ('EXTRACCION_COMUNIDAD','EXTRACCION_EVENTO'));

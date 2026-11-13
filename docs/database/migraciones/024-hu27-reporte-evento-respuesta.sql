SET NAMES utf8mb4;

-- Reportes completos (HU27): respuesta del admin y reportes de eventos. Lleva una base creada con la versión anterior de
-- schema.sql a la actual. Requiere la 022 (reporte.evento_id apunta a evento).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'evento_id'", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · reporte.evento_id: el evento reportado. Sin ON DELETE (la columna está en un CHECK; MySQL no permite acciones de FK
--     sobre ellas): el borrado de los reportes lo hace de forma explícita el backend.
--   · reporte.respuesta: la nota opcional del admin al resolver o descartar (hasta 500 caracteres). Solo puede existir si el
--     reporte ya no está PENDIENTE. NULL en los reportes que ya existen.
--   · ck_rep_motivo: se agrega EVENTO_INAPROPIADO.
--   · ck_rep_objeto: cada motivo lleva solo su objeto: un lugar, una comunidad o un evento; «algo no funciona» y las
--     sugerencias, ninguno. Los reportes que ya existen siguen siendo válidos.

ALTER TABLE reporte
  ADD COLUMN evento_id INT NULL AFTER comunidad_id,
  ADD COLUMN respuesta TEXT NULL,
  DROP CHECK ck_rep_motivo,
  DROP CHECK ck_rep_objeto,
  ADD CONSTRAINT ck_rep_motivo CHECK (motivo IN ('ERROR_APP','SUGERENCIA','LUGAR_INCORRECTO','COMUNIDAD_INAPROPIADA','EVENTO_INAPROPIADO')),
  ADD CONSTRAINT ck_rep_objeto CHECK ((motivo = 'LUGAR_INCORRECTO'      AND lugar_id IS NOT NULL AND comunidad_id IS NULL AND evento_id IS NULL)
                                   OR (motivo = 'COMUNIDAD_INAPROPIADA' AND comunidad_id IS NOT NULL AND lugar_id IS NULL AND evento_id IS NULL)
                                   OR (motivo = 'EVENTO_INAPROPIADO'    AND evento_id IS NOT NULL AND lugar_id IS NULL AND comunidad_id IS NULL)
                                   OR (motivo IN ('ERROR_APP','SUGERENCIA') AND lugar_id IS NULL AND comunidad_id IS NULL AND evento_id IS NULL)),
  ADD CONSTRAINT ck_rep_respuesta CHECK (respuesta IS NULL OR (estado <> 'PENDIENTE' AND CHAR_LENGTH(respuesta) BETWEEN 1 AND 500)),
  ADD CONSTRAINT fk_rep_evento FOREIGN KEY (evento_id) REFERENCES evento (id);

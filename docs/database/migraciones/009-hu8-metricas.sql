-- HU8 (#52): lo que se muestra de cada lugar no puede estar vacio.
-- Antes de correrlo, ejecuta estas dos consultas. Las dos deben dar 0 filas:
--   SELECT id FROM lugar WHERE TRIM(nombre) = '';
--   SELECT id FROM categoria WHERE TRIM(nombre) = '';
-- Si una base nueva se crea con schema.sql, no necesita este script.

ALTER TABLE categoria
  ADD CONSTRAINT ck_categoria_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0);

ALTER TABLE lugar
  ADD CONSTRAINT ck_lugar_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0);

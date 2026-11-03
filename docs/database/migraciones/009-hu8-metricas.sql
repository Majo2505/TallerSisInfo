ALTER TABLE categoria
  ADD CONSTRAINT ck_categoria_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0);

ALTER TABLE lugar
  ADD CONSTRAINT ck_lugar_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0);

-- HU21: gestión del perfil. Lleva una base creada con la versión anterior de schema.sql a la actual.

-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- Si falla con "Duplicate column name", la migración ya estaba aplicada.
-- Carrera y semestre son opcionales: el estudiante los completa desde su perfil.
ALTER TABLE usuario
  ADD COLUMN carrera  VARCHAR(80) NULL AFTER nombre,
  ADD COLUMN semestre TINYINT     NULL AFTER carrera,
  ADD CONSTRAINT ck_usuario_semestre CHECK (semestre IS NULL OR semestre BETWEEN 1 AND 10);

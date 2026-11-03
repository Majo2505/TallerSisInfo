ALTER TABLE usuario
  ADD COLUMN carrera  VARCHAR(80) NULL AFTER nombre,
  ADD COLUMN semestre TINYINT     NULL AFTER carrera,
  ADD CONSTRAINT ck_usuario_semestre CHECK (semestre IS NULL OR semestre BETWEEN 1 AND 10);

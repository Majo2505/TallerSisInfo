-- Administración: foto real de cada lugar del catálogo. Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'foto_actualizada_en'", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · lugar_foto: la foto del lugar (JPG, PNG o WebP de hasta 2 MB; el panel la achica antes de subirla). Va en una tabla
--     aparte para que las listas de lugares no carguen los bytes, y en la base (no en el disco del servidor) para que el
--     equipo la vea desde cualquier computadora. Si se elimina el lugar, se elimina su foto.
--   · lugar.foto_actualizada_en: cuándo cambió la foto. Sirve de versión en la URL (/api/lugares/{id}/foto?v=…) para que el
--     navegador guarde la foto y la vuelva a pedir solo cuando cambia. NULL = el lugar no tiene foto y se muestra la ilustración.

ALTER TABLE lugar
  ADD COLUMN foto_actualizada_en DATETIME(6) NULL AFTER validado;

CREATE TABLE lugar_foto (
  lugar_id        INT          NOT NULL,
  contenido       MEDIUMBLOB   NOT NULL,
  tipo_mime       VARCHAR(20)  NOT NULL,
  bytes           INT          NOT NULL,
  actualizada_en  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  actualizada_por INT          NOT NULL,
  PRIMARY KEY (lugar_id),
  CONSTRAINT ck_foto_mime  CHECK (tipo_mime IN ('image/jpeg','image/png','image/webp')),
  CONSTRAINT ck_foto_bytes CHECK (bytes BETWEEN 1 AND 2097152),
  CONSTRAINT fk_foto_lugar FOREIGN KEY (lugar_id)        REFERENCES lugar (id) ON DELETE CASCADE,
  CONSTRAINT fk_foto_admin FOREIGN KEY (actualizada_por) REFERENCES usuario (id)
) ENGINE=InnoDB;

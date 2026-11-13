SET NAMES utf8mb4;

-- Etiquetas de tipo de actividad para personalizar las recomendaciones (HU30). Lleva una base creada con la versión
-- anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- Hay que ejecutarla con --default-character-set=utf8mb4 (o con el SET NAMES de arriba): las etiquetas llevan tildes.
-- No hace falta en una base nueva creada con el schema.sql actual (el catálogo viene en el schema.sql).
-- Es segura de correr dos veces: crea solo lo que falta.
--
-- Qué cambia:
--   · etiqueta_actividad: el catálogo cerrado de tipos de actividad (8 etiquetas amplias, no de energía), que gestiona el
--     admin. Se diseña sin depender de lugar para poder reutilizarlo después en comunidades y eventos.
--   · lugar_etiqueta: las etiquetas de cada lugar; un lugar puede tener varias. Si se elimina el lugar, se eliminan sus
--     etiquetas. Que cada lugar recomendable tenga al menos una lo valida el backend y el formulario del admin.
--   · La asignación de etiquetas a los lugares NO va aquí: en una base nueva no hay lugares cuando corre la migración. Está
--     en docs/database/seed-dev-etiquetas.sql, que asigna por el nombre exacto del lugar.

CREATE TABLE IF NOT EXISTS etiqueta_actividad (
  id      INT          NOT NULL AUTO_INCREMENT,
  nombre  VARCHAR(50)  NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_etiqueta_nombre UNIQUE (nombre),
  CONSTRAINT ck_etiqueta_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS lugar_etiqueta (
  lugar_id     INT  NOT NULL,
  etiqueta_id  INT  NOT NULL,
  PRIMARY KEY (lugar_id, etiqueta_id),
  KEY ix_lugar_etiqueta_etiqueta (etiqueta_id),
  CONSTRAINT fk_le_lugar    FOREIGN KEY (lugar_id)    REFERENCES lugar (id) ON DELETE CASCADE,
  CONSTRAINT fk_le_etiqueta FOREIGN KEY (etiqueta_id) REFERENCES etiqueta_actividad (id)
) ENGINE=InnoDB;

INSERT IGNORE INTO etiqueta_actividad (nombre) VALUES
  ('Café y bebidas'),
  ('Comer'),
  ('Deportes'),
  ('Caminar y aire libre'),
  ('Descanso y silencio'),
  ('Estudio'),
  ('Juegos de mesa'),
  ('Arte y música');

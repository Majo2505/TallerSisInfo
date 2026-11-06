CREATE TABLE IF NOT EXISTS evento_universitario (
  id              BIGINT       NOT NULL AUTO_INCREMENT,
  fuente          VARCHAR(40)  NOT NULL,
  fuente_url      VARCHAR(500) NOT NULL,
  titulo          VARCHAR(200) NOT NULL,
  descripcion     VARCHAR(2000) NOT NULL,
  inicio          DATETIME(6)  NOT NULL,
  fin             DATETIME(6)  NOT NULL,
  lugar           VARCHAR(200) NULL,
  actualizado_en  DATETIME(6)  NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_evento_fuente_url (fuente_url),
  KEY ix_evento_vigencia (inicio, fin)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

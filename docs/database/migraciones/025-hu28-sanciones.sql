SET NAMES utf8mb4;

-- Sanciones a usuarios (HU28). Lleva una base creada con la versión anterior de schema.sql a la actual. Requiere la
-- 024 (sancion.reporte_id apunta a reporte).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Table 'sancion' already exists", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · sancion: una sanción del admin a un usuario. Tipos: ADVERTENCIA (sin fecha de fin), SUSPENSION (siempre con fecha
--     de fin, posterior a creado_en) y BAJA (definitiva, sin fecha de fin; la fila de usuario se conserva y queda
--     bloqueada). Guarda quién y cuándo la aplicó, el reporte de origen (si lo hay; si el reporte se borra, queda NULL)
--     y quién y cuándo la levantó. El índice sostiene la validación del estado del usuario en cada request.

CREATE TABLE IF NOT EXISTS sancion (
  id             INT           NOT NULL AUTO_INCREMENT,
  usuario_id     INT           NOT NULL,
  tipo           VARCHAR(12)   NOT NULL,
  motivo         VARCHAR(500)  NOT NULL,
  reporte_id     INT           NULL,
  creado_por     INT           NOT NULL,
  creado_en      DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  hasta          DATETIME(6)   NULL,
  levantada_en   DATETIME(6)   NULL,
  levantada_por  INT           NULL,
  PRIMARY KEY (id),
  KEY ix_sancion_usuario (usuario_id, tipo, hasta),
  CONSTRAINT ck_sancion_tipo      CHECK (tipo IN ('ADVERTENCIA','SUSPENSION','BAJA')),
  CONSTRAINT ck_sancion_motivo    CHECK (CHAR_LENGTH(TRIM(motivo)) > 0),
  CONSTRAINT ck_sancion_hasta     CHECK ((tipo = 'SUSPENSION' AND hasta IS NOT NULL AND hasta > creado_en)
                                      OR (tipo <> 'SUSPENSION' AND hasta IS NULL)),
  CONSTRAINT ck_sancion_levantada CHECK ((levantada_en IS NULL) = (levantada_por IS NULL)),
  CONSTRAINT fk_sancion_usuario FOREIGN KEY (usuario_id)    REFERENCES usuario (id),
  CONSTRAINT fk_sancion_creador FOREIGN KEY (creado_por)    REFERENCES usuario (id),
  CONSTRAINT fk_sancion_levanta FOREIGN KEY (levantada_por) REFERENCES usuario (id),
  CONSTRAINT fk_sancion_reporte FOREIGN KEY (reporte_id)    REFERENCES reporte (id) ON DELETE SET NULL
) ENGINE=InnoDB;

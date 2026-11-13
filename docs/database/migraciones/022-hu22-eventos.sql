SET NAMES utf8mb4;

-- Eventos del campus (HU22 a HU26). Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Table 'evento' already exists", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · evento: un evento del campus. Las fechas van en UTC (Bolivia es UTC-4). Un evento tiene un lugar del catálogo
--     (lugar_id) o un lugar en texto libre (lugar_texto): al menos uno. Con asistencia FIJA hay que llegar a la hora; con
--     LIBRE se puede pasar un rato y el visitante necesita visita_min y visita_max (minutos). Un evento PENDIENTE o
--     RECHAZADO guarda su motivo completo (la notificación lo recorta a 200 caracteres).
--     creador_id y lugar_id no llevan ON DELETE: el borrado de los eventos de un usuario (baja o eliminar cuenta) lo hace
--     de forma explícita el backend.
--   · evento_imagen: la imagen del evento (JPG o PNG de hasta 2 MB), una por evento, en tabla aparte como lugar_foto para
--     que las listas no carguen los bytes. Si se elimina el evento, se elimina su imagen. actualizada_en sirve de versión
--     en la URL de la imagen.
--   · ck_hist_entidad: historial_cambio admite la entidad EVENTO (se amplía, no se restringe).

CREATE TABLE IF NOT EXISTS evento (
  id           INT           NOT NULL AUTO_INCREMENT,
  creador_id   INT           NOT NULL,
  titulo       VARCHAR(120)  NOT NULL,
  descripcion  TEXT          NULL,
  lugar_id     INT           NULL,
  lugar_texto  VARCHAR(150)  NULL,
  inicio       DATETIME(6)   NOT NULL,
  fin          DATETIME(6)   NOT NULL,
  estado       VARCHAR(10)   NOT NULL,
  origen       VARCHAR(10)   NOT NULL,
  asistencia   VARCHAR(5)    NOT NULL,
  visita_min   SMALLINT      NULL,
  visita_max   SMALLINT      NULL,
  motivo       VARCHAR(500)  NULL,
  created_at   DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at   DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_evento_estado_fin (estado, fin),
  KEY ix_evento_creador (creador_id, created_at),
  CONSTRAINT ck_evento_titulo     CHECK (CHAR_LENGTH(TRIM(titulo)) > 0),
  CONSTRAINT ck_evento_fechas     CHECK (fin > inicio),
  CONSTRAINT ck_evento_lugar      CHECK (lugar_id IS NOT NULL OR (lugar_texto IS NOT NULL AND CHAR_LENGTH(TRIM(lugar_texto)) > 0)),
  CONSTRAINT ck_evento_estado     CHECK (estado IN ('PENDIENTE','PUBLICADO','RECHAZADO','CANCELADO')),
  CONSTRAINT ck_evento_origen     CHECK (origen IN ('FORMULARIO','FLYER','ADMIN')),
  CONSTRAINT ck_evento_asistencia CHECK (asistencia IN ('FIJA','LIBRE')),
  CONSTRAINT ck_evento_visita     CHECK ((asistencia = 'FIJA'  AND visita_min IS NULL AND visita_max IS NULL)
                                      OR (asistencia = 'LIBRE' AND visita_min IS NOT NULL AND visita_max IS NOT NULL
                                          AND visita_min > 0 AND visita_min <= visita_max)),
  CONSTRAINT ck_evento_motivo     CHECK (estado NOT IN ('PENDIENTE','RECHAZADO') OR (motivo IS NOT NULL AND CHAR_LENGTH(TRIM(motivo)) > 0)),
  CONSTRAINT fk_evento_creador FOREIGN KEY (creador_id) REFERENCES usuario (id),
  CONSTRAINT fk_evento_lugar   FOREIGN KEY (lugar_id)   REFERENCES lugar (id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS evento_imagen (
  evento_id       INT          NOT NULL,
  contenido       MEDIUMBLOB   NOT NULL,
  tipo_mime       VARCHAR(20)  NOT NULL,
  bytes           INT          NOT NULL,
  actualizada_en  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (evento_id),
  CONSTRAINT ck_evimg_mime  CHECK (tipo_mime IN ('image/jpeg','image/png')),
  CONSTRAINT ck_evimg_bytes CHECK (bytes BETWEEN 1 AND 2097152),
  CONSTRAINT fk_evimg_evento FOREIGN KEY (evento_id) REFERENCES evento (id) ON DELETE CASCADE
) ENGINE=InnoDB;

ALTER TABLE historial_cambio
  DROP CHECK ck_hist_entidad,
  ADD CONSTRAINT ck_hist_entidad CHECK (entidad IN ('USUARIO','LUGAR','ZONA','EVENTO'));

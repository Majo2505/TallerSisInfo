SET NAMES utf8mb4;

-- Test de afinidad y conexiones de «Libres contigo» (HU12, ampliación). Lleva una base creada con la versión anterior de
-- schema.sql a la actual. Requiere la 026 (notificacion.sancion_id).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Table 'test_afinidad' already exists" o "Duplicate column name 'conexion_id'", ya estaba aplicada.
--
-- Qué cambia:
--   · test_afinidad: las respuestas del test (2 minutos) de cada estudiante, una fila por persona. respuestas es un objeto
--     JSON {pregunta: [opciones]}; qué preguntas y opciones valen lo valida el backend (Domain/ReglasAfinidad.cs).
--     version permite cambiar las preguntas más adelante sin mezclar respuestas viejas.
--   · conexion: un «saludo» de un estudiante a otro con quien coincide en tiempo libre y en el test. Guarda el tramo
--     libre que comparten, la afinidad y lo que más los conecta (en_comun, una foto de ese momento). Estados PENDIENTE,
--     ACEPTADA y RECHAZADA. Solo puede haber una conexión por pareja, en cualquier sentido (uq_conexion_par sobre las
--     columnas generadas par_menor y par_mayor).
--   · notificacion.conexion_id y los tipos CONEXION_SOLICITADA y CONEXION_ACEPTADA. ck_notif_ref se reescribe: cada rama
--     acepta exactamente lo que aceptaba antes y, además, exige conexion_id en NULL.
--   · Sin ON DELETE (las columnas están en CHECK, como en la 026): la purga de una cuenta los borra de forma explícita.

CREATE TABLE IF NOT EXISTS test_afinidad (
  usuario_id      INT          NOT NULL,
  version         TINYINT      NOT NULL,
  respuestas      JSON         NOT NULL,
  completado_en   DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  actualizado_en  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (usuario_id),
  CONSTRAINT ck_test_version    CHECK (version >= 1),
  CONSTRAINT ck_test_respuestas CHECK (JSON_TYPE(respuestas) = 'OBJECT'),
  CONSTRAINT ck_test_fechas     CHECK (actualizado_en >= completado_en),
  CONSTRAINT fk_test_usuario    FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS conexion (
  id               INT          NOT NULL AUTO_INCREMENT,
  solicitante_id   INT          NOT NULL,
  destinatario_id  INT          NOT NULL,
  par_menor        INT          GENERATED ALWAYS AS (LEAST(solicitante_id, destinatario_id)) STORED,
  par_mayor        INT          GENERATED ALWAYS AS (GREATEST(solicitante_id, destinatario_id)) STORED,
  dia_semana       SMALLINT     NOT NULL,
  hora_inicio      TIME         NOT NULL,
  hora_fin         TIME         NOT NULL,
  afinidad         TINYINT      NOT NULL,
  en_comun         JSON         NOT NULL,
  estado           VARCHAR(10)  NOT NULL DEFAULT 'PENDIENTE',
  creada_en        DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  respondida_en    DATETIME(6)  NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_conexion_par UNIQUE (par_menor, par_mayor),
  KEY ix_conexion_destinatario (destinatario_id, estado),
  KEY ix_conexion_solicitante  (solicitante_id, estado),
  CONSTRAINT ck_conexion_distintos CHECK (solicitante_id <> destinatario_id),
  CONSTRAINT ck_conexion_estado    CHECK (estado IN ('PENDIENTE','ACEPTADA','RECHAZADA')),
  CONSTRAINT ck_conexion_respuesta CHECK ((estado = 'PENDIENTE') = (respondida_en IS NULL)),
  CONSTRAINT ck_conexion_dia       CHECK (dia_semana BETWEEN 1 AND 6),
  CONSTRAINT ck_conexion_horas     CHECK (hora_fin > hora_inicio),
  CONSTRAINT ck_conexion_afinidad  CHECK (afinidad BETWEEN 0 AND 100),
  CONSTRAINT ck_conexion_en_comun  CHECK (JSON_TYPE(en_comun) = 'ARRAY'),
  CONSTRAINT fk_conexion_solicitante  FOREIGN KEY (solicitante_id)  REFERENCES usuario (id),
  CONSTRAINT fk_conexion_destinatario FOREIGN KEY (destinatario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

ALTER TABLE notificacion
  ADD COLUMN conexion_id INT NULL AFTER sancion_id,
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref,
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA',
                                               'EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA',
                                               'CONEXION_SOLICITADA','CONEXION_ACEPTADA')),
  ADD CONSTRAINT ck_notif_ref CHECK (
       (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES')
          AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL)
    OR (tipo IN ('INVITACION_COMUNIDAD','COMUNIDAD_CERRADA') AND comunidad_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL)
    OR (tipo IN ('EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO') AND evento_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL)
    OR (tipo = 'REPORTE_RESUELTO' AND reporte_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL)
    OR (tipo = 'SANCION_ADVERTENCIA' AND sancion_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND conexion_id IS NULL)
    OR (tipo IN ('CONEXION_SOLICITADA','CONEXION_ACEPTADA') AND conexion_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)),
  ADD CONSTRAINT uq_notif_conexion UNIQUE (usuario_id, conexion_id, tipo),
  ADD CONSTRAINT fk_notif_conexion FOREIGN KEY (conexion_id) REFERENCES conexion (id);

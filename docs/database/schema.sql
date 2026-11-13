CREATE DATABASE IF NOT EXISTS breaku CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE breaku;

CREATE TABLE usuario (
  id                        INT          NOT NULL AUTO_INCREMENT,
  correo                    VARCHAR(120) NOT NULL,
  password_hash             VARCHAR(255) NULL,
  google_sub                VARCHAR(64)  NULL,              -- cuenta de Google enlazada (claim «sub»); NULL si nunca entró con Google
  nombre                    VARCHAR(100) NOT NULL,
  carrera                   VARCHAR(80)  NULL,
  semestre                  TINYINT      NULL,
  rol                       VARCHAR(15)  NOT NULL DEFAULT 'ESTUDIANTE',
  correo_verificado         BOOLEAN      NOT NULL DEFAULT FALSE,
  consentimiento_ubicacion  BOOLEAN      NOT NULL DEFAULT FALSE,
  fecha_consentimiento      DATETIME(6)  NULL,
  notificaciones_activas    BOOLEAN      NOT NULL DEFAULT TRUE,
  visible_matching          BOOLEAN      NOT NULL DEFAULT FALSE,
  fecha_visible_matching    DATETIME(6)  NULL,
  zona_actual_id            INT          NULL,
  created_at                DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at                DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_by                INT          NULL,
  activo                    BOOLEAN      NOT NULL DEFAULT TRUE,
  PRIMARY KEY (id),
  CONSTRAINT uq_usuario_correo UNIQUE (correo),
  CONSTRAINT uq_usuario_google UNIQUE (google_sub),
  CONSTRAINT ck_usuario_credencial CHECK (password_hash IS NOT NULL OR google_sub IS NOT NULL),   -- contraseña, Google o ambas
  CONSTRAINT ck_usuario_rol    CHECK (rol IN ('ESTUDIANTE','ADMIN')),
  CONSTRAINT ck_usuario_semestre CHECK (semestre IS NULL OR semestre BETWEEN 1 AND 10),
  CONSTRAINT ck_usuario_consentimiento CHECK (
    consentimiento_ubicacion = FALSE OR (fecha_consentimiento IS NOT NULL AND zona_actual_id IS NOT NULL)
  ),
  CONSTRAINT ck_usuario_visible_matching CHECK (visible_matching = FALSE OR fecha_visible_matching IS NOT NULL),
  CONSTRAINT fk_usuario_updated_by FOREIGN KEY (updated_by) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE zona (
  id          INT           NOT NULL AUTO_INCREMENT,
  nombre      VARCHAR(80)   NOT NULL,
  latitud     DECIMAL(9,6)  NOT NULL,
  longitud    DECIMAL(9,6)  NOT NULL,
  radio_max_m INT           NOT NULL,
  activa      BOOLEAN       NOT NULL DEFAULT TRUE,
  created_at  DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at  DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  created_by  INT           NOT NULL,
  updated_by  INT           NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_zona_nombre UNIQUE (nombre),
  CONSTRAINT ck_zona_radio  CHECK (radio_max_m > 0),
  CONSTRAINT ck_zona_lat    CHECK (latitud  BETWEEN -90  AND 90),
  CONSTRAINT ck_zona_lng    CHECK (longitud BETWEEN -180 AND 180),
  CONSTRAINT fk_zona_created_by FOREIGN KEY (created_by) REFERENCES usuario (id),
  CONSTRAINT fk_zona_updated_by FOREIGN KEY (updated_by) REFERENCES usuario (id)
) ENGINE=InnoDB;

ALTER TABLE usuario
  ADD KEY ix_usuario_zona_actual (zona_actual_id),
  ADD KEY ix_usuario_matching (zona_actual_id, visible_matching),
  ADD CONSTRAINT fk_usuario_zona_actual FOREIGN KEY (zona_actual_id) REFERENCES zona (id);

CREATE TABLE categoria (
  id          INT         NOT NULL AUTO_INCREMENT,
  nombre      VARCHAR(50) NOT NULL,
  created_at  DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at  DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  created_by  INT         NOT NULL,
  updated_by  INT         NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_categoria_nombre UNIQUE (nombre),
  CONSTRAINT ck_categoria_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0),
  CONSTRAINT fk_categoria_created_by FOREIGN KEY (created_by) REFERENCES usuario (id),
  CONSTRAINT fk_categoria_updated_by FOREIGN KEY (updated_by) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE lugar (
  id                     INT           NOT NULL AUTO_INCREMENT,
  nombre                 VARCHAR(100)  NOT NULL,
  descripcion            TEXT          NULL,
  categoria_id           INT           NOT NULL,
  latitud                DECIMAL(9,6)  NOT NULL,
  longitud               DECIMAL(9,6)  NOT NULL,
  duracion_estimada_min  SMALLINT      NOT NULL,
  hora_apertura          TIME          NULL,
  hora_cierre            TIME          NULL,
  estado                 VARCHAR(15)   NOT NULL DEFAULT 'ACTIVO',
  tipo_lugar             VARCHAR(30)   NULL,
  modalidad              VARCHAR(40)   NULL,
  nivel_energia          VARCHAR(40)   NULL,
  duracion_min           SMALLINT      NULL,
  duracion_max           SMALLINT      NULL,
  actividades_sugeridas  TEXT          NULL,
  min_a_pie_desde_bloque_a SMALLINT    NULL,
  recomendable           BOOLEAN       NOT NULL DEFAULT TRUE,
  validado               BOOLEAN       NOT NULL DEFAULT TRUE,
  foto_actualizada_en    DATETIME(6)   NULL,                                   -- admin: cuándo cambió la foto (versión de la URL); NULL = sin foto
  created_at             DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at             DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  created_by             INT           NOT NULL,
  updated_by             INT           NOT NULL,
  PRIMARY KEY (id),
  KEY ix_lugar_estado_categoria (estado, categoria_id),
  KEY ix_lugar_recomendable (recomendable, estado),
  CONSTRAINT ck_lugar_estado   CHECK (estado IN ('ACTIVO','EN_REVISION','INACTIVO')),
  CONSTRAINT ck_lugar_duracion CHECK (duracion_estimada_min > 0),
  CONSTRAINT ck_lugar_nombre   CHECK (CHAR_LENGTH(TRIM(nombre)) > 0),
  CONSTRAINT uq_lugar_nombre   UNIQUE (nombre),                                  -- admin: dos lugares con el mismo nombre confunden al estudiante y al asistente
  CONSTRAINT ck_lugar_horario  CHECK ((hora_apertura IS NULL AND hora_cierre IS NULL)
                                   OR (hora_apertura IS NOT NULL AND hora_cierre IS NOT NULL AND hora_cierre > hora_apertura)),
  CONSTRAINT ck_lugar_rango_duracion CHECK (duracion_min IS NULL OR duracion_max IS NULL OR duracion_min <= duracion_max),
  CONSTRAINT ck_lugar_lat      CHECK (latitud  BETWEEN -90  AND 90),
  CONSTRAINT ck_lugar_lng      CHECK (longitud BETWEEN -180 AND 180),
  CONSTRAINT fk_lugar_categoria  FOREIGN KEY (categoria_id) REFERENCES categoria (id),
  CONSTRAINT fk_lugar_created_by FOREIGN KEY (created_by)   REFERENCES usuario (id),
  CONSTRAINT fk_lugar_updated_by FOREIGN KEY (updated_by)   REFERENCES usuario (id)
) ENGINE=InnoDB;

-- admin: foto real del lugar, aparte para que las listas no carguen los bytes. Una por lugar.
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

CREATE TABLE horario (
  id          INT          NOT NULL AUTO_INCREMENT,
  usuario_id  INT          NOT NULL,
  materia     VARCHAR(100) NOT NULL,
  dia_semana  SMALLINT     NOT NULL,
  hora_inicio TIME         NOT NULL,
  hora_fin    TIME         NOT NULL,
  aula        VARCHAR(30)  NULL,
  origen      VARCHAR(10)  NOT NULL,
  asiste      BOOLEAN      NOT NULL DEFAULT TRUE,
  autorizacion_choque BOOLEAN NOT NULL DEFAULT FALSE,
  created_at  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  CONSTRAINT uq_horario_bloque UNIQUE (usuario_id, dia_semana, hora_inicio, materia),
  CONSTRAINT ck_horario_dia    CHECK (dia_semana BETWEEN 1 AND 6),
  CONSTRAINT ck_horario_horas  CHECK (hora_fin > hora_inicio),
  CONSTRAINT ck_horario_rango  CHECK (hora_inicio >= '07:15:00' AND hora_fin <= '21:00:00'),
  CONSTRAINT ck_horario_duracion CHECK (TIME_TO_SEC(TIMEDIFF(hora_fin, hora_inicio)) >= 2700),  -- HU2: una clase dura al menos 45 min
  CONSTRAINT ck_horario_origen CHECK (origen IN ('MANUAL','FOTO')),
  CONSTRAINT fk_horario_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE puente (
  id          INT      NOT NULL AUTO_INCREMENT,
  usuario_id  INT      NOT NULL,
  dia_semana  SMALLINT NOT NULL,
  hora_inicio TIME     NOT NULL,
  hora_fin    TIME     NOT NULL,
  tipo        VARCHAR(20) NOT NULL DEFAULT 'ENTRE_CLASES',
  PRIMARY KEY (id),
  CONSTRAINT uq_puente_slot UNIQUE (usuario_id, dia_semana, hora_inicio),
  KEY ix_puente_matching (dia_semana, hora_inicio, hora_fin),
  CONSTRAINT ck_puente_dia   CHECK (dia_semana BETWEEN 1 AND 6),
  CONSTRAINT ck_puente_horas CHECK (hora_fin > hora_inicio),
  CONSTRAINT ck_puente_tipo  CHECK (tipo IN ('ENTRE_CLASES','DESPUES_DE_CLASES')),
  CONSTRAINT fk_puente_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE log_auditoria_rag (
  id                     BIGINT       NOT NULL AUTO_INCREMENT,
  usuario_id             INT          NOT NULL,
  consulta_origen_id     BIGINT       NULL,               -- HU11: primer turno de la conversación (NULL = esta fila es el primero)
  tipo                   VARCHAR(25)  NOT NULL,
  zona_id                INT          NULL,
  radio_aplicado_m       INT          NULL,
  puente_id              INT          NULL,
  tiempo_disponible_min  SMALLINT     NULL,
  entrada_usuario        TEXT         NULL,
  prompt_enviado         TEXT         NULL,
  respuesta_llm          TEXT         NULL,
  estado                 VARCHAR(20)  NOT NULL,
  motivo_rechazo         VARCHAR(20)  NULL,
  datos_extraidos        JSON         NULL,               -- HU10/HU11: resumen validado {actividad, horaInicio, duracionMin, maxParticipantes}; lo que se confirma
  modelo                 VARCHAR(40)  NULL,
  latencia_ms            INT          NULL,
  created_at             DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_log_created (created_at),
  KEY ix_log_usuario (usuario_id, created_at),
  KEY ix_log_motivo (motivo_rechazo, created_at),
  KEY ix_log_origen (consulta_origen_id),
  CONSTRAINT ck_log_tipo   CHECK (tipo   IN ('RECOMENDACION','EXTRACCION_COMUNIDAD','EXTRACCION_EVENTO')),
  CONSTRAINT ck_log_estado CHECK (estado IN ('OK','SIN_RESULTADOS','RECHAZADO_SEGURIDAD','ERROR','INCOMPLETO')),
  CONSTRAINT ck_log_motivo CHECK (motivo_rechazo IN ('INYECCION','FUERA_DE_ALCANCE','BIENESTAR','FUERA_DE_REGLAMENTO','FUERA_DE_CONTEXTO')),
  CONSTRAINT ck_log_motivo_estado CHECK (motivo_rechazo IS NULL OR estado = 'RECHAZADO_SEGURIDAD'),
  CONSTRAINT ck_log_datos_tipo    CHECK (datos_extraidos IS NULL OR tipo IN ('EXTRACCION_COMUNIDAD','EXTRACCION_EVENTO')),   -- HU10 y HU24: solo las extracciones tienen resumen
  CONSTRAINT fk_log_origen  FOREIGN KEY (consulta_origen_id) REFERENCES log_auditoria_rag (id) ON DELETE SET NULL,
  CONSTRAINT fk_log_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id),
  CONSTRAINT fk_log_zona    FOREIGN KEY (zona_id)    REFERENCES zona (id),
  CONSTRAINT fk_log_puente  FOREIGN KEY (puente_id)  REFERENCES puente (id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE lugar_recuperado (
  log_id              BIGINT   NOT NULL,
  lugar_id            INT      NOT NULL,
  distancia_m         INT      NOT NULL,
  tiempo_traslado_min SMALLINT NOT NULL,
  orden               SMALLINT NOT NULL,
  PRIMARY KEY (log_id, lugar_id),
  CONSTRAINT uq_lr_orden UNIQUE (log_id, orden),
  CONSTRAINT fk_lr_log   FOREIGN KEY (log_id)   REFERENCES log_auditoria_rag (id),
  CONSTRAINT fk_lr_lugar FOREIGN KEY (lugar_id) REFERENCES lugar (id)
) ENGINE=InnoDB;

CREATE TABLE recomendacion (
  id             BIGINT      NOT NULL AUTO_INCREMENT,
  log_id         BIGINT      NOT NULL,
  lugar_id       INT         NOT NULL,
  explicacion    TEXT        NOT NULL,
  feedback       VARCHAR(8)  NULL,
  fecha_feedback DATETIME(6) NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_reco_log_lugar UNIQUE (log_id, lugar_id),
  KEY ix_recomendacion_feedback (feedback, fecha_feedback),
  CONSTRAINT ck_reco_feedback  CHECK (feedback IN ('LIKE','DISLIKE')),
  CONSTRAINT ck_reco_fecha     CHECK ((feedback IS NULL) = (fecha_feedback IS NULL)),
  CONSTRAINT fk_reco_recuperado FOREIGN KEY (log_id, lugar_id) REFERENCES lugar_recuperado (log_id, lugar_id)
) ENGINE=InnoDB;

CREATE TABLE comunidad_efimera (
  id                INT          NOT NULL AUTO_INCREMENT,
  creador_id        INT          NOT NULL,
  log_id            BIGINT       NULL,
  texto_original    TEXT         NOT NULL,
  actividad         VARCHAR(100) NOT NULL,
  inicio            DATETIME(6)  NOT NULL,
  fin               DATETIME(6)  NOT NULL,
  max_participantes SMALLINT     NOT NULL,
  lugar_id          INT          NULL,
  estado            VARCHAR(10)  NOT NULL DEFAULT 'ACTIVA',
  created_at        DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at        DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_comunidad_estado_fin (estado, fin),
  CONSTRAINT uq_com_log    UNIQUE (log_id),
  CONSTRAINT ck_com_fechas CHECK (fin > inicio),
  CONSTRAINT ck_com_estado CHECK (estado IN ('ACTIVA','ARCHIVADA')),
  CONSTRAINT ck_com_max    CHECK (max_participantes BETWEEN 2 AND 30),
  -- HU10: las mismas reglas de la extracción, como última barrera. inicio y fin están en UTC; Bolivia es UTC-4.
  CONSTRAINT ck_com_duracion CHECK (TIMESTAMPDIFF(MINUTE, inicio, fin) BETWEEN 10 AND 240),
  CONSTRAINT ck_com_horario  CHECK (TIME(inicio - INTERVAL 4 HOUR) >= '07:15:00'
                                    AND TIME(fin - INTERVAL 4 HOUR) <= '21:00:00'
                                    AND DATE(inicio - INTERVAL 4 HOUR) = DATE(fin - INTERVAL 4 HOUR)),
  CONSTRAINT fk_com_creador FOREIGN KEY (creador_id) REFERENCES usuario (id),
  CONSTRAINT fk_com_log     FOREIGN KEY (log_id)     REFERENCES log_auditoria_rag (id),
  CONSTRAINT fk_com_lugar   FOREIGN KEY (lugar_id)   REFERENCES lugar (id)
) ENGINE=InnoDB;

CREATE TABLE participacion (
  comunidad_id  INT         NOT NULL,
  usuario_id    INT         NOT NULL,
  estado        VARCHAR(10) NOT NULL DEFAULT 'PENDIENTE',
  invitado_en   DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  respondido_en DATETIME(6) NULL,
  PRIMARY KEY (comunidad_id, usuario_id),
  KEY ix_part_usuario_estado (usuario_id, estado),
  CONSTRAINT ck_part_estado   CHECK (estado IN ('PENDIENTE','CONFIRMADO','RECHAZADO')),
  CONSTRAINT ck_part_respuesta CHECK ((estado = 'PENDIENTE') = (respondido_en IS NULL)),
  CONSTRAINT fk_part_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera (id),
  CONSTRAINT fk_part_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE evento (
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

CREATE TABLE evento_imagen (
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

CREATE TABLE reporte (
  id           INT          NOT NULL AUTO_INCREMENT,
  usuario_id   INT          NOT NULL,
  lugar_id     INT          NULL,
  comunidad_id INT          NULL,
  evento_id    INT          NULL,
  motivo       VARCHAR(30)  NOT NULL,                     -- ERROR_APP | SUGERENCIA | LUGAR_INCORRECTO | COMUNIDAD_INAPROPIADA | EVENTO_INAPROPIADO
  descripcion  TEXT         NULL,
  estado       VARCHAR(12)  NOT NULL DEFAULT 'PENDIENTE',
  revisor_id   INT          NULL,
  created_at   DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  revisado_en  DATETIME(6)  NULL,
  respuesta    TEXT         NULL,
  PRIMARY KEY (id),
  KEY ix_rep_estado (estado, created_at),
  CONSTRAINT ck_rep_estado  CHECK (estado IN ('PENDIENTE','RESUELTO','DESCARTADO')),
  CONSTRAINT ck_rep_motivo  CHECK (motivo IN ('ERROR_APP','SUGERENCIA','LUGAR_INCORRECTO','COMUNIDAD_INAPROPIADA','EVENTO_INAPROPIADO')),
  -- Un reporte de un lugar apunta a ese lugar; uno de una comunidad, a esa comunidad; uno de un evento, a ese evento; «algo no funciona» y las sugerencias, a nada.
  CONSTRAINT ck_rep_objeto  CHECK ((motivo = 'LUGAR_INCORRECTO'      AND lugar_id IS NOT NULL AND comunidad_id IS NULL AND evento_id IS NULL)
                                OR (motivo = 'COMUNIDAD_INAPROPIADA' AND comunidad_id IS NOT NULL AND lugar_id IS NULL AND evento_id IS NULL)
                                OR (motivo = 'EVENTO_INAPROPIADO'    AND evento_id IS NOT NULL AND lugar_id IS NULL AND comunidad_id IS NULL)
                                OR (motivo IN ('ERROR_APP','SUGERENCIA') AND lugar_id IS NULL AND comunidad_id IS NULL AND evento_id IS NULL)),
  CONSTRAINT ck_rep_descripcion CHECK (CHAR_LENGTH(TRIM(descripcion)) BETWEEN 10 AND 500),     -- lo mismo que pide el formulario
  CONSTRAINT ck_rep_revision CHECK ((estado = 'PENDIENTE' AND revisor_id IS NULL AND revisado_en IS NULL)
                                 OR (estado <> 'PENDIENTE' AND revisor_id IS NOT NULL AND revisado_en IS NOT NULL)),
  CONSTRAINT ck_rep_respuesta CHECK (respuesta IS NULL OR (estado <> 'PENDIENTE' AND CHAR_LENGTH(respuesta) BETWEEN 1 AND 500)),
  CONSTRAINT fk_rep_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuario (id),
  CONSTRAINT fk_rep_lugar     FOREIGN KEY (lugar_id)     REFERENCES lugar (id),
  CONSTRAINT fk_rep_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera (id),
  CONSTRAINT fk_rep_revisor   FOREIGN KEY (revisor_id)   REFERENCES usuario (id),
  CONSTRAINT fk_rep_evento    FOREIGN KEY (evento_id)    REFERENCES evento (id)
) ENGINE=InnoDB;

CREATE TABLE sancion (
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

CREATE TABLE notificacion (
  id                  BIGINT       NOT NULL AUTO_INCREMENT,
  usuario_id          INT          NOT NULL,
  tipo                VARCHAR(25)  NOT NULL,
  comunidad_id        INT          NULL,
  evento_id           INT          NULL,
  reporte_id          INT          NULL,
  sancion_id          INT          NULL,
  puente_fecha        DATE         NULL,
  puente_hora_inicio  TIME         NULL,
  puente_hora_fin     TIME         NULL,
  mensaje             VARCHAR(200) NOT NULL,
  leida               BOOLEAN      NOT NULL DEFAULT FALSE,
  activa              BOOLEAN      NOT NULL DEFAULT TRUE,
  created_at          DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  CONSTRAINT uq_notif_puente UNIQUE (usuario_id, puente_fecha, puente_hora_inicio),
  CONSTRAINT uq_notif_comunidad UNIQUE (usuario_id, comunidad_id, tipo),
  CONSTRAINT uq_notif_evento  UNIQUE (usuario_id, evento_id, tipo),
  CONSTRAINT uq_notif_reporte UNIQUE (usuario_id, reporte_id, tipo),
  CONSTRAINT uq_notif_sancion UNIQUE (usuario_id, sancion_id, tipo),
  KEY ix_notif_usuario (usuario_id, activa, leida, created_at),
  CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA',
                                           'EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA')),
  -- Cada aviso apunta solo a lo suyo: un puente, una comunidad, un evento, un reporte o una sanción.
  CONSTRAINT ck_notif_ref CHECK (
       (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES')
          AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)
    OR (tipo IN ('INVITACION_COMUNIDAD','COMUNIDAD_CERRADA') AND comunidad_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)
    OR (tipo IN ('EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO') AND evento_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)
    OR (tipo = 'REPORTE_RESUELTO' AND reporte_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND sancion_id IS NULL)
    OR (tipo = 'SANCION_ADVERTENCIA' AND sancion_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL)),
  CONSTRAINT ck_notif_puente_horas CHECK (puente_hora_fin > puente_hora_inicio),
  CONSTRAINT fk_notif_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuario (id),
  CONSTRAINT fk_notif_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera (id),
  CONSTRAINT fk_notif_evento    FOREIGN KEY (evento_id)    REFERENCES evento (id),
  CONSTRAINT fk_notif_reporte   FOREIGN KEY (reporte_id)   REFERENCES reporte (id),
  CONSTRAINT fk_notif_sancion   FOREIGN KEY (sancion_id)   REFERENCES sancion (id)
) ENGINE=InnoDB;

CREATE TABLE historial_cambio (
  id                 BIGINT      NOT NULL AUTO_INCREMENT,
  entidad            VARCHAR(30) NOT NULL,
  entidad_id         INT         NOT NULL,
  accion             VARCHAR(10) NOT NULL,
  valores_anteriores JSON        NULL,
  valores_nuevos     JSON        NULL,
  usuario_id         INT         NULL,
  created_at         DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_hist_entidad (entidad, entidad_id, created_at),
  KEY ix_hist_usuario (usuario_id, created_at),
  CONSTRAINT ck_hist_entidad CHECK (entidad IN ('USUARIO','LUGAR','ZONA','EVENTO')),
  CONSTRAINT ck_hist_accion  CHECK (accion IN ('CREATE','UPDATE','DELETE')),
  CONSTRAINT ck_hist_valores CHECK (
        (accion = 'CREATE' AND valores_anteriores IS NULL     AND valores_nuevos IS NOT NULL)
     OR (accion = 'UPDATE' AND valores_anteriores IS NOT NULL AND valores_nuevos IS NOT NULL)
     OR (accion = 'DELETE' AND valores_anteriores IS NOT NULL AND valores_nuevos IS NULL)),
  CONSTRAINT fk_hist_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE etiqueta_actividad (
  id      INT          NOT NULL AUTO_INCREMENT,
  nombre  VARCHAR(50)  NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_etiqueta_nombre UNIQUE (nombre),
  CONSTRAINT ck_etiqueta_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) > 0)
) ENGINE=InnoDB;

CREATE TABLE lugar_etiqueta (
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

CREATE OR REPLACE VIEW v_metricas_lugar AS
SELECT l.id AS lugar_id, l.nombre, c.nombre AS categoria,
       COUNT(r.id)                     AS veces_recomendado,
       COALESCE(SUM(r.feedback = 'LIKE'), 0)    AS likes,
       COALESCE(SUM(r.feedback = 'DISLIKE'), 0) AS dislikes,
       (SELECT COUNT(*) FROM reporte rp WHERE rp.lugar_id = l.id) AS reportes
FROM lugar l
JOIN categoria c ON c.id = l.categoria_id
LEFT JOIN recomendacion r ON r.lugar_id = l.id
GROUP BY l.id, l.nombre, c.nombre;

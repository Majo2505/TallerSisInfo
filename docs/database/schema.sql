-- BREAKU · Modelo físico v2 (MySQL 8.x, InnoDB, utf8mb4)
-- Todas las fechas se guardan en UTC (DATETIME(6)); Bolivia es UTC-4 y se convierte en la interfaz.
CREATE DATABASE IF NOT EXISTS breaku CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE breaku;

-- ============ IDENTIDAD ============
CREATE TABLE usuario (
  id                        INT          NOT NULL AUTO_INCREMENT,
  correo                    VARCHAR(120) NOT NULL,
  password_hash             VARCHAR(255) NULL,            -- NULL solo si se adopta SSO (D14)
  nombre                    VARCHAR(100) NOT NULL,
  rol                       VARCHAR(15)  NOT NULL DEFAULT 'ESTUDIANTE',
  correo_verificado         BOOLEAN      NOT NULL DEFAULT FALSE,
  consentimiento_ubicacion  BOOLEAN      NOT NULL DEFAULT FALSE,
  fecha_consentimiento      DATETIME(6)  NULL,
  created_at                DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at                DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_by                INT          NULL,            -- quien hizo el último cambio (NULL = el propio usuario / sistema)
  activo                    BOOLEAN      NOT NULL DEFAULT TRUE,   -- borrado lógico: 1 = activa, 0 = dada de baja (quién/cuándo: historial_cambio)
  PRIMARY KEY (id),
  CONSTRAINT uq_usuario_correo UNIQUE (correo),
  CONSTRAINT ck_usuario_rol    CHECK (rol IN ('ESTUDIANTE','ADMIN')),
  CONSTRAINT ck_usuario_consentimiento CHECK (consentimiento_ubicacion = FALSE OR fecha_consentimiento IS NOT NULL),
  CONSTRAINT fk_usuario_updated_by FOREIGN KEY (updated_by) REFERENCES usuario (id)
) ENGINE=InnoDB;

-- ============ CATÁLOGO (administrado por ADMIN) ============
CREATE TABLE zona (
  id          INT           NOT NULL AUTO_INCREMENT,
  nombre      VARCHAR(80)   NOT NULL,
  latitud     DECIMAL(9,6)  NOT NULL,
  longitud    DECIMAL(9,6)  NOT NULL,
  radio_max_m INT           NOT NULL,                     -- RN06
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

CREATE TABLE categoria (
  id          INT         NOT NULL AUTO_INCREMENT,
  nombre      VARCHAR(50) NOT NULL,
  created_at  DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at  DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  created_by  INT         NOT NULL,
  updated_by  INT         NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_categoria_nombre UNIQUE (nombre),
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
  duracion_estimada_min  SMALLINT      NOT NULL,          -- RN03
  hora_apertura          TIME          NULL,              -- [CONFIRMAR]
  hora_cierre            TIME          NULL,              -- [CONFIRMAR]
  estado                 VARCHAR(15)   NOT NULL DEFAULT 'ACTIVO',
  created_at             DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at             DATETIME(6)   NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  created_by             INT           NOT NULL,
  updated_by             INT           NOT NULL,
  PRIMARY KEY (id),
  KEY ix_lugar_estado_categoria (estado, categoria_id),
  CONSTRAINT ck_lugar_estado   CHECK (estado IN ('ACTIVO','EN_REVISION','INACTIVO')),
  CONSTRAINT ck_lugar_duracion CHECK (duracion_estimada_min > 0),
  CONSTRAINT ck_lugar_lat      CHECK (latitud  BETWEEN -90  AND 90),
  CONSTRAINT ck_lugar_lng      CHECK (longitud BETWEEN -180 AND 180),
  CONSTRAINT fk_lugar_categoria  FOREIGN KEY (categoria_id) REFERENCES categoria (id),
  CONSTRAINT fk_lugar_created_by FOREIGN KEY (created_by)   REFERENCES usuario (id),
  CONSTRAINT fk_lugar_updated_by FOREIGN KEY (updated_by)   REFERENCES usuario (id)
) ENGINE=InnoDB;

-- ============ HORARIOS Y PUENTES ============
CREATE TABLE horario (
  id          INT          NOT NULL AUTO_INCREMENT,
  usuario_id  INT          NOT NULL,
  materia     VARCHAR(100) NOT NULL,
  dia_semana  SMALLINT     NOT NULL,                      -- 1=lunes ... 6=sábado
  hora_inicio TIME         NOT NULL,
  hora_fin    TIME         NOT NULL,
  aula        VARCHAR(30)  NULL,
  origen      VARCHAR(10)  NOT NULL,                      -- MANUAL | FOTO (RF02)
  created_at  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at  DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  CONSTRAINT uq_horario_slot UNIQUE (usuario_id, dia_semana, hora_inicio),
  CONSTRAINT ck_horario_dia    CHECK (dia_semana BETWEEN 1 AND 6),
  CONSTRAINT ck_horario_horas  CHECK (hora_fin > hora_inicio),
  CONSTRAINT ck_horario_origen CHECK (origen IN ('MANUAL','FOTO')),
  CONSTRAINT fk_horario_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE puente (
  id          INT      NOT NULL AUTO_INCREMENT,
  usuario_id  INT      NOT NULL,
  dia_semana  SMALLINT NOT NULL,
  hora_inicio TIME     NOT NULL,
  hora_fin    TIME     NOT NULL,
  PRIMARY KEY (id),
  CONSTRAINT uq_puente_slot UNIQUE (usuario_id, dia_semana, hora_inicio),
  KEY ix_puente_matching (dia_semana, hora_inicio, hora_fin),
  CONSTRAINT ck_puente_dia   CHECK (dia_semana BETWEEN 1 AND 6),
  CONSTRAINT ck_puente_horas CHECK (hora_fin > hora_inicio),   -- el mínimo de 45 min (RN02) es parámetro (D8), no CHECK
  CONSTRAINT fk_puente_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

-- ============ RAG: TRAZABILIDAD DE LA IA ============
CREATE TABLE log_auditoria_rag (
  id                     BIGINT       NOT NULL AUTO_INCREMENT,
  usuario_id             INT          NOT NULL,
  tipo                   VARCHAR(25)  NOT NULL,
  zona_id                INT          NULL,
  radio_aplicado_m       INT          NULL,               -- copia del radio usado (RN06)
  puente_id              INT          NULL,
  tiempo_disponible_min  SMALLINT     NULL,               -- copia del puente al momento de la consulta
  entrada_usuario        TEXT         NULL,
  prompt_enviado         TEXT         NULL,               -- NULL si nunca se llamó al LLM
  respuesta_llm          TEXT         NULL,
  estado                 VARCHAR(20)  NOT NULL,
  modelo                 VARCHAR(40)  NULL,
  latencia_ms            INT          NULL,               -- evidencia de RNF02
  created_at             DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_log_created (created_at),
  KEY ix_log_usuario (usuario_id, created_at),
  CONSTRAINT ck_log_tipo   CHECK (tipo   IN ('RECOMENDACION','EXTRACCION_COMUNIDAD')),
  CONSTRAINT ck_log_estado CHECK (estado IN ('OK','SIN_RESULTADOS','RECHAZADO_SEGURIDAD','ERROR')),
  CONSTRAINT fk_log_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id),
  CONSTRAINT fk_log_zona    FOREIGN KEY (zona_id)    REFERENCES zona (id),
  CONSTRAINT fk_log_puente  FOREIGN KEY (puente_id)  REFERENCES puente (id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE lugar_recuperado (
  log_id              BIGINT   NOT NULL,
  lugar_id            INT      NOT NULL,
  distancia_m         INT      NOT NULL,
  tiempo_traslado_min SMALLINT NOT NULL,                  -- ida y vuelta (RN03)
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
  CONSTRAINT ck_reco_feedback  CHECK (feedback IN ('LIKE','DISLIKE')),
  CONSTRAINT ck_reco_fecha     CHECK ((feedback IS NULL) = (fecha_feedback IS NULL)),
  -- RN04 / RNF06: solo se puede recomendar un lugar recuperado en ESA misma consulta
  CONSTRAINT fk_reco_recuperado FOREIGN KEY (log_id, lugar_id) REFERENCES lugar_recuperado (log_id, lugar_id)
) ENGINE=InnoDB;

-- ============ COMUNIDADES EFÍMERAS ============
CREATE TABLE comunidad_efimera (
  id                INT          NOT NULL AUTO_INCREMENT,
  creador_id        INT          NOT NULL,                -- hace de "created_by"
  log_id            BIGINT       NULL,
  texto_original    TEXT         NOT NULL,
  actividad         VARCHAR(100) NOT NULL,
  inicio            DATETIME(6)  NOT NULL,
  fin               DATETIME(6)  NOT NULL,
  max_participantes SMALLINT     NOT NULL,
  lugar_id          INT          NULL,                    -- [CONFIRMAR] I9
  estado            VARCHAR(10)  NOT NULL DEFAULT 'ACTIVA',
  created_at        DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at        DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_comunidad_estado_fin (estado, fin),
  CONSTRAINT ck_com_fechas CHECK (fin > inicio),
  CONSTRAINT ck_com_estado CHECK (estado IN ('ACTIVA','ARCHIVADA')),
  CONSTRAINT ck_com_max    CHECK (max_participantes >= 2),
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
  CONSTRAINT ck_part_estado    CHECK (estado IN ('PENDIENTE','CONFIRMADO','RECHAZADO')),
  CONSTRAINT ck_part_respuesta CHECK ((estado = 'PENDIENTE') = (respondido_en IS NULL)),
  CONSTRAINT fk_part_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera (id),
  CONSTRAINT fk_part_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuario (id)
) ENGINE=InnoDB;

CREATE TABLE notificacion (
  id           BIGINT       NOT NULL AUTO_INCREMENT,
  usuario_id   INT          NOT NULL,
  tipo         VARCHAR(25)  NOT NULL,
  comunidad_id INT          NULL,                         -- sin puente_id: el aviso de puente es texto (ver hallazgo)
  mensaje      VARCHAR(200) NOT NULL,
  leida        BOOLEAN      NOT NULL DEFAULT FALSE,
  created_at   DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_notif_usuario (usuario_id, leida, created_at),
  CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA')),
  CONSTRAINT ck_notif_ref  CHECK ((tipo = 'PUENTE_DISPONIBLE' AND comunidad_id IS NULL)
                               OR (tipo <> 'PUENTE_DISPONIBLE' AND comunidad_id IS NOT NULL)),
  CONSTRAINT fk_notif_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuario (id),
  CONSTRAINT fk_notif_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera (id)
) ENGINE=InnoDB;

-- ============ MODERACIÓN ============
CREATE TABLE reporte (
  id           INT          NOT NULL AUTO_INCREMENT,
  usuario_id   INT          NOT NULL,                     -- autor del reporte (= created_by)
  lugar_id     INT          NULL,
  comunidad_id INT          NULL,
  motivo       VARCHAR(30)  NOT NULL,
  descripcion  TEXT         NULL,
  estado       VARCHAR(12)  NOT NULL DEFAULT 'PENDIENTE',
  revisor_id   INT          NULL,                         -- ADMIN que resolvió (= updated_by)
  created_at   DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  revisado_en  DATETIME(6)  NULL,                         -- (= updated_at)
  PRIMARY KEY (id),
  CONSTRAINT ck_rep_estado  CHECK (estado IN ('PENDIENTE','RESUELTO','DESCARTADO')),
  CONSTRAINT ck_rep_objeto  CHECK ((lugar_id IS NULL) <> (comunidad_id IS NULL)),
  CONSTRAINT ck_rep_revision CHECK ((estado = 'PENDIENTE' AND revisor_id IS NULL AND revisado_en IS NULL)
                                 OR (estado <> 'PENDIENTE' AND revisor_id IS NOT NULL AND revisado_en IS NOT NULL)),
  CONSTRAINT fk_rep_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuario (id),
  CONSTRAINT fk_rep_lugar     FOREIGN KEY (lugar_id)     REFERENCES lugar (id),
  CONSTRAINT fk_rep_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera (id),
  CONSTRAINT fk_rep_revisor   FOREIGN KEY (revisor_id)   REFERENCES usuario (id)
) ENGINE=InnoDB;

-- ============ AUDITORÍA: HISTORIAL DE CAMBIOS (valores antes/después) ============
CREATE TABLE historial_cambio (
  id                 BIGINT      NOT NULL AUTO_INCREMENT,
  entidad            VARCHAR(30) NOT NULL,
  entidad_id         INT         NOT NULL,                -- sin FK a propósito: el historial debe sobrevivir a la fila
  accion             VARCHAR(10) NOT NULL,
  valores_anteriores JSON        NULL,
  valores_nuevos     JSON        NULL,
  usuario_id         INT         NULL,                    -- actor; NULL = proceso del sistema
  created_at         DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_hist_entidad (entidad, entidad_id, created_at),
  KEY ix_hist_usuario (usuario_id, created_at),
  CONSTRAINT ck_hist_entidad CHECK (entidad IN ('USUARIO','LUGAR','ZONA')),
  CONSTRAINT ck_hist_accion  CHECK (accion IN ('CREATE','UPDATE','DELETE')),
  CONSTRAINT ck_hist_valores CHECK (
        (accion = 'CREATE' AND valores_anteriores IS NULL     AND valores_nuevos IS NOT NULL)
     OR (accion = 'UPDATE' AND valores_anteriores IS NOT NULL AND valores_nuevos IS NOT NULL)
     OR (accion = 'DELETE' AND valores_anteriores IS NOT NULL AND valores_nuevos IS NULL)),
  CONSTRAINT fk_hist_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
) ENGINE=InnoDB;

-- ============ MÉTRICAS RF20 (vista, no tabla) ============
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

-- HU13-HU15: comunidades efímeras, participaciones y RSVP.
CREATE TABLE IF NOT EXISTS comunidad_efimera (
  id INT NOT NULL AUTO_INCREMENT,
  creador_id INT NOT NULL,
  log_id BIGINT NULL,
  texto_original TEXT NOT NULL,
  actividad VARCHAR(100) NOT NULL,
  inicio DATETIME(6) NOT NULL,
  fin DATETIME(6) NOT NULL,
  max_participantes SMALLINT NOT NULL DEFAULT 6,
  lugar_id INT NULL,
  estado VARCHAR(10) NOT NULL DEFAULT 'ACTIVA',
  created_at DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  updated_at DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  PRIMARY KEY (id),
  KEY ix_comunidad_estado_fin (estado, fin),
  CONSTRAINT fk_com_010_creador FOREIGN KEY (creador_id) REFERENCES usuario(id),
  CONSTRAINT fk_com_010_lugar FOREIGN KEY (lugar_id) REFERENCES lugar(id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS participacion (
  comunidad_id INT NOT NULL,
  usuario_id INT NOT NULL,
  estado VARCHAR(10) NOT NULL DEFAULT 'PENDIENTE',
  invitado_en DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
  respondido_en DATETIME(6) NULL,
  PRIMARY KEY (comunidad_id, usuario_id),
  CONSTRAINT fk_part_010_comunidad FOREIGN KEY (comunidad_id) REFERENCES comunidad_efimera(id),
  CONSTRAINT fk_part_010_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id)
) ENGINE=InnoDB;

ALTER TABLE comunidad_efimera
  ADD CONSTRAINT uq_com_log UNIQUE (log_id),
  DROP CHECK ck_com_max,
  ADD CONSTRAINT ck_com_max CHECK (max_participantes BETWEEN 2 AND 30);

ALTER TABLE notificacion
  ADD CONSTRAINT uq_notif_comunidad UNIQUE (usuario_id, comunidad_id, tipo);

ALTER TABLE usuario
  ADD COLUMN visible_matching       BOOLEAN     NOT NULL DEFAULT FALSE AFTER notificaciones_activas,
  ADD COLUMN fecha_visible_matching DATETIME(6) NULL AFTER visible_matching,
  ADD KEY ix_usuario_matching (zona_actual_id, visible_matching),
  ADD CONSTRAINT ck_usuario_visible_matching CHECK (visible_matching = FALSE OR fecha_visible_matching IS NOT NULL);

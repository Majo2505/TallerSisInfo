-- Lleva una base creada con la versión anterior de schema.sql a la actual.

-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- Si una sentencia falla con "Duplicate column name", la migración ya estaba aplicada.
ALTER TABLE usuario
  ADD COLUMN notificaciones_activas BOOLEAN NOT NULL DEFAULT TRUE AFTER fecha_consentimiento;

ALTER TABLE notificacion
  DROP CHECK ck_notif_ref,
  DROP INDEX ix_notif_usuario,
  ADD COLUMN puente_fecha       DATE    NULL AFTER comunidad_id,
  ADD COLUMN puente_hora_inicio TIME    NULL AFTER puente_fecha,
  ADD COLUMN puente_hora_fin    TIME    NULL AFTER puente_hora_inicio,
  ADD COLUMN activa             BOOLEAN NOT NULL DEFAULT TRUE AFTER leida,
  ADD CONSTRAINT uq_notif_puente UNIQUE (usuario_id, puente_fecha, puente_hora_inicio),
  ADD KEY ix_notif_usuario (usuario_id, activa, leida, created_at),
  ADD CONSTRAINT ck_notif_ref CHECK ((tipo = 'PUENTE_DISPONIBLE' AND comunidad_id IS NULL
                                      AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL)
                                  OR (tipo <> 'PUENTE_DISPONIBLE' AND comunidad_id IS NOT NULL
                                      AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL)),
  ADD CONSTRAINT ck_notif_puente_horas CHECK (puente_hora_fin > puente_hora_inicio);

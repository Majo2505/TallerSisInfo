-- HU13: creación automática de comunidades efímeras y notificación. Lleva una base creada con la versión anterior
-- de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate key name", la migración ya estaba aplicada.
-- Requiere MySQL 8.0.19 o superior (DROP CHECK).
--
-- Las tablas comunidad_efimera, participacion y notificacion ya existen; solo se agregan las reglas de HU13:
--   · uq_com_log: cada extracción de HU11 (log_auditoria_rag) crea como mucho UNA comunidad. Si el estudiante pulsa
--     «Crear» dos veces o el pedido se repite, la base lo rechaza.
--   · ck_com_max: de 2 a 30 personas, contando a quien la crea (el mismo rango que valida HU11).
--   · uq_notif_comunidad: un estudiante recibe una sola invitación por comunidad. En los avisos de puente
--     comunidad_id es NULL, así que no les afecta.

ALTER TABLE comunidad_efimera
  ADD CONSTRAINT uq_com_log UNIQUE (log_id),
  DROP CHECK ck_com_max,
  ADD CONSTRAINT ck_com_max CHECK (max_participantes BETWEEN 2 AND 30);

ALTER TABLE notificacion
  ADD CONSTRAINT uq_notif_comunidad UNIQUE (usuario_id, comunidad_id, tipo);

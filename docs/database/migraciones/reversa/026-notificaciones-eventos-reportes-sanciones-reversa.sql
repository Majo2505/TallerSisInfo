SET NAMES utf8mb4;

-- Reversa de la 026: devuelve notificacion a como estaba (4 tipos de aviso y sin evento_id, reporte_id ni sancion_id).
-- Se ejecuta con la base de datos ya seleccionada (sin USE).
-- ABORTA si ya hay avisos de los tipos nuevos: bórralos antes (DELETE FROM notificacion WHERE tipo IN (...)) o restaura el
-- respaldo.

DROP PROCEDURE IF EXISTS fase0_abortar_si;
DELIMITER //
CREATE PROCEDURE fase0_abortar_si(IN filas BIGINT, IN mensaje VARCHAR(200))
BEGIN
  IF filas > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = mensaje;
  END IF;
END//
DELIMITER ;

SELECT COUNT(*) INTO @n FROM notificacion
 WHERE tipo IN ('EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA');
CALL fase0_abortar_si(@n, 'Hay avisos de eventos, reportes o sanciones: borra esas notificaciones antes de revertir la 026.');

ALTER TABLE notificacion
  DROP FOREIGN KEY fk_notif_evento,
  DROP FOREIGN KEY fk_notif_reporte,
  DROP FOREIGN KEY fk_notif_sancion;

ALTER TABLE notificacion
  DROP INDEX uq_notif_evento,
  DROP INDEX uq_notif_reporte,
  DROP INDEX uq_notif_sancion,
  DROP INDEX fk_notif_evento,
  DROP INDEX fk_notif_reporte,
  DROP INDEX fk_notif_sancion;

ALTER TABLE notificacion
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref;

ALTER TABLE notificacion
  DROP COLUMN evento_id,
  DROP COLUMN reporte_id,
  DROP COLUMN sancion_id;

ALTER TABLE notificacion
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA')),
  ADD CONSTRAINT ck_notif_ref  CHECK ((tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES') AND comunidad_id IS NULL
                                       AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL)
                                   OR (tipo NOT IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES') AND comunidad_id IS NOT NULL
                                       AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL));

DROP PROCEDURE IF EXISTS fase0_abortar_si;

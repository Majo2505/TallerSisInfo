SET NAMES utf8mb4;

-- Reversa de la 029: quita test_afinidad, conexion y notificacion.conexion_id, y deja notificacion como la dejó la 026.
-- Se ejecuta con la base de datos ya seleccionada (sin USE).
-- ABORTA si ya hay tests, conexiones o avisos de conexión: bórralos antes (DELETE FROM notificacion WHERE conexion_id IS
-- NOT NULL; DELETE FROM conexion; DELETE FROM test_afinidad) o restaura el respaldo.

DROP PROCEDURE IF EXISTS fase0_abortar_si;
DELIMITER //
CREATE PROCEDURE fase0_abortar_si(IN filas BIGINT, IN mensaje VARCHAR(200))
BEGIN
  IF filas > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = mensaje;
  END IF;
END//
DELIMITER ;

SELECT COUNT(*) INTO @n FROM notificacion WHERE conexion_id IS NOT NULL;
CALL fase0_abortar_si(@n, 'Hay avisos de conexión: borra esas notificaciones antes de revertir la 029.');
SELECT COUNT(*) INTO @n FROM conexion;
CALL fase0_abortar_si(@n, 'Hay conexiones guardadas: bórralas antes de revertir la 029.');
SELECT COUNT(*) INTO @n FROM test_afinidad;
CALL fase0_abortar_si(@n, 'Hay tests de afinidad guardados: bórralos antes de revertir la 029.');
DROP PROCEDURE fase0_abortar_si;

ALTER TABLE notificacion
  DROP FOREIGN KEY fk_notif_conexion;

ALTER TABLE notificacion
  DROP INDEX uq_notif_conexion,
  DROP INDEX fk_notif_conexion;

ALTER TABLE notificacion
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref;

ALTER TABLE notificacion
  DROP COLUMN conexion_id;

ALTER TABLE notificacion
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA',
                                               'EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA')),
  ADD CONSTRAINT ck_notif_ref CHECK (
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
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL));

DROP TABLE conexion;
DROP TABLE test_afinidad;

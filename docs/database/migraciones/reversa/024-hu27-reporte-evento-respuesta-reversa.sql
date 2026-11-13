SET NAMES utf8mb4;

-- Reversa de la 024: devuelve reporte a como estaba (sin evento_id ni respuesta, y sin el motivo EVENTO_INAPROPIADO).
-- Se ejecuta con la base de datos ya seleccionada (sin USE). Hay que revertir antes la 026.
-- ABORTA si ya hay reportes de eventos o con respuesta: bórralos o restaura el respaldo.

DROP PROCEDURE IF EXISTS fase0_abortar_si;
DELIMITER //
CREATE PROCEDURE fase0_abortar_si(IN filas BIGINT, IN mensaje VARCHAR(200))
BEGIN
  IF filas > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = mensaje;
  END IF;
END//
DELIMITER ;

SELECT COUNT(*) INTO @n FROM reporte
 WHERE evento_id IS NOT NULL OR respuesta IS NOT NULL OR motivo = 'EVENTO_INAPROPIADO';
CALL fase0_abortar_si(@n, 'Hay reportes de eventos o con respuesta del admin: bórralos antes de revertir la 024.');

ALTER TABLE reporte DROP FOREIGN KEY fk_rep_evento;
ALTER TABLE reporte DROP INDEX fk_rep_evento;

ALTER TABLE reporte
  DROP CHECK ck_rep_respuesta,
  DROP CHECK ck_rep_objeto,
  DROP CHECK ck_rep_motivo;

ALTER TABLE reporte
  DROP COLUMN respuesta,
  DROP COLUMN evento_id;

ALTER TABLE reporte
  ADD CONSTRAINT ck_rep_motivo CHECK (motivo IN ('ERROR_APP','SUGERENCIA','LUGAR_INCORRECTO','COMUNIDAD_INAPROPIADA')),
  ADD CONSTRAINT ck_rep_objeto CHECK ((motivo = 'LUGAR_INCORRECTO'      AND lugar_id IS NOT NULL AND comunidad_id IS NULL)
                                   OR (motivo = 'COMUNIDAD_INAPROPIADA' AND comunidad_id IS NOT NULL AND lugar_id IS NULL)
                                   OR (motivo IN ('ERROR_APP','SUGERENCIA') AND lugar_id IS NULL AND comunidad_id IS NULL));

DROP PROCEDURE IF EXISTS fase0_abortar_si;

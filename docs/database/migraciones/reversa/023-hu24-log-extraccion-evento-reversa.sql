SET NAMES utf8mb4;

-- Reversa de la 023: log_auditoria_rag vuelve a admitir solo RECOMENDACION y EXTRACCION_COMUNIDAD.
-- Se ejecuta con la base de datos ya seleccionada (sin USE).
-- ABORTA si ya hay registros EXTRACCION_EVENTO: bórralos o restaura el respaldo.

DROP PROCEDURE IF EXISTS fase0_abortar_si;
DELIMITER //
CREATE PROCEDURE fase0_abortar_si(IN filas BIGINT, IN mensaje VARCHAR(200))
BEGIN
  IF filas > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = mensaje;
  END IF;
END//
DELIMITER ;

SELECT COUNT(*) INTO @n FROM log_auditoria_rag WHERE tipo = 'EXTRACCION_EVENTO';
CALL fase0_abortar_si(@n, 'Hay registros EXTRACCION_EVENTO: bórralos antes de revertir la 023.');

ALTER TABLE log_auditoria_rag
  DROP CHECK ck_log_tipo,
  DROP CHECK ck_log_datos_tipo,
  ADD CONSTRAINT ck_log_tipo       CHECK (tipo IN ('RECOMENDACION','EXTRACCION_COMUNIDAD')),
  ADD CONSTRAINT ck_log_datos_tipo CHECK (datos_extraidos IS NULL OR tipo = 'EXTRACCION_COMUNIDAD');

DROP PROCEDURE IF EXISTS fase0_abortar_si;

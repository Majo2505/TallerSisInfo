SET NAMES utf8mb4;

-- Reversa de la 022: elimina los eventos y su imagen, y historial_cambio vuelve a admitir solo USUARIO, LUGAR y ZONA.
-- BORRA todos los eventos. Hay que revertir antes la 026 y la 024 (apuntan a evento).
-- Se ejecuta con la base de datos ya seleccionada (sin USE).
-- ABORTA si ya hay historial de eventos: bórralo o restaura el respaldo.

DROP PROCEDURE IF EXISTS fase0_abortar_si;
DELIMITER //
CREATE PROCEDURE fase0_abortar_si(IN filas BIGINT, IN mensaje VARCHAR(200))
BEGIN
  IF filas > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = mensaje;
  END IF;
END//
DELIMITER ;

SELECT COUNT(*) INTO @n FROM historial_cambio WHERE entidad = 'EVENTO';
CALL fase0_abortar_si(@n, 'Hay historial de eventos: bórralo antes de revertir la 022.');

DROP TABLE IF EXISTS evento_imagen;
DROP TABLE IF EXISTS evento;

ALTER TABLE historial_cambio
  DROP CHECK ck_hist_entidad,
  ADD CONSTRAINT ck_hist_entidad CHECK (entidad IN ('USUARIO','LUGAR','ZONA'));

DROP PROCEDURE IF EXISTS fase0_abortar_si;

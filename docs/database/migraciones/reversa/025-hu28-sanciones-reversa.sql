SET NAMES utf8mb4;

-- Reversa de la 025: elimina la tabla de sanciones. BORRA todas las sanciones.
-- Hay que revertir antes la 026 (notificacion.sancion_id apunta a sancion).
-- Se ejecuta con la base de datos ya seleccionada (sin USE).

DROP TABLE IF EXISTS sancion;

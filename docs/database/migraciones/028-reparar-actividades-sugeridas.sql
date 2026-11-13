SET NAMES utf8mb4;

-- Repara las tildes mal guardadas en lugar.actividades_sugeridas (bug #305). La 016 reparó los demás textos pero no esta
-- columna. Los seeds se cargaron sin utf8mb4 desde una consola de Windows: el UTF-8 se leyó como cp850 y el carácter «├»
-- (E2 94 9C) quedó guardado en el texto («r├ípida» en vez de «rápida»).
-- Se ejecuta con la base de datos ya seleccionada (sin USE), con --default-character-set=utf8mb4.
-- Es idempotente y segura en cualquier base: solo toca las filas rotas (HEX con E2949C). En una base sana no cambia nada.
-- Esta migración repara datos y no tiene reversa: si hace falta deshacerla, se restaura el respaldo.
--
-- Después de correrla, esta consulta debe dar 0 filas:
--   SELECT id, nombre FROM lugar WHERE HEX(actividades_sugeridas) LIKE '%E2949C%';

UPDATE IGNORE lugar
   SET actividades_sugeridas = CONVERT(CAST(CONVERT(actividades_sugeridas USING cp850) AS BINARY) USING utf8mb4)
 WHERE HEX(actividades_sugeridas) LIKE '%E2949C%';

SET NAMES utf8mb4;

-- Reversa de la 028: no hay. La 028 repara datos (tildes mal guardadas) y volver a romperlos no tiene sentido.
-- Si hiciera falta deshacerla, se restaura el respaldo de la base.
SELECT 'La 028 repara datos y no tiene reversa: restaura el respaldo si hace falta.' AS aviso;

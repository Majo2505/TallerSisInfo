SET NAMES utf8mb4;

-- Reversa de la 027: elimina las etiquetas de actividad. BORRA el catálogo y la asignación de etiquetas a los lugares.
-- Se ejecuta con la base de datos ya seleccionada (sin USE).

DROP TABLE IF EXISTS lugar_etiqueta;
DROP TABLE IF EXISTS etiqueta_actividad;

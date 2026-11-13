SET NAMES utf8mb4;

-- BREAKU · Seed de DESARROLLO: etiquetas de tipo de actividad de los lugares (HU30).
-- SOLO para desarrollo y la base compartida de pruebas. NUNCA ejecutar en producción.
--
-- Requisitos: la migración 027-hu30-etiquetas.sql (o la base creada con el schema.sql actual, que ya trae las tablas y el
-- catálogo de 8 etiquetas) y los lugares de seed-dev-catalogo.sql y seed-dev-cochabamba.sql.
-- Se ejecuta con la base de datos ya seleccionada (sin USE) y con --default-character-set=utf8mb4: los nombres llevan tildes.
-- Es re-ejecutable: asigna por el NOMBRE EXACTO del lugar y de la etiqueta, no repite lo que ya está asignado y no falla si
-- un lugar no existe en esa base.
--
-- Qué hace: asigna las etiquetas a los 14 lugares recomendables. «Cafetería Central» (La Terraza, la cafetería de la
-- universidad) lleva dos etiquetas. «Kiosco Nuevo (en revisión)» NO se etiqueta: está EN_REVISION y se etiqueta al activarlo
-- desde el formulario del admin. «Mirador del Sur (fuera del radio)» es una asignación dudosa que debe confirmar alguien que
-- conozca el campus.
--
-- Al terminar, esta consulta debe dar 14 (lugares con al menos una etiqueta):
--   SELECT COUNT(DISTINCT lugar_id) FROM lugar_etiqueta;

INSERT IGNORE INTO lugar_etiqueta (lugar_id, etiqueta_id)
SELECT l.id, e.id
FROM (
  SELECT 'Cafetería Central' AS lugar, 'Café y bebidas' AS etiqueta UNION ALL
  SELECT 'HAPPY TEA', 'Café y bebidas' UNION ALL
  SELECT 'Latte', 'Café y bebidas' UNION ALL
  SELECT 'Cafetería Central', 'Comer' UNION ALL
  SELECT '911 Fast Food', 'Comer' UNION ALL
  SELECT 'Cairo Restaurante', 'Comer' UNION ALL
  SELECT 'Pizzeria Marechiaro', 'Comer' UNION ALL
  SELECT 'Cancha Polideportiva', 'Deportes' UNION ALL
  SELECT 'Parque Jorge Trigo Andia', 'Caminar y aire libre' UNION ALL
  SELECT 'Mirador del Sur (fuera del radio)', 'Caminar y aire libre' UNION ALL
  SELECT 'Capilla San Nicolás de Tolentino', 'Descanso y silencio' UNION ALL
  SELECT 'Jardín Interior', 'Descanso y silencio' UNION ALL
  SELECT 'Biblioteca Central', 'Estudio' UNION ALL
  SELECT 'Biblioteca Tupuraya - UCB Cochabamba', 'Estudio' UNION ALL
  SELECT 'Sala de Estudio Grupal', 'Estudio'
) AS m
JOIN lugar l              ON l.nombre = m.lugar
JOIN etiqueta_actividad e ON e.nombre = m.etiqueta;

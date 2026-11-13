-- Descripción normalizada del esquema de la base seleccionada (sin el nombre de la base), para comparar dos bases con diff.
-- Solo SELECT. La usa verificar-fase0.sh.

SELECT 'columnas' AS seccion;
SELECT table_name, column_name, ordinal_position, column_type, is_nullable, IFNULL(column_default, '<NULL>'), extra, IFNULL(collation_name, '')
  FROM information_schema.columns
 WHERE table_schema = DATABASE()
 ORDER BY table_name, ordinal_position;

SELECT 'indices' AS seccion;
SELECT table_name, index_name, seq_in_index, column_name, non_unique, IFNULL(sub_part, '')
  FROM information_schema.statistics
 WHERE table_schema = DATABASE()
 ORDER BY table_name, index_name, seq_in_index;

SELECT 'restricciones' AS seccion;
SELECT tc.table_name, tc.constraint_name, tc.constraint_type, IFNULL(cc.check_clause, '')
  FROM information_schema.table_constraints tc
  LEFT JOIN information_schema.check_constraints cc
         ON cc.constraint_schema = tc.constraint_schema AND cc.constraint_name = tc.constraint_name
 WHERE tc.table_schema = DATABASE()
 ORDER BY tc.table_name, tc.constraint_name;

SELECT 'claves_foraneas' AS seccion;
SELECT k.table_name, k.constraint_name, k.column_name, k.referenced_table_name, k.referenced_column_name, r.update_rule, r.delete_rule
  FROM information_schema.key_column_usage k
  JOIN information_schema.referential_constraints r
    ON r.constraint_schema = k.constraint_schema AND r.constraint_name = k.constraint_name AND r.table_name = k.table_name
 WHERE k.table_schema = DATABASE() AND k.referenced_table_name IS NOT NULL
 ORDER BY k.table_name, k.constraint_name, k.ordinal_position;

SELECT 'vistas' AS seccion;
SELECT table_name, view_definition FROM information_schema.views WHERE table_schema = DATABASE() ORDER BY table_name;

SELECT 'catalogo_etiquetas' AS seccion;
SELECT nombre FROM etiqueta_actividad ORDER BY nombre;

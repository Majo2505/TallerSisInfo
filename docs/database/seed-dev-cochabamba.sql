-- BREAKU · Seed de DESARROLLO: sede UCB Cochabamba (zona, categoría de referencia y 13 lugares).
-- Generado con docs/datos/generar_seed_cochabamba.py a partir de docs/datos/lugares_ucb_cochabamba.json. No lo edites a mano.
-- SOLO para desarrollo local. NUNCA ejecutar en producción.
--
-- Requisitos: haber ejecutado seed-dev.sql (usa al usuario de prueba como created_by / updated_by) y la
-- migración 004-sede-cochabamba.sql (o haber creado la base con el schema.sql actual).
-- Se ejecuta con la base de datos ya seleccionada (sin USE). Es idempotente: se puede correr varias veces.
--
-- Qué hace:
--   · Crea (o actualiza) la sede «Campus UCB Cochabamba» como sede activa, con un radio de 500 m.
--   · Deja las demás sedes (La Paz) guardadas pero inactivas: no aparecen para elegir ni reciben recomendaciones.
--   · Crea la categoría «Referencia» y reutiliza Comida, Estudio, Descanso y Deporte.
--   · Carga los lugares con validado = FALSE: el equipo tiene que revisarlos. Los puntos de referencia
--     (campus, parqueo, bloques) quedan con recomendable = FALSE y nunca se recomiendan.
--
-- Para volver a La Paz: UPDATE zona SET activa = (nombre = 'Campus UCB La Paz');

SET @autor := (SELECT id FROM usuario WHERE correo = 'dev.estudiante@ucb.edu.bo');

INSERT INTO zona (nombre, latitud, longitud, radio_max_m, activa, created_by, updated_by)
VALUES ('Campus UCB Cochabamba', -17.371672, -66.143916, 500, TRUE, @autor, @autor) AS nueva
ON DUPLICATE KEY UPDATE
  latitud = nueva.latitud, longitud = nueva.longitud, radio_max_m = nueva.radio_max_m,
  activa = TRUE, updated_at = UTC_TIMESTAMP(6);

UPDATE zona SET activa = FALSE, updated_at = UTC_TIMESTAMP(6) WHERE nombre <> 'Campus UCB Cochabamba' AND activa = TRUE;

INSERT IGNORE INTO categoria (nombre, created_by, updated_by) VALUES
  ('Comida',     @autor, @autor),
  ('Estudio',    @autor, @autor),
  ('Descanso',   @autor, @autor),
  ('Deporte',    @autor, @autor),
  ('Referencia', @autor, @autor);

INSERT INTO lugar (nombre, descripcion, categoria_id, latitud, longitud, duracion_estimada_min, estado,
                   tipo_lugar, modalidad, nivel_energia, duracion_min, duracion_max, actividades_sugeridas,
                   min_a_pie_desde_bloque_a, recomendable, validado, created_by, updated_by)
SELECT d.nombre, d.descripcion, c.id, d.latitud, d.longitud, d.duracion, 'ACTIVO',
       d.tipo_lugar, d.modalidad, d.nivel_energia, d.duracion_min, d.duracion_max, d.actividades,
       d.min_a_pie, d.recomendable, d.validado, @autor, @autor
FROM (
  SELECT 'Universidad Católica Boliviana - Cochabamba' AS nombre, 'Punto de referencia del campus. No se recomienda.' AS descripcion, 'Referencia' AS categoria, -17.371672 AS latitud, -66.143916 AS longitud, 1 AS duracion, 'referencia' AS tipo_lugar, NULL AS modalidad, NULL AS nivel_energia, NULL AS duracion_min, NULL AS duracion_max, NULL AS actividades, 1 AS min_a_pie, FALSE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Parqueo UCB' AS nombre, 'Punto de referencia del campus. No se recomienda.' AS descripcion, 'Referencia' AS categoria, -17.372287 AS latitud, -66.144082 AS longitud, 1 AS duracion, 'referencia' AS tipo_lugar, NULL AS modalidad, NULL AS nivel_energia, NULL AS duracion_min, NULL AS duracion_max, NULL AS actividades, 1 AS min_a_pie, FALSE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Bloque A' AS nombre, 'Punto de referencia del campus. No se recomienda.' AS descripcion, 'Referencia' AS categoria, -17.371932 AS latitud, -66.143664 AS longitud, 1 AS duracion, 'referencia' AS tipo_lugar, NULL AS modalidad, NULL AS nivel_energia, NULL AS duracion_min, NULL AS duracion_max, NULL AS actividades, 0 AS min_a_pie, FALSE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Bloque B' AS nombre, 'Punto de referencia del campus. No se recomienda.' AS descripcion, 'Referencia' AS categoria, -17.372240 AS latitud, -66.143711 AS longitud, 1 AS duracion, 'referencia' AS tipo_lugar, NULL AS modalidad, NULL AS nivel_energia, NULL AS duracion_min, NULL AS duracion_max, NULL AS actividades, 1 AS min_a_pie, FALSE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Bloque C' AS nombre, 'Punto de referencia del campus. No se recomienda.' AS descripcion, 'Referencia' AS categoria, -17.372602 AS latitud, -66.143153 AS longitud, 1 AS duracion, 'referencia' AS tipo_lugar, NULL AS modalidad, NULL AS nivel_energia, NULL AS duracion_min, NULL AS duracion_max, NULL AS actividades, 1 AS min_a_pie, FALSE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Biblioteca Tupuraya - UCB Cochabamba' AS nombre, 'Actividades: estudiar, leer, adelantar tareas.' AS descripcion, 'Estudio' AS categoria, -17.371851 AS latitud, -66.144054 AS longitud, 20 AS duracion, 'campus' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'bajo' AS nivel_energia, 20 AS duracion_min, 120 AS duracion_max, '["estudiar", "leer", "adelantar tareas"]' AS actividades, 1 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Capilla San Nicolás de Tolentino' AS nombre, 'Actividades: silencio, oración, pausa tranquila.' AS descripcion, 'Descanso' AS categoria, -17.373221 AS latitud, -66.143153 AS longitud, 10 AS duracion, 'campus' AS tipo_lugar, 'solitario' AS modalidad, 'bajo' AS nivel_energia, 10 AS duracion_min, 30 AS duracion_max, '["silencio", "oración", "pausa tranquila"]' AS actividades, 2 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Parque Jorge Trigo Andia' AS nombre, 'Actividades: caminar, trotar, estirar, tomar aire.' AS descripcion, 'Deporte' AS categoria, -17.371000 AS latitud, -66.144362 AS longitud, 15 AS duracion, 'aire_libre' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'medio,alto' AS nivel_energia, 15 AS duracion_min, 60 AS duracion_max, '["caminar", "trotar", "estirar", "tomar aire"]' AS actividades, 2 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Cairo Restaurante' AS nombre, 'Actividades: almorzar, comer con amigos.' AS descripcion, 'Comida' AS categoria, -17.371926 AS latitud, -66.142899 AS longitud, 30 AS duracion, 'comida' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'bajo,medio' AS nivel_energia, 30 AS duracion_min, 60 AS duracion_max, '["almorzar", "comer con amigos"]' AS actividades, 1 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT '911 Fast Food' AS nombre, 'Actividades: comida rápida, snack.' AS descripcion, 'Comida' AS categoria, -17.371234 AS latitud, -66.143821 AS longitud, 20 AS duracion, 'comida' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'bajo,medio' AS nivel_energia, 20 AS duracion_min, 45 AS duracion_max, '["comida rápida", "snack"]' AS actividades, 1 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Pizzeria Marechiaro' AS nombre, 'Actividades: pizza, comer con amigos.' AS descripcion, 'Comida' AS categoria, -17.373012 AS latitud, -66.143006 AS longitud, 40 AS duracion, 'comida' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'bajo,medio' AS nivel_energia, 40 AS duracion_min, 75 AS duracion_max, '["pizza", "comer con amigos"]' AS actividades, 2 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'HAPPY TEA' AS nombre, 'Actividades: té, bebida, conversar.' AS descripcion, 'Comida' AS categoria, -17.371108 AS latitud, -66.144798 AS longitud, 20 AS duracion, 'comida' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'bajo' AS nivel_energia, 20 AS duracion_min, 45 AS duracion_max, '["té", "bebida", "conversar"]' AS actividades, 2 AS min_a_pie, TRUE AS recomendable, FALSE AS validado UNION ALL
  SELECT 'Latte' AS nombre, 'Actividades: café, conversar, trabajar con laptop.' AS descripcion, 'Comida' AS categoria, -17.373775 AS latitud, -66.145300 AS longitud, 20 AS duracion, 'comida' AS tipo_lugar, 'solitario,grupal' AS modalidad, 'bajo' AS nivel_energia, 20 AS duracion_min, 45 AS duracion_max, '["café", "conversar", "trabajar con laptop"]' AS actividades, 4 AS min_a_pie, TRUE AS recomendable, FALSE AS validado
) AS d
JOIN categoria c ON c.nombre = d.categoria
WHERE NOT EXISTS (SELECT 1 FROM lugar l WHERE l.nombre = d.nombre);

SET NAMES utf8mb4;

-- BREAKU · Seed de DESARROLLO: eventos de ejemplo del campus (HU22 a HU26).
-- SOLO para desarrollo y la base compartida de pruebas. NUNCA ejecutar en producción. Sin datos reales de estudiantes.
--
-- Requisitos: la migración 022-hu22-eventos.sql (o la base creada con el schema.sql actual) y los usuarios y lugares de
-- seed-dev.sql, seed-dev-catalogo.sql, seed-dev-cochabamba.sql y seed-dev-matching.sql.
-- Se ejecuta con la base de datos ya seleccionada (sin USE) y con --default-character-set=utf8mb4.
--
-- Es re-ejecutable y sirve para REFRESCAR las fechas: los eventos de ejemplo llevan «[Prueba]» en el título; si ya existen,
-- se actualizan al día en que corres el script (hoy y los días siguientes, en hora de Bolivia, UTC-4) y se dejan como
-- en este archivo; si faltan, se crean. No toca ningún otro evento. Si un usuario o un lugar no existe en esa base, ese
-- evento se omite sin error.
-- Para quitar los eventos de ejemplo: DELETE FROM evento WHERE titulo LIKE '[Prueba]%';
--   (falla mientras haya notificaciones o reportes que apunten a ellos: bórralos antes).
--
-- Los eventos (los dos bloques de abajo tienen la misma lista; si cambias uno, cambia el otro):
--   · Hoy: una feria LIBRE de 8:00 a 13:00, un partido FIJO de 16:00 a 17:00 y una charla FIJA de 12:30 a 14:00 (del admin).
--   · Esta semana: un taller LIBRE (en 2 días), un concierto fuera del campus (en 5 días, solo con lugar en texto libre).
--   · Un evento PENDIENTE (en 3 días, «En revisión», con su motivo).

SET @hoy := DATE(UTC_TIMESTAMP() - INTERVAL 4 HOUR);

UPDATE evento e
JOIN usuario u ON u.id = e.creador_id
JOIN (
  SELECT '[Prueba] Feria de libros' AS titulo, 'dev.estudiante@ucb.edu.bo' AS correo, 'Feria de libros usados en la biblioteca.' AS descripcion,
         'Biblioteca Central' AS lugar, NULL AS lugar_texto, 0 AS dias, '08:00:00' AS hora_ini, '13:00:00' AS hora_fin,
         'PUBLICADO' AS estado, 'FORMULARIO' AS origen, 'LIBRE' AS asistencia, 20 AS visita_min, 60 AS visita_max, NULL AS motivo UNION ALL
  SELECT '[Prueba] Partido de fútbol de salón', 'dev.ana.quiroga@ucb.edu.bo', 'Partido amistoso entre carreras.',
         'Cancha Polideportiva', NULL, 0, '16:00:00', '17:00:00', 'PUBLICADO', 'FORMULARIO', 'FIJA', NULL, NULL, NULL UNION ALL
  SELECT '[Prueba] Charla de bienvenida', 'dev.admin@ucb.edu.bo', 'Charla corta para estudiantes nuevos.',
         'Parque Jorge Trigo Andia', NULL, 0, '12:30:00', '14:00:00', 'PUBLICADO', 'ADMIN', 'FIJA', NULL, NULL, NULL UNION ALL
  SELECT '[Prueba] Taller de estudio en grupo', 'dev.diego.mamani@ucb.edu.bo', 'Taller de técnicas de estudio.',
         'Sala de Estudio Grupal', NULL, 2, '10:00:00', '12:00:00', 'PUBLICADO', 'FORMULARIO', 'LIBRE', 15, 45, NULL UNION ALL
  SELECT '[Prueba] Concierto de la semana', 'dev.lucia.rojas@ucb.edu.bo', 'Concierto de bandas universitarias.',
         NULL, 'Auditorio municipal (fuera del campus)', 5, '18:00:00', '20:00:00', 'PUBLICADO', 'FORMULARIO', 'FIJA', NULL, NULL, NULL UNION ALL
  SELECT '[Prueba] Torneo en revisión', 'dev.sofia.flores@ucb.edu.bo', 'Torneo de ajedrez que espera la revisión del admin.',
         NULL, 'Canchas del barrio (fuera del campus)', 3, '15:00:00', '16:00:00', 'PENDIENTE', 'FORMULARIO', 'FIJA', NULL, NULL, 'El lugar no está en el catálogo.'
) AS d ON d.titulo = e.titulo AND d.correo = u.correo
LEFT JOIN lugar l ON l.nombre = d.lugar
SET e.descripcion = d.descripcion,
    e.lugar_id    = l.id,
    e.lugar_texto = d.lugar_texto,
    e.inicio      = TIMESTAMP(DATE_ADD(@hoy, INTERVAL d.dias DAY), d.hora_ini) + INTERVAL 4 HOUR,
    e.fin         = TIMESTAMP(DATE_ADD(@hoy, INTERVAL d.dias DAY), d.hora_fin) + INTERVAL 4 HOUR,
    e.estado      = d.estado,
    e.origen      = d.origen,
    e.asistencia  = d.asistencia,
    e.visita_min  = d.visita_min,
    e.visita_max  = d.visita_max,
    e.motivo      = d.motivo,
    e.updated_at  = UTC_TIMESTAMP(6)
WHERE e.titulo LIKE '[Prueba]%';

INSERT INTO evento (creador_id, titulo, descripcion, lugar_id, lugar_texto, inicio, fin, estado, origen, asistencia, visita_min, visita_max, motivo)
SELECT u.id, d.titulo, d.descripcion, l.id, d.lugar_texto,
       TIMESTAMP(DATE_ADD(@hoy, INTERVAL d.dias DAY), d.hora_ini) + INTERVAL 4 HOUR,
       TIMESTAMP(DATE_ADD(@hoy, INTERVAL d.dias DAY), d.hora_fin) + INTERVAL 4 HOUR,
       d.estado, d.origen, d.asistencia, d.visita_min, d.visita_max, d.motivo
FROM (
  SELECT '[Prueba] Feria de libros' AS titulo, 'dev.estudiante@ucb.edu.bo' AS correo, 'Feria de libros usados en la biblioteca.' AS descripcion,
         'Biblioteca Central' AS lugar, NULL AS lugar_texto, 0 AS dias, '08:00:00' AS hora_ini, '13:00:00' AS hora_fin,
         'PUBLICADO' AS estado, 'FORMULARIO' AS origen, 'LIBRE' AS asistencia, 20 AS visita_min, 60 AS visita_max, NULL AS motivo UNION ALL
  SELECT '[Prueba] Partido de fútbol de salón', 'dev.ana.quiroga@ucb.edu.bo', 'Partido amistoso entre carreras.',
         'Cancha Polideportiva', NULL, 0, '16:00:00', '17:00:00', 'PUBLICADO', 'FORMULARIO', 'FIJA', NULL, NULL, NULL UNION ALL
  SELECT '[Prueba] Charla de bienvenida', 'dev.admin@ucb.edu.bo', 'Charla corta para estudiantes nuevos.',
         'Parque Jorge Trigo Andia', NULL, 0, '12:30:00', '14:00:00', 'PUBLICADO', 'ADMIN', 'FIJA', NULL, NULL, NULL UNION ALL
  SELECT '[Prueba] Taller de estudio en grupo', 'dev.diego.mamani@ucb.edu.bo', 'Taller de técnicas de estudio.',
         'Sala de Estudio Grupal', NULL, 2, '10:00:00', '12:00:00', 'PUBLICADO', 'FORMULARIO', 'LIBRE', 15, 45, NULL UNION ALL
  SELECT '[Prueba] Concierto de la semana', 'dev.lucia.rojas@ucb.edu.bo', 'Concierto de bandas universitarias.',
         NULL, 'Auditorio municipal (fuera del campus)', 5, '18:00:00', '20:00:00', 'PUBLICADO', 'FORMULARIO', 'FIJA', NULL, NULL, NULL UNION ALL
  SELECT '[Prueba] Torneo en revisión', 'dev.sofia.flores@ucb.edu.bo', 'Torneo de ajedrez que espera la revisión del admin.',
         NULL, 'Canchas del barrio (fuera del campus)', 3, '15:00:00', '16:00:00', 'PENDIENTE', 'FORMULARIO', 'FIJA', NULL, NULL, 'El lugar no está en el catálogo.'
) AS d
JOIN usuario u ON u.correo = d.correo
LEFT JOIN lugar l ON l.nombre = d.lugar
WHERE (d.lugar IS NULL OR l.id IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM evento x WHERE x.titulo = d.titulo AND x.creador_id = u.id);

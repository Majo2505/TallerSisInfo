SET @autor := (SELECT id FROM usuario WHERE correo = 'dev.estudiante@ucb.edu.bo');

INSERT INTO zona (nombre, latitud, longitud, radio_max_m, created_by, updated_by)
VALUES ('Campus UCB La Paz', -16.523600, -68.111900, 800, @autor, @autor) AS nueva
ON DUPLICATE KEY UPDATE
  latitud = nueva.latitud, longitud = nueva.longitud, radio_max_m = nueva.radio_max_m,
  updated_at = UTC_TIMESTAMP(6);

INSERT IGNORE INTO categoria (nombre, created_by, updated_by) VALUES
  ('Comida',   @autor, @autor),
  ('Estudio',  @autor, @autor),
  ('Descanso', @autor, @autor),
  ('Deporte',  @autor, @autor);

INSERT INTO lugar (nombre, descripcion, categoria_id, latitud, longitud, duracion_estimada_min, estado, created_by, updated_by)
SELECT d.nombre, d.descripcion, c.id, d.latitud, d.longitud, d.duracion, d.estado, @autor, @autor
FROM (
  SELECT 'Cafetería Central'                  AS nombre, 'Almuerzos, café y masitas en el bloque principal.' AS descripcion, 'Comida'   AS categoria, -16.524000 AS latitud, -68.111500 AS longitud, 20 AS duracion, 'ACTIVO'      AS estado UNION ALL
  SELECT 'Biblioteca Central',                'Salas silenciosas y computadoras para estudiar.',                  'Estudio',  -16.523200, -68.112200, 45, 'ACTIVO'      UNION ALL
  SELECT 'Jardín Interior',                   'Bancas a la sombra para descansar entre clases.',                  'Descanso', -16.523800, -68.112500, 15, 'ACTIVO'      UNION ALL
  SELECT 'Sala de Estudio Grupal',            'Mesas grandes y pizarras para trabajar en equipo.',                'Estudio',  -16.522900, -68.111300, 40, 'ACTIVO'      UNION ALL
  SELECT 'Cancha Polideportiva',              'Cancha de fútbol de salón y básquet.',                             'Deporte',  -16.525000, -68.113000, 40, 'ACTIVO'      UNION ALL
  SELECT 'Mirador del Sur (fuera del radio)', 'Mirador lejos del campus.',                                        'Descanso', -16.540000, -68.090000, 30, 'ACTIVO'      UNION ALL
  SELECT 'Kiosco Nuevo (en revisión)',        'Kiosco que el administrador todavía no aprobó.',                   'Comida',   -16.523700, -68.111800, 10, 'EN_REVISION'
) AS d
JOIN categoria c ON c.nombre = d.categoria
WHERE NOT EXISTS (SELECT 1 FROM lugar l WHERE l.nombre = d.nombre);

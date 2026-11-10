SET NAMES utf8mb4;
SET @sede := (SELECT id FROM zona WHERE nombre = 'Campus UCB Cochabamba');
SET @clave := (SELECT password_hash FROM usuario WHERE correo = 'dev.estudiante@ucb.edu.bo');

INSERT INTO usuario
  (correo, password_hash, nombre, carrera, semestre, rol, correo_verificado,
   consentimiento_ubicacion, fecha_consentimiento, zona_actual_id, visible_matching, fecha_visible_matching, activo)
VALUES
  ('dev.ana.quiroga@ucb.edu.bo',  @clave, 'Ana Quiroga',   'Ingeniería de Sistemas',  5, 'ESTUDIANTE', 1, 1, UTC_TIMESTAMP(6), @sede, 1, UTC_TIMESTAMP(6), 1),
  ('dev.diego.mamani@ucb.edu.bo', @clave, 'Diego Mamani',  'Ingeniería Industrial',   3, 'ESTUDIANTE', 1, 1, UTC_TIMESTAMP(6), @sede, 1, UTC_TIMESTAMP(6), 1),
  ('dev.lucia.rojas@ucb.edu.bo',  @clave, 'Lucía Rojas',   'Psicología',              7, 'ESTUDIANTE', 1, 1, UTC_TIMESTAMP(6), @sede, 1, UTC_TIMESTAMP(6), 1),
  ('dev.mateo.vargas@ucb.edu.bo', @clave, 'Mateo Vargas',  'Arquitectura',            2, 'ESTUDIANTE', 1, 1, UTC_TIMESTAMP(6), @sede, 1, UTC_TIMESTAMP(6), 1),
  ('dev.sofia.flores@ucb.edu.bo', @clave, 'Sofía Flores',  'Comunicación Social',     4, 'ESTUDIANTE', 1, 1, UTC_TIMESTAMP(6), @sede, 0, NULL,             1) AS nuevo
ON DUPLICATE KEY UPDATE
  password_hash            = nuevo.password_hash,
  nombre                   = nuevo.nombre,
  carrera                  = nuevo.carrera,
  semestre                 = nuevo.semestre,
  rol                      = nuevo.rol,
  correo_verificado        = nuevo.correo_verificado,
  consentimiento_ubicacion = nuevo.consentimiento_ubicacion,
  fecha_consentimiento     = nuevo.fecha_consentimiento,
  zona_actual_id           = nuevo.zona_actual_id,
  visible_matching         = nuevo.visible_matching,
  fecha_visible_matching   = nuevo.fecha_visible_matching,
  activo                   = nuevo.activo,
  updated_at               = UTC_TIMESTAMP(6);

SET @ana   := (SELECT id FROM usuario WHERE correo = 'dev.ana.quiroga@ucb.edu.bo');
SET @diego := (SELECT id FROM usuario WHERE correo = 'dev.diego.mamani@ucb.edu.bo');
SET @lucia := (SELECT id FROM usuario WHERE correo = 'dev.lucia.rojas@ucb.edu.bo');
SET @mateo := (SELECT id FROM usuario WHERE correo = 'dev.mateo.vargas@ucb.edu.bo');
SET @sofia := (SELECT id FROM usuario WHERE correo = 'dev.sofia.flores@ucb.edu.bo');

DELETE FROM puente  WHERE usuario_id IN (@ana, @diego, @lucia, @mateo, @sofia);
DELETE FROM horario WHERE usuario_id IN (@ana, @diego, @lucia, @mateo, @sofia);

INSERT INTO horario (usuario_id, materia, dia_semana, hora_inicio, hora_fin, aula, origen) VALUES
  (@ana,   'Bases de Datos II',          1, '08:00', '09:30', 'A-201', 'MANUAL'),
  (@ana,   'Ingeniería de Software',     1, '11:00', '12:30', 'A-305', 'MANUAL'),
  (@ana,   'Redes I',                    3, '08:00', '10:00', 'B-102', 'MANUAL'),
  (@ana,   'Taller de Sistemas',         3, '12:00', '13:30', 'A-305', 'MANUAL'),
  (@ana,   'Estadística',                5, '14:00', '15:30', 'C-110', 'MANUAL'),
  (@ana,   'Ética Profesional',          5, '17:00', '18:30', 'C-110', 'MANUAL'),

  (@diego, 'Investigación Operativa',    1, '08:00', '10:00', 'B-204', 'MANUAL'),
  (@diego, 'Procesos Industriales',      1, '11:30', '13:00', 'B-204', 'MANUAL'),
  (@diego, 'Física II',                  2, '09:00', '10:30', 'C-001', 'MANUAL'),
  (@diego, 'Cálculo III',                2, '12:00', '13:30', 'C-001', 'MANUAL'),

  (@lucia, 'Psicología del Desarrollo',  1, '07:30', '09:00', 'D-12',  'MANUAL'),
  (@lucia, 'Neuropsicología',            1, '10:30', '12:00', 'D-12',  'MANUAL'),
  (@lucia, 'Psicometría',                3, '09:00', '10:30', 'D-14',  'MANUAL'),
  (@lucia, 'Psicología Social',          3, '12:30', '14:00', 'D-14',  'MANUAL'),

  (@mateo, 'Taller de Diseño I',         2, '08:00', '09:30', 'E-01',  'MANUAL'),
  (@mateo, 'Geometría Descriptiva',      2, '11:30', '13:00', 'E-03',  'MANUAL'),
  (@mateo, 'Historia del Arte',          4, '10:00', '11:30', 'E-03',  'MANUAL'),
  (@mateo, 'Expresión Gráfica',          4, '14:00', '15:30', 'E-01',  'MANUAL'),

  (@sofia, 'Teorías de la Comunicación', 1, '08:00', '09:30', 'F-20',  'MANUAL'),
  (@sofia, 'Producción Audiovisual',     1, '11:00', '12:30', 'F-22',  'MANUAL');

INSERT INTO puente (usuario_id, dia_semana, hora_inicio, hora_fin) VALUES
  (@ana,   1, '09:30', '11:00'),
  (@ana,   3, '10:00', '12:00'),
  (@ana,   5, '15:30', '17:00'),
  (@diego, 1, '10:00', '11:30'),
  (@diego, 2, '10:30', '12:00'),
  (@lucia, 1, '09:00', '10:30'),
  (@lucia, 3, '10:30', '12:30'),
  (@mateo, 2, '09:30', '11:30'),
  (@mateo, 4, '11:30', '14:00'),
  (@sofia, 1, '09:30', '11:00');

INSERT INTO puente (usuario_id, dia_semana, hora_inicio, hora_fin, tipo) VALUES
  (@ana,   1, '12:30', '21:00', 'DESPUES_DE_CLASES'),
  (@ana,   3, '13:30', '21:00', 'DESPUES_DE_CLASES'),
  (@ana,   5, '18:30', '21:00', 'DESPUES_DE_CLASES'),
  (@diego, 1, '13:00', '21:00', 'DESPUES_DE_CLASES'),
  (@diego, 2, '13:30', '21:00', 'DESPUES_DE_CLASES'),
  (@lucia, 1, '12:00', '21:00', 'DESPUES_DE_CLASES'),
  (@lucia, 3, '14:00', '21:00', 'DESPUES_DE_CLASES'),
  (@mateo, 2, '13:00', '21:00', 'DESPUES_DE_CLASES'),
  (@mateo, 4, '15:30', '21:00', 'DESPUES_DE_CLASES'),
  (@sofia, 1, '12:30', '21:00', 'DESPUES_DE_CLASES');

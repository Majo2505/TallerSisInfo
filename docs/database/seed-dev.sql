SET NAMES utf8mb4;
INSERT INTO usuario
  (correo, password_hash, nombre, rol, correo_verificado, consentimiento_ubicacion, fecha_consentimiento, activo)
VALUES
  ('dev.estudiante@ucb.edu.bo',
   '$2a$11$CSVfZ6.lqKlbrUGz5fm2JOAJ8ul5ey3aZMeW4NqMX7XopYQX1Mm/q',
   'Usuario de Prueba (Desarrollo)',
   'ESTUDIANTE',
   1,
   0,
   NULL,
   1) AS nuevo
ON DUPLICATE KEY UPDATE
  password_hash            = nuevo.password_hash,
  nombre                   = nuevo.nombre,
  rol                      = nuevo.rol,
  correo_verificado        = nuevo.correo_verificado,
  consentimiento_ubicacion = nuevo.consentimiento_ubicacion,
  fecha_consentimiento     = nuevo.fecha_consentimiento,
  activo                   = nuevo.activo,
  updated_at               = UTC_TIMESTAMP(6);

INSERT INTO usuario
  (correo, password_hash, nombre, rol, correo_verificado, consentimiento_ubicacion, fecha_consentimiento, activo)
VALUES
  ('dev.admin@ucb.edu.bo',
   '$2a$11$7TjmbUyE.zLFEGXdHQc39O7TfIN/CkhYcMd1ZDdWHihwQDD6LrHdS',
   'Admin',
   'ADMIN',
   1,
   0,
   NULL,
   1) AS nuevo
ON DUPLICATE KEY UPDATE
  password_hash            = nuevo.password_hash,
  nombre                   = nuevo.nombre,
  rol                      = nuevo.rol,
  correo_verificado        = nuevo.correo_verificado,
  consentimiento_ubicacion = nuevo.consentimiento_ubicacion,
  fecha_consentimiento     = nuevo.fecha_consentimiento,
  activo                   = nuevo.activo,
  updated_at               = UTC_TIMESTAMP(6);

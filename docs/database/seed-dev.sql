-- BREAKU · Seed de DESARROLLO: un usuario de prueba.
-- SOLO para desarrollo local. NUNCA ejecutar en producción.
--
-- Se ejecuta con la base de datos ya seleccionada (sin USE: el nombre de la BD
-- varía según el DB_NAME de cada integrante). Es idempotente: se puede correr
-- varias veces; si el usuario ya existe (uq_usuario_correo), se restablece.
--
-- Contraseña de desarrollo: ver README.md (sección "Usuario de prueba").
-- password_hash es un hash BCrypt de esa contraseña.

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

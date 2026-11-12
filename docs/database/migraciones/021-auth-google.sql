-- Iniciar sesión con Google. Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'google_sub'", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · usuario.google_sub: el identificador de la cuenta de Google enlazada (el claim «sub» del token de Google, que no
--     cambia aunque cambie el correo). NULL si nunca entró con Google. Único: una cuenta de Google enlaza una sola cuenta.
--   · ck_usuario_credencial: toda cuenta tiene al menos una forma de entrar (contraseña o Google). Quien se registra con
--     Google no tiene contraseña hasta que la crea desde el perfil.
--
-- Antes de correrla, esta consulta debe dar 0 filas (cuentas sin contraseña, que el CHECK nuevo no dejaría):
--   SELECT id, correo FROM usuario WHERE password_hash IS NULL;

ALTER TABLE usuario
  ADD COLUMN google_sub VARCHAR(64) NULL AFTER password_hash,
  ADD CONSTRAINT uq_usuario_google UNIQUE (google_sub),
  ADD CONSTRAINT ck_usuario_credencial CHECK (password_hash IS NOT NULL OR google_sub IS NOT NULL);

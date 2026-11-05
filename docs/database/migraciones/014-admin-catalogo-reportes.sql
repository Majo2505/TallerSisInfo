-- Administración del catálogo y reportes de los estudiantes. Lleva una base creada con la versión anterior de schema.sql
-- a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate key name 'uq_lugar_nombre'", la migración ya estaba aplicada.
-- Requiere MySQL 8.0.19 o superior (DROP CHECK).
--
-- Qué cambia:
--   · lugar.uq_lugar_nombre: el admin ya puede crear y editar lugares; dos con el mismo nombre confundirían al estudiante
--     y al asistente.
--   · lugar.ck_lugar_horario: el horario se carga completo (apertura y cierre) o no se carga, y cierra después de abrir.
--   · reporte: el estudiante ya puede enviar reportes desde su perfil, también los que no son de un lugar:
--       ck_rep_motivo       ERROR_APP («algo no funciona»), SUGERENCIA, LUGAR_INCORRECTO o COMUNIDAD_INAPROPIADA;
--       ck_rep_objeto       antes exigía SIEMPRE un lugar o una comunidad, y un «algo no funciona» no tiene ninguno.
--                           Ahora el lugar va solo con LUGAR_INCORRECTO y la comunidad solo con COMUNIDAD_INAPROPIADA;
--       ck_rep_descripcion  de 10 a 500 caracteres, como el formulario;
--       ix_rep_estado       para la lista del admin (primero los pendientes).
--
-- Antes de correrla, estas consultas deben dar 0 filas:
--   SELECT nombre, COUNT(*) FROM lugar GROUP BY nombre HAVING COUNT(*) > 1;
--   SELECT id FROM lugar WHERE (hora_apertura IS NULL) <> (hora_cierre IS NULL) OR hora_cierre <= hora_apertura;
--   SELECT id, motivo FROM reporte
--   WHERE motivo NOT IN ('ERROR_APP','SUGERENCIA','LUGAR_INCORRECTO','COMUNIDAD_INAPROPIADA')
--      OR descripcion IS NULL OR CHAR_LENGTH(TRIM(descripcion)) NOT BETWEEN 10 AND 500;

ALTER TABLE lugar
  ADD CONSTRAINT uq_lugar_nombre UNIQUE (nombre),
  ADD CONSTRAINT ck_lugar_horario CHECK ((hora_apertura IS NULL AND hora_cierre IS NULL)
                                      OR (hora_apertura IS NOT NULL AND hora_cierre IS NOT NULL AND hora_cierre > hora_apertura));

ALTER TABLE reporte
  DROP CHECK ck_rep_objeto,
  ADD KEY ix_rep_estado (estado, created_at),
  ADD CONSTRAINT ck_rep_motivo CHECK (motivo IN ('ERROR_APP','SUGERENCIA','LUGAR_INCORRECTO','COMUNIDAD_INAPROPIADA')),
  ADD CONSTRAINT ck_rep_objeto CHECK ((motivo = 'LUGAR_INCORRECTO'      AND lugar_id IS NOT NULL AND comunidad_id IS NULL)
                                   OR (motivo = 'COMUNIDAD_INAPROPIADA' AND comunidad_id IS NOT NULL AND lugar_id IS NULL)
                                   OR (motivo IN ('ERROR_APP','SUGERENCIA') AND lugar_id IS NULL AND comunidad_id IS NULL)),
  ADD CONSTRAINT ck_rep_descripcion CHECK (CHAR_LENGTH(TRIM(descripcion)) BETWEEN 10 AND 500);

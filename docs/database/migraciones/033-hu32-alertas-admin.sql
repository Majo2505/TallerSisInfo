SET NAMES utf8mb4;

-- HU32: alertas al administrador. Lleva una base con la 032 a la versión actual de schema.sql. Se ejecuta UNA sola vez,
-- con la base de datos ya seleccionada (sin USE). No hace falta en una base nueva creada con el schema.sql actual.
-- Se deshace con reversa/033-hu32-alertas-admin-reversa.sql.
-- Si falla con "Duplicate column name 'usuario_objetivo_id'", la migración ya estaba aplicada.
--
-- Qué cambia (solo notificacion; ningún dato existente):
--   · tipo pasa de VARCHAR(25) a VARCHAR(30): ALERTA_REPORTES_ACUMULADOS tiene 26 letras y no cabía.
--   · usuario_objetivo_id (nullable, FK a usuario): en las alertas, el estudiante del que se avisa. La campanita lo usa
--     para llevar a Admin → Usuarios. Todos los avisos que ya existen quedan con NULL.
--   · Tipos nuevos ALERTA_USUARIO_SOSPECHOSO (3 intentos bloqueados por seguridad en 24 h) y ALERTA_REPORTES_ACUMULADOS
--     (3 o más reportes pendientes de personas distintas). REPORTE_NUEVO y EVENTO_POR_REVISAR (030) no cambian.
--   · ck_notif_ref: las alertas exigen usuario_objetivo_id y nada más; los demás tipos exigen lo mismo que antes y además
--     usuario_objetivo_id NULL.
--   · uq_notif_objetivo (usuario_id, usuario_objetivo_id, tipo): una alerta por admin, estudiante y tipo. Si vuelve a
--     pasar, el backend la actualiza (ON DUPLICATE KEY UPDATE: mensaje nuevo y sin leer). Las filas con NULL no chocan
--     entre sí (en MySQL, NULL no cuenta como repetido en un UNIQUE).
--   · La FK no lleva ON DELETE: la purga de cuentas (HU28/HU29) borra antes las alertas sobre ese estudiante.
-- ATENCIÓN: el backend de HU32 lee esta columna en la campanita. Hay que aplicar la migración en Aiven ANTES de usar
-- ese backend; si no, la campanita da error («Unknown column 'usuario_objetivo_id'»).

ALTER TABLE notificacion
  MODIFY COLUMN tipo VARCHAR(30) NOT NULL,
  ADD COLUMN usuario_objetivo_id INT NULL AFTER conexion_id,
  ADD CONSTRAINT uq_notif_objetivo UNIQUE (usuario_id, usuario_objetivo_id, tipo),
  ADD CONSTRAINT fk_notif_usuario_objetivo FOREIGN KEY (usuario_objetivo_id) REFERENCES usuario (id);

ALTER TABLE notificacion
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref,
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA',
                                           'EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA',
                                           'CONEXION_SOLICITADA','CONEXION_ACEPTADA','REPORTE_NUEVO','EVENTO_POR_REVISAR',
                                           'ASISTENCIA_COMUNIDAD','INVITACION_EVENTO',
                                           'ALERTA_USUARIO_SOSPECHOSO','ALERTA_REPORTES_ACUMULADOS')),
  ADD CONSTRAINT ck_notif_ref CHECK (
       (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES')
          AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo IN ('INVITACION_COMUNIDAD','COMUNIDAD_CERRADA','ASISTENCIA_COMUNIDAD') AND comunidad_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo IN ('EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','EVENTO_POR_REVISAR','INVITACION_EVENTO') AND evento_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo IN ('REPORTE_RESUELTO','REPORTE_NUEVO') AND reporte_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo = 'SANCION_ADVERTENCIA' AND sancion_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND conexion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo IN ('CONEXION_SOLICITADA','CONEXION_ACEPTADA') AND conexion_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo IN ('ALERTA_USUARIO_SOSPECHOSO','ALERTA_REPORTES_ACUMULADOS') AND usuario_objetivo_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL));

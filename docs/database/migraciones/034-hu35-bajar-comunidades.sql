SET NAMES utf8mb4;

-- HU35: el admin quita (BAJADA) y restaura comunidades. Lleva una base con la 033 a la versión actual de schema.sql.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE). No hace falta en una base nueva creada con el
-- schema.sql actual. Se deshace con reversa/034-hu35-bajar-comunidades-reversa.sql.
-- Si falla con "Duplicate check constraint name 'ck_com_baja'" o "Duplicate column name 'motivo_baja'", ya estaba aplicada.
--
-- Qué cambia:
--   · comunidad_efimera: estado nuevo BAJADA (distinto de ARCHIVADA, que es el cierre automático al vencer: así el cierre
--     no la toca y se puede restaurar) y columnas motivo_baja (hasta 500) y fecha_baja. ck_com_baja: una BAJADA tiene
--     motivo y fecha; las demás, ninguno. No se borra ningún dato: participaciones, avisos y reportes se conservan.
--   · notificacion: avisos COMUNIDAD_BAJADA y COMUNIDAD_RESTAURADA, con comunidad_id como los demás avisos de comunidad.
--     uq_notif_comunidad (usuario, comunidad, tipo) se mantiene: si una comunidad se quita o se restaura otra vez, el
--     backend actualiza el aviso existente (ON DUPLICATE KEY UPDATE), como con ASISTENCIA_COMUNIDAD.
--   · ix_comunidad_estado_fin sigue sirviendo al cierre automático (solo mira ACTIVA).
--   · No se guarda en historial_cambio quién la quitó (HU35 lo deja opcional): agregar 'COMUNIDAD' ahí obligaría a tocar
--     la purga de cuentas (HU28/HU29).
-- Aplicarla no cambia nada de lo que ya funciona: el backend actual no lee estas columnas y ninguna comunidad está BAJADA.
--
-- Antes de correrla, revisar (debería dar 0 filas): SELECT id, estado FROM comunidad_efimera WHERE estado NOT IN ('ACTIVA','ARCHIVADA');

ALTER TABLE comunidad_efimera
  ADD COLUMN motivo_baja VARCHAR(500) NULL AFTER estado,
  ADD COLUMN fecha_baja  DATETIME(6)  NULL AFTER motivo_baja,
  DROP CHECK ck_com_estado,
  ADD CONSTRAINT ck_com_estado CHECK (estado IN ('ACTIVA','ARCHIVADA','BAJADA')),
  ADD CONSTRAINT ck_com_baja   CHECK ((estado = 'BAJADA' AND motivo_baja IS NOT NULL AND CHAR_LENGTH(TRIM(motivo_baja)) > 0
                                       AND fecha_baja IS NOT NULL)
                                   OR (estado <> 'BAJADA' AND motivo_baja IS NULL AND fecha_baja IS NULL));

ALTER TABLE notificacion
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref,
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA',
                                           'EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA',
                                           'CONEXION_SOLICITADA','CONEXION_ACEPTADA','REPORTE_NUEVO','EVENTO_POR_REVISAR',
                                           'ASISTENCIA_COMUNIDAD','INVITACION_EVENTO',
                                           'ALERTA_USUARIO_SOSPECHOSO','ALERTA_REPORTES_ACUMULADOS',
                                           'COMUNIDAD_BAJADA','COMUNIDAD_RESTAURADA')),
  ADD CONSTRAINT ck_notif_ref CHECK (
       (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES')
          AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL AND conexion_id IS NULL AND usuario_objetivo_id IS NULL)
    OR (tipo IN ('INVITACION_COMUNIDAD','COMUNIDAD_CERRADA','ASISTENCIA_COMUNIDAD','COMUNIDAD_BAJADA','COMUNIDAD_RESTAURADA')
          AND comunidad_id IS NOT NULL
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

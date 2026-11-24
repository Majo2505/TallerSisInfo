SET NAMES utf8mb4;

-- Reversa de la 034 (HU35): las comunidades BAJADA vuelven a ACTIVA (no se pierde ninguna), se borran los avisos
-- COMUNIDAD_BAJADA y COMUNIDAD_RESTAURADA, y comunidad_efimera y notificacion quedan como en la 033.

DELETE FROM notificacion WHERE tipo IN ('COMUNIDAD_BAJADA','COMUNIDAD_RESTAURADA');

ALTER TABLE comunidad_efimera DROP CHECK ck_com_baja;
UPDATE comunidad_efimera SET estado = 'ACTIVA', motivo_baja = NULL, fecha_baja = NULL WHERE estado = 'BAJADA';

ALTER TABLE comunidad_efimera
  DROP CHECK ck_com_estado,
  ADD CONSTRAINT ck_com_estado CHECK (estado IN ('ACTIVA','ARCHIVADA')),
  DROP COLUMN motivo_baja,
  DROP COLUMN fecha_baja;

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

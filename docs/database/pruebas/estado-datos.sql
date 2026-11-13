-- Resumen de las filas que ya existían antes de la Fase 0 (solo SELECT). La usa verificar-fase0.sh para comprobar que las
-- migraciones no pierden ni cambian datos viejos.

SELECT 'notificacion', tipo, COUNT(*) FROM notificacion WHERE tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA') GROUP BY tipo ORDER BY tipo;
SELECT 'reporte', motivo, estado, COUNT(*), SUM(lugar_id IS NOT NULL), SUM(comunidad_id IS NOT NULL) FROM reporte WHERE motivo IN ('ERROR_APP','SUGERENCIA','LUGAR_INCORRECTO','COMUNIDAD_INAPROPIADA') GROUP BY motivo, estado ORDER BY motivo, estado;
SELECT 'log', tipo, estado, COUNT(*), SUM(datos_extraidos IS NOT NULL) FROM log_auditoria_rag WHERE tipo IN ('RECOMENDACION','EXTRACCION_COMUNIDAD') GROUP BY tipo, estado ORDER BY tipo, estado;
SELECT 'historial', entidad, accion, COUNT(*) FROM historial_cambio WHERE entidad IN ('USUARIO','LUGAR','ZONA') GROUP BY entidad, accion ORDER BY entidad, accion;
SELECT 'comunidad', COUNT(*), SUM(estado = 'ACTIVA') FROM comunidad_efimera;
SELECT 'lugar', COUNT(*), SUM(recomendable), SUM(estado = 'ACTIVO') FROM lugar;
SELECT 'usuario', COUNT(*) FROM usuario;

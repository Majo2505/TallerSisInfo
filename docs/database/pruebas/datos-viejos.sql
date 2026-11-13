SET NAMES utf8mb4;

-- Filas de ejemplo con el ESQUEMA ANTERIOR a la Fase 0 (los 4 tipos de aviso, los 4 motivos de reporte, los 2 tipos de log y
-- el historial de lugares). Se cargan antes de migrar para comprobar que sobreviven. Requiere los seeds de desarrollo.
-- Solo datos de prueba. La usa verificar-fase0.sh.

SET @u := (SELECT id FROM usuario WHERE correo = 'dev.estudiante@ucb.edu.bo');
SET @a := (SELECT id FROM usuario WHERE correo = 'dev.admin@ucb.edu.bo');
SET @l := (SELECT id FROM lugar WHERE nombre = 'Biblioteca Central');

INSERT INTO comunidad_efimera (creador_id, texto_original, actividad, inicio, fin, max_participantes, lugar_id)
VALUES (@u, 'Jugar cartas en el patio', 'Jugar cartas', '2030-01-07 15:00:00', '2030-01-07 16:00:00', 5, @l);
SET @c := LAST_INSERT_ID();

INSERT INTO notificacion (usuario_id, tipo, comunidad_id, puente_fecha, puente_hora_inicio, puente_hora_fin, mensaje) VALUES
  (@u, 'PUENTE_DISPONIBLE',    NULL, '2030-01-07', '10:00:00', '11:00:00', 'Tienes un puente libre.'),
  (@u, 'DESPUES_DE_CLASES',    NULL, '2030-01-07', '18:00:00', '20:00:00', 'Saliste temprano.'),
  (@u, 'INVITACION_COMUNIDAD', @c,   NULL, NULL, NULL, 'Te invitaron a una comunidad.'),
  (@u, 'COMUNIDAD_CERRADA',    @c,   NULL, NULL, NULL, 'La comunidad terminó.');

INSERT INTO reporte (usuario_id, lugar_id, comunidad_id, motivo, descripcion) VALUES
  (@u, NULL, NULL, 'ERROR_APP',             'La pantalla se queda en blanco al abrir Inicio.'),
  (@u, NULL, NULL, 'SUGERENCIA',            'Sería útil ver los eventos del campus.'),
  (@u, @l,   NULL, 'LUGAR_INCORRECTO',      'El horario de la biblioteca no es correcto.'),
  (@u, NULL, @c,   'COMUNIDAD_INAPROPIADA', 'El nombre de la comunidad es ofensivo.');
INSERT INTO reporte (usuario_id, lugar_id, comunidad_id, motivo, descripcion, estado, revisor_id, revisado_en)
VALUES (@u, NULL, NULL, 'ERROR_APP', 'Otro error que ya se resolvió.', 'RESUELTO', @a, UTC_TIMESTAMP(6));

INSERT INTO log_auditoria_rag (usuario_id, tipo, estado) VALUES (@u, 'RECOMENDACION', 'OK');
INSERT INTO log_auditoria_rag (usuario_id, tipo, estado, datos_extraidos) VALUES (@u, 'EXTRACCION_COMUNIDAD', 'OK', '{"actividad":"Jugar cartas"}');

INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id)
VALUES ('LUGAR', @l, 'UPDATE', '{"estado":"ACTIVO"}', '{"estado":"INACTIVO"}', @a);

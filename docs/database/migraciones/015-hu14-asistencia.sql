-- HU14: confirmar o rechazar la asistencia a una comunidad (RSVP). Lleva una base creada con la versión anterior de
-- schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate key name 'ix_part_usuario_estado'", la migración ya estaba aplicada.
--
-- Qué cambia:
--   · participacion.ix_part_usuario_estado: al confirmar, el servidor revisa si el estudiante ya va a otra comunidad
--     a esa hora (sus participaciones CONFIRMADO). Sin el índice, esa consulta recorre toda la tabla.
--   · Quien crea la comunidad siempre va (HU13 lo deja CONFIRMADO). Si alguna fila del creador quedó con otro estado,
--     se corrige; así el conteo de confirmados no se equivoca.
--
-- El cupo (no más confirmados que max_participantes) no se puede escribir como CHECK porque compara filas distintas:
-- lo controla el servidor, bloqueando la comunidad (SELECT … FOR UPDATE) mientras confirma.
--
-- Antes de correrla, revisar (deberían dar 0 filas):
--   SELECT p.comunidad_id, p.usuario_id, p.estado FROM participacion p
--   JOIN comunidad_efimera c ON c.id = p.comunidad_id AND c.creador_id = p.usuario_id
--   WHERE p.estado <> 'CONFIRMADO';
--   SELECT c.id, c.max_participantes, COUNT(*) AS confirmados FROM comunidad_efimera c
--   JOIN participacion p ON p.comunidad_id = c.id AND p.estado = 'CONFIRMADO'
--   GROUP BY c.id, c.max_participantes HAVING COUNT(*) > c.max_participantes;

UPDATE participacion p
JOIN comunidad_efimera c ON c.id = p.comunidad_id AND c.creador_id = p.usuario_id
SET p.estado = 'CONFIRMADO', p.respondido_en = COALESCE(p.respondido_en, p.invitado_en)
WHERE p.estado <> 'CONFIRMADO';

ALTER TABLE participacion
  ADD KEY ix_part_usuario_estado (usuario_id, estado);

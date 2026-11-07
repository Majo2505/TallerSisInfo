-- Auditoría: los turnos de una misma conversación con el asistente quedan unidos. Lleva una base creada con la versión
-- anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'consulta_origen_id'", la migración ya estaba aplicada.
--
-- Por qué: al crear una actividad, el asistente pregunta lo que falta («¿A qué hora?») y cada respuesta («5») se
-- guardaba como una consulta suelta. En la Auditoría RAG aparecía un «5» que no se entendía.
--
-- Qué cambia:
--   · log_auditoria_rag.consulta_origen_id: el primer turno de la conversación. NULL en el primero (y en todas las
--     recomendaciones, que son de un solo turno: eso lo controla el servidor, porque MySQL no deja poner un CHECK en una
--     columna con ON DELETE SET NULL). Si se borra el primero, los demás quedan sueltos.
--   · Une las conversaciones que ya estaban guardadas: un turno de extracción es continuación del anterior del mismo
--     estudiante si llegó menos de 30 minutos después y el anterior quedó INCOMPLETO (el asistente estaba preguntando
--     algo), o si es una corrección hecha a mano (modelo «manual»).

ALTER TABLE log_auditoria_rag
  ADD COLUMN consulta_origen_id BIGINT NULL AFTER usuario_id,
  ADD KEY ix_log_origen (consulta_origen_id),
  ADD CONSTRAINT fk_log_origen FOREIGN KEY (consulta_origen_id) REFERENCES log_auditoria_rag (id) ON DELETE SET NULL;

UPDATE log_auditoria_rag g
JOIN (
  SELECT id, MIN(id) OVER (PARTITION BY usuario_id, conversacion) AS origen
  FROM (
    SELECT id, usuario_id, SUM(empieza) OVER (PARTITION BY usuario_id ORDER BY created_at, id) AS conversacion
    FROM (
      SELECT id, usuario_id, created_at,
             CASE WHEN LAG(id) OVER w IS NOT NULL
                       AND TIMESTAMPDIFF(MINUTE, LAG(created_at) OVER w, created_at) < 30
                       AND (LAG(estado) OVER w = 'INCOMPLETO' OR modelo = 'manual')
                  THEN 0 ELSE 1 END AS empieza
      FROM log_auditoria_rag
      WHERE tipo = 'EXTRACCION_COMUNIDAD'
      WINDOW w AS (PARTITION BY usuario_id ORDER BY created_at, id)
    ) turnos
  ) agrupados
) conversaciones ON conversaciones.id = g.id
SET g.consulta_origen_id = conversaciones.origen
WHERE conversaciones.origen <> g.id;

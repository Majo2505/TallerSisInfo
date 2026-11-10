-- HU3: tiempo libre después de clases. Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate check constraint name 'ck_puente_tipo'", la migración ya estaba aplicada y no cambió nada.
--
-- Qué cambia:
--   · puente.tipo: ENTRE_CLASES (el puente de siempre) o DESPUES_DE_CLASES (desde el fin de la última clase del día
--     hasta las 21:00, si son 45 minutos o más). El tiempo antes de la primera clase y los días sin clases no cuentan.
--   · notificacion.tipo admite DESPUES_DE_CLASES, con las mismas columnas de puente que PUENTE_DISPONIBLE.
--   · Crea el tramo DESPUES_DE_CLASES de los horarios que ya están guardados. Sin esto, cada estudiante recién lo
--     tendría al volver a editar su horario.
--
-- El servidor sin esta rama no lee la columna tipo, así que la migración se puede aplicar antes de actualizarlo. Al
-- revés no: el servidor nuevo falla con "Unknown column 'tipo'" si la base todavía no la tiene.

ALTER TABLE puente
  ADD COLUMN tipo VARCHAR(20) NOT NULL DEFAULT 'ENTRE_CLASES' AFTER hora_fin,
  ADD CONSTRAINT ck_puente_tipo CHECK (tipo IN ('ENTRE_CLASES','DESPUES_DE_CLASES'));

ALTER TABLE notificacion
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref;

ALTER TABLE notificacion
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA')),
  ADD CONSTRAINT ck_notif_ref  CHECK ((tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES') AND comunidad_id IS NULL
                                       AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL)
                                   OR (tipo NOT IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES') AND comunidad_id IS NOT NULL
                                       AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL));

INSERT INTO puente (usuario_id, dia_semana, hora_inicio, hora_fin, tipo)
SELECT h.usuario_id, h.dia_semana, MAX(h.hora_fin), '21:00:00', 'DESPUES_DE_CLASES'
FROM horario h
WHERE h.asiste = TRUE
GROUP BY h.usuario_id, h.dia_semana
HAVING TIME_TO_SEC(TIMEDIFF('21:00:00', MAX(h.hora_fin))) >= 45 * 60;

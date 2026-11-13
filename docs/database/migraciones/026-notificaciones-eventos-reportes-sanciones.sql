SET NAMES utf8mb4;

-- Avisos de eventos, de reportes resueltos y de advertencias (HU26, HU27 y HU28). Lleva una base creada con la versión
-- anterior de schema.sql a la actual. Requiere la 022 (evento), la 024 (reporte.respuesta) y la 025 (sancion).
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate column name 'evento_id'", la migración ya estaba aplicada.
--
-- Es una sola migración para los tres avisos: así ck_notif_ref se reescribe una vez y no hay migraciones que choquen.
--
-- Qué cambia (los CHECK se amplían, no se restringen):
--   · notificacion.evento_id, reporte_id y sancion_id: a qué apunta el aviso. Sin ON DELETE (están en un CHECK): el
--     borrado de las notificaciones de un usuario lo hace de forma explícita el backend.
--   · ck_notif_tipo: tipos nuevos EVENTO_APROBADO, EVENTO_RECHAZADO, EVENTO_BAJADO, REPORTE_RESUELTO y SANCION_ADVERTENCIA.
--   · ck_notif_ref: cada tipo exige solo su referencia (puente, comunidad, evento, reporte o sanción) y ninguna de las
--     demás. Las dos primeras ramas aceptan exactamente lo que aceptaba el CHECK anterior.
--   · uq_notif_evento, uq_notif_reporte y uq_notif_sancion: un mismo aviso no se duplica para el mismo usuario. No cambian
--     a quién se notifica.

ALTER TABLE notificacion
  ADD COLUMN evento_id  INT NULL AFTER comunidad_id,
  ADD COLUMN reporte_id INT NULL AFTER evento_id,
  ADD COLUMN sancion_id INT NULL AFTER reporte_id,
  DROP CHECK ck_notif_tipo,
  DROP CHECK ck_notif_ref,
  ADD CONSTRAINT ck_notif_tipo CHECK (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES','INVITACION_COMUNIDAD','COMUNIDAD_CERRADA',
                                               'EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO','REPORTE_RESUELTO','SANCION_ADVERTENCIA')),
  ADD CONSTRAINT ck_notif_ref CHECK (
       (tipo IN ('PUENTE_DISPONIBLE','DESPUES_DE_CLASES')
          AND puente_fecha IS NOT NULL AND puente_hora_inicio IS NOT NULL AND puente_hora_fin IS NOT NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)
    OR (tipo IN ('INVITACION_COMUNIDAD','COMUNIDAD_CERRADA') AND comunidad_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND evento_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)
    OR (tipo IN ('EVENTO_APROBADO','EVENTO_RECHAZADO','EVENTO_BAJADO') AND evento_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND reporte_id IS NULL AND sancion_id IS NULL)
    OR (tipo = 'REPORTE_RESUELTO' AND reporte_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND sancion_id IS NULL)
    OR (tipo = 'SANCION_ADVERTENCIA' AND sancion_id IS NOT NULL
          AND puente_fecha IS NULL AND puente_hora_inicio IS NULL AND puente_hora_fin IS NULL
          AND comunidad_id IS NULL AND evento_id IS NULL AND reporte_id IS NULL)),
  ADD CONSTRAINT uq_notif_evento  UNIQUE (usuario_id, evento_id, tipo),
  ADD CONSTRAINT uq_notif_reporte UNIQUE (usuario_id, reporte_id, tipo),
  ADD CONSTRAINT uq_notif_sancion UNIQUE (usuario_id, sancion_id, tipo),
  ADD CONSTRAINT fk_notif_evento  FOREIGN KEY (evento_id)  REFERENCES evento (id),
  ADD CONSTRAINT fk_notif_reporte FOREIGN KEY (reporte_id) REFERENCES reporte (id),
  ADD CONSTRAINT fk_notif_sancion FOREIGN KEY (sancion_id) REFERENCES sancion (id);

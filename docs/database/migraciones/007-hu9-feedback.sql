-- HU9: valoración de recomendaciones (me gusta / no me sirve). Lleva una base creada con la versión anterior de schema.sql a la actual.
-- Se ejecuta UNA sola vez, con la base de datos ya seleccionada (sin USE, como seed-dev.sql).
-- No hace falta en una base nueva creada con el schema.sql actual.
-- Si falla con "Duplicate key name", la migración ya estaba aplicada.
--
-- Las columnas recomendacion.feedback y recomendacion.fecha_feedback ya forman parte del esquema base, con sus CHECK
-- (solo LIKE o DISLIKE, y la fecha va junto con el voto). Un voto nuevo sobre la misma recomendación reemplaza al anterior:
-- hay una sola valoración por recomendación. Este índice acelera las métricas de valoraciones del panel de administración.
CREATE INDEX ix_recomendacion_feedback ON recomendacion (feedback, fecha_feedback);

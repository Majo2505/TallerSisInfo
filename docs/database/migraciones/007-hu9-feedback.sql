-- HU9: la columna ya forma parte del esquema base; este índice acelera las métricas.
CREATE INDEX ix_recomendacion_feedback ON recomendacion (feedback, fecha_feedback);

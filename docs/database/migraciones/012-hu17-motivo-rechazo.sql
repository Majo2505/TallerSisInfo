ALTER TABLE log_auditoria_rag
  ADD COLUMN motivo_rechazo VARCHAR(20) NULL AFTER estado,
  ADD KEY ix_log_motivo (motivo_rechazo, created_at),
  ADD CONSTRAINT ck_log_motivo CHECK (motivo_rechazo IN ('INYECCION','FUERA_DE_ALCANCE','BIENESTAR','FUERA_DE_REGLAMENTO','FUERA_DE_CONTEXTO')),
  ADD CONSTRAINT ck_log_motivo_estado CHECK (motivo_rechazo IS NULL OR estado = 'RECHAZADO_SEGURIDAD');

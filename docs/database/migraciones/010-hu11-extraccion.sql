ALTER TABLE log_auditoria_rag
  DROP CHECK ck_log_estado,
  ADD CONSTRAINT ck_log_estado CHECK (estado IN ('OK','SIN_RESULTADOS','RECHAZADO_SEGURIDAD','ERROR','INCOMPLETO'));

ALTER TABLE horario
  ADD CONSTRAINT ck_horario_rango CHECK (hora_inicio >= '07:15:00' AND hora_fin <= '21:00:00');

# Backlog de Issues - Fase 1

## 1) [EPIC] Autenticación institucional y sesión JWT
- **Título sugerido:** `[EPIC] Autenticación institucional y sesión JWT`
- **Labels:** `epic`, `backend`, `security`, `auth`
- **Prioridad:** Alta
- **Dependencias:** Ninguna
- **Cubre:** RF01, RNF01, RN01
- **Descripción:** Implementar el acceso con correo institucional verificado, generación y renovación de JWT, y control de acceso por roles.
- **Criterios de aceptación:**
  - [ ] Solo se permite login con correo institucional verificado.
  - [ ] El sistema emite JWT con expiración y mecanismo de renovación.
  - [ ] Las rutas protegidas validan rol y token.

## 2) Registro y edición de horario académico del estudiante
- **Título sugerido:** `Registro y edición de horario académico del estudiante`
- **Labels:** `feature`, `backend`, `frontend`, `horarios`
- **Prioridad:** Alta
- **Dependencias:** #1
- **Cubre:** RF02
- **Descripción:** Permitir al estudiante crear, editar y eliminar bloques de clase, con persistencia por usuario.
- **Criterios de aceptación:**
  - [ ] Alta/edición/eliminación de bloques de clase.
  - [ ] Validación de solapamientos en carga y edición.
  - [ ] Los datos quedan persistidos por usuario autenticado.

## 3) Detección automática de puentes horarios
- **Título sugerido:** `Detección automática de puentes horarios`
- **Labels:** `feature`, `backend`, `horarios`, `rules`
- **Prioridad:** Alta
- **Dependencias:** #2
- **Cubre:** RF03, RN02
- **Descripción:** Calcular huecos entre clases y detectar automáticamente puentes válidos según duración mínima configurable.
- **Criterios de aceptación:**
  - [ ] Cálculo automático de puentes entre bloques académicos.
  - [ ] Filtro de duración mínima (por defecto 45 min, configurable).
  - [ ] Registro del puente detectado para consumo por otros módulos.

## 4) Notificaciones de puente disponible
- **Título sugerido:** `Notificaciones de puente disponible`
- **Labels:** `feature`, `notifications`, `backend`, `frontend`
- **Prioridad:** Media
- **Dependencias:** #3
- **Cubre:** RF04
- **Descripción:** Notificar al estudiante cuando se detecta un puente elegible para recomendación o comunidad.
- **Criterios de aceptación:**
  - [ ] Se emite notificación al detectar puente válido.
  - [ ] No se envían notificaciones duplicadas para el mismo puente.
  - [ ] El estudiante puede visualizar el aviso desde la interfaz.

## 5) Geolocalización de estudiante por campus/zona
- **Título sugerido:** `Geolocalización de estudiante por campus/zona`
- **Labels:** `feature`, `backend`, `frontend`, `privacy`, `location`
- **Prioridad:** Alta
- **Dependencias:** #1
- **Cubre:** RF05, RNF07
- **Descripción:** Capturar ubicación o campus/zona de origen para contextualizar recomendaciones respetando privacidad.
- **Criterios de aceptación:**
  - [ ] Captura de campus/zona actual del estudiante.
  - [ ] Flujo de consentimiento explícito de uso de ubicación.
  - [ ] No se comparte ubicación con terceros.

## 6) Consulta de lugares por tiempo, cercanía y zona segura
- **Título sugerido:** `Consulta de lugares por tiempo, cercanía y zona segura`
- **Labels:** `feature`, `backend`, `rag`, `rules`, `geo`
- **Prioridad:** Alta
- **Dependencias:** #3, #5
- **Cubre:** RF06, RN03, RN06
- **Descripción:** Filtrar lugares del catálogo por tiempo disponible, cercanía, radio permitido y seguridad.
- **Criterios de aceptación:**
  - [ ] Filtro por radio predefinido desde campus de origen.
  - [ ] Filtro por zona/lugar seguro del catálogo curado.
  - [ ] Validación: ida + vuelta + actividad <= puente.

## 7) Implementar pipeline RAG con contexto estricto
- **Título sugerido:** `Implementar pipeline RAG con contexto estricto`
- **Labels:** `epic`, `ai`, `rag`, `backend`, `security`
- **Prioridad:** Alta
- **Dependencias:** #6
- **Cubre:** RF07, RNF06, RN04
- **Descripción:** Integrar recuperación + generación forzando al LLM a responder solo con contexto recuperado.
- **Criterios de aceptación:**
  - [ ] El prompt solo incluye resultados recuperados y metadatos necesarios.
  - [ ] El sistema bloquea respuestas fuera del contexto recuperado.
  - [ ] No se generan lugares inventados por el LLM.

## 8) UI de recomendaciones con métricas útiles
- **Título sugerido:** `UI de recomendaciones con métricas útiles`
- **Labels:** `feature`, `frontend`, `ux`, `rag`
- **Prioridad:** Media
- **Dependencias:** #7
- **Cubre:** RF08
- **Descripción:** Mostrar recomendaciones con datos accionables para decidir rápido entre clases.
- **Criterios de aceptación:**
  - [ ] Cada recomendación muestra distancia.
  - [ ] Cada recomendación muestra tiempo estimado.
  - [ ] Cada recomendación muestra categoría del lugar.

## 9) Feedback de recomendaciones (like/dislike)
- **Título sugerido:** `Feedback de recomendaciones (like/dislike)`
- **Labels:** `feature`, `backend`, `frontend`, `analytics`
- **Prioridad:** Media
- **Dependencias:** #8
- **Cubre:** RF09
- **Descripción:** Permitir feedback explícito del estudiante sobre la utilidad de cada recomendación.
- **Criterios de aceptación:**
  - [ ] Registro de like/dislike por usuario y recomendación.
  - [ ] Prevención de votos duplicados simultáneos.
  - [ ] Persistencia para análisis y mejora del motor.

## 10) Entrada en lenguaje natural para crear actividad
- **Título sugerido:** `Entrada en lenguaje natural para crear actividad`
- **Labels:** `feature`, `frontend`, `nlp`, `ux`
- **Prioridad:** Alta
- **Dependencias:** #3
- **Cubre:** RF10, RNF05
- **Descripción:** Habilitar captura de intención de actividad mediante texto libre sin formularios extensos.
- **Criterios de aceptación:**
  - [ ] Campo de entrada en lenguaje natural disponible para estudiante.
  - [ ] Validación mínima de texto (vacío, longitud, formato básico).
  - [ ] Mensajes claros ante entradas no interpretables.

## 11) Extracción estructurada por tool calling (JSON)
- **Título sugerido:** `Extracción estructurada por tool calling (JSON)`
- **Labels:** `feature`, `ai`, `nlp`, `backend`
- **Prioridad:** Alta
- **Dependencias:** #10
- **Cubre:** RF11
- **Descripción:** Parsear texto libre a JSON estructurado para crear eventos efímeros.
- **Criterios de aceptación:**
  - [ ] Se extraen actividad, hora, duración y número de participantes.
  - [ ] Se valida esquema JSON antes de persistir.
  - [ ] Manejo explícito de ambigüedad y errores de extracción.

## 12) Matching de estudiantes con puente compatible
- **Título sugerido:** `Matching de estudiantes con puente compatible`
- **Labels:** `feature`, `backend`, `matching`, `events`
- **Prioridad:** Alta
- **Dependencias:** #3, #11
- **Cubre:** RF12
- **Descripción:** Encontrar estudiantes con disponibilidad compatible para actividades efímeras.
- **Criterios de aceptación:**
  - [ ] Cruce por bloque horario compatible.
  - [ ] Exclusión de usuarios sin disponibilidad válida.
  - [ ] Lista de candidatos ordenada por compatibilidad.

## 13) Creación automática de comunidad efímera y notificación
- **Título sugerido:** `Creación automática de comunidad efímera y notificación`
- **Labels:** `feature`, `backend`, `events`, `notifications`
- **Prioridad:** Alta
- **Dependencias:** #12
- **Cubre:** RF13
- **Descripción:** Crear evento/comunidad de forma automática y avisar a usuarios coincidentes.
- **Criterios de aceptación:**
  - [ ] Creación automática de la comunidad con datos estructurados.
  - [ ] Notificación a estudiantes compatibles.
  - [ ] Estado inicial del evento visible para participantes.

## 14) Confirmación/rechazo de asistencia
- **Título sugerido:** `Confirmación/rechazo de asistencia`
- **Labels:** `feature`, `frontend`, `backend`, `events`, `realtime`
- **Prioridad:** Media
- **Dependencias:** #13
- **Cubre:** RF14
- **Descripción:** Permitir RSVP para administrar asistencia en comunidades efímeras.
- **Criterios de aceptación:**
  - [ ] Estados RSVP disponibles (confirmado/rechazado/pendiente).
  - [ ] Actualización de cupos/participantes en tiempo real.
  - [ ] Persistencia del estado de asistencia por usuario.

## 15) Cierre/archivo automático de comunidad al vencer bloque
- **Título sugerido:** `Cierre/archivo automático de comunidad al vencer bloque`
- **Labels:** `feature`, `backend`, `events`, `automation`
- **Prioridad:** Alta
- **Dependencias:** #13
- **Cubre:** RF15, RN05
- **Descripción:** Cerrar y archivar comunidades al finalizar el bloque horario asociado.
- **Criterios de aceptación:**
  - [ ] Job/programación de cierre basada en tiempo.
  - [ ] Evento cerrado deja de figurar como activo.
  - [ ] Historial consultable para auditoría/analítica.

## 16) [EPIC] Panel admin: CRUD de lugares, reportes y auditoría RAG
- **Título sugerido:** `[EPIC] Panel admin: CRUD de lugares, reportes y auditoría RAG`
- **Labels:** `epic`, `admin`, `backend`, `frontend`, `moderation`, `audit`
- **Prioridad:** Alta
- **Dependencias:** #1
- **Cubre:** RF16, RF17, RF18
- **Descripción:** Desarrollar panel administrativo para catálogo verificado, trazabilidad RAG y moderación de reportes.
- **Criterios de aceptación:**
  - [ ] CRUD completo de lugares verificados.
  - [ ] Registro de auditoría de consultas RAG.
  - [ ] Flujo para reportar y moderar contenido inapropiado.

## 17) Protección contra prompt injection y solicitudes no permitidas
- **Título sugerido:** `Protección contra prompt injection y solicitudes no permitidas`
- **Labels:** `security`, `ai`, `backend`, `guardrails`
- **Prioridad:** Alta
- **Dependencias:** #7
- **Cubre:** RN07
- **Descripción:** Implementar guardrails para rechazar intentos de vulnerar el sistema o actividades fuera de reglamento.
- **Criterios de aceptación:**
  - [ ] Detección y rechazo explícito de prompt injection.
  - [ ] Rechazo de solicitudes fuera del reglamento universitario.
  - [ ] Registro trazable de rechazos por seguridad.

## 18) Actualización en tiempo real del estado de eventos (frontend)
- **Título sugerido:** `Actualización en tiempo real del estado de eventos (frontend)`
- **Labels:** `feature`, `frontend`, `realtime`, `events`
- **Prioridad:** Alta
- **Dependencias:** #13, #14
- **Cubre:** RNF010
- **Descripción:** Refrescar estados de eventos sin recargar la aplicación.
- **Criterios de aceptación:**
  - [ ] Cambios de estado visibles en tiempo real.
  - [ ] Sin necesidad de recarga manual de la vista.
  - [ ] Comportamiento estable en móvil y web.

## 19) Objetivos de rendimiento, disponibilidad y escalabilidad
- **Título sugerido:** `Objetivos de rendimiento, disponibilidad y escalabilidad`
- **Labels:** `non-functional`, `performance`, `ops`, `scalability`
- **Prioridad:** Alta
- **Dependencias:** #6, #7
- **Cubre:** RNF02, RNF03, RNF04
- **Descripción:** Definir, medir y validar objetivos operativos del sistema en horarios y picos de uso.
- **Criterios de aceptación:**
  - [ ] Recomendación responde en menos de 3.5 segundos en escenario objetivo.
  - [ ] Cobertura operativa mínima 7am–9pm.
  - [ ] Pruebas de carga para picos de cambio de clase.

## 20) Arquitectura modular .NET + UI responsive React
- **Título sugerido:** `Arquitectura modular .NET + UI responsive React`
- **Labels:** `architecture`, `backend`, `frontend`, `non-functional`
- **Prioridad:** Alta
- **Dependencias:** #1
- **Cubre:** RNF08, RNF09
- **Descripción:** Establecer base arquitectónica modular que facilite extensión de agentes IA y UX móvil.
- **Criterios de aceptación:**
  - [ ] Módulos desacoplados y extensibles para componentes de IA.
  - [ ] Interfaz responsive usable en pantallas móviles.
  - [ ] Convenciones de integración frontend-backend documentadas.

---

## Sugerencia de orden de ejecución (MVP)
1. #1, #2, #3, #5, #6, #7
2. #8, #9
3. #10, #11, #12, #13, #14, #15, #18
4. #16, #17, #19, #20

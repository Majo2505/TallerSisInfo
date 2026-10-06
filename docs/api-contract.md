# Contrato de API — BREAKU

**Borrador v0.1 — Sprint 1**

Alcance: autenticación (HU1), horario (HU2) y puentes (HU3). Parte del issue #139 (ítem "Contrato de endpoints").

## Leyenda

Cada decisión del documento lleva una de estas marcas:

| Marca | Significado |
|---|---|
| ✅ **Definido** | Sale de un criterio de aceptación (#9, #10, #11), de `docs/database/schema.sql` o de `AGENTS.md`. |
| 🟡 **Propuesta** | Decisión tomada para poder avanzar; la confirma la persona indicada. |
| 🔴 **Pendiente** | Nadie decidió todavía; se indica quién decide. |
| 🔎 **Inferido** | Endpoint que ningún criterio nombra literalmente, pero que el flujo necesita. |

Responsables: **Amira** = HU1 (auth), **Maite** = HU2 (horarios), **Majo** = HU3 (puentes), **Ariana** = BD y base del proyecto.

## Cómo usar este contrato

**Para FE (Amira #29, Maite #32, Majo #35)**
- Consumí solo los endpoints de este documento. Si necesitás otro, pedilo a la persona responsable del área: no lo inventes.
- Mostrá al usuario el campo `detail` de los errores tal cual viene; los tres mensajes de login están definidos en #9.
- Los campos marcados 🟡 o 🔴 pueden cambiar: no los dejes fijos en el código sin avisar.

**Para BE (Amira #30, Maite #33, Majo #36)**
- Implementá exactamente estas rutas, campos y códigos. Los DTOs no exponen entidades ni campos internos.
- Si algo contradice este documento, consultá antes de cambiarlo y actualizá el contrato en el mismo PR.

**Cambios al contrato:** se hacen por PR a `main`, con revisión de la persona dueña del área.

> **Usuarios:** sin endpoint de registro en el sprint 1; el usuario de prueba para desarrollo se define aparte (ver #139).

## 1. Convenciones generales

| Tema | Regla | Estado |
|---|---|---|
| Prefijo | Todos los endpoints de negocio cuelgan de `/api` | 🟡 Propuesta (confirmada por Ariana) |
| Nombres de rutas | Español y plural: `horarios`, `puentes` | 🟡 Propuesta (confirmada por Ariana) |
| JSON | camelCase | ✅ Definido (`AGENTS.md`) |
| Idioma de los mensajes | Español | 🟡 Propuesta |
| Fechas con hora | UTC, ISO 8601, p. ej. `2026-10-06T13:07:07Z` | ✅ Definido (`AGENTS.md`, `schema.sql`) |
| Horas de clase (`horaInicio`, `horaFin`) | Hora local del campus, sin conversión a UTC (columnas `TIME` sin fecha) | 🟡 Propuesta, a confirmar por Maite |
| Formato de hora | El backend responde `"HH:mm:ss"` (24 h), p. ej. `"14:30:00"`. Acepta `"HH:mm"` y `"HH:mm:ss"`; rechaza `"2:30 PM"` | 🟡 Propuesta (comportamiento de `TimeOnly` verificado en .NET 10) |
| Autenticación | Header `Authorization: Bearer <token>` en todo `/api`, salvo el login | ✅ Definido (JWT, `AGENTS.md`) |
| Usuario de la petición | Sale del token, nunca del body ni de la URL | 🟡 Propuesta |
| Paginación | Ninguna: las listas devuelven todo | 🟡 Propuesta, a confirmar por Maite y Majo |
| DTOs | No exponen entidades. Nunca salen `passwordHash`, `correoVerificado`, `consentimientoUbicacion`, `fechaConsentimiento`, `createdAt`, `updatedAt`, `updatedBy` ni `activo` | ✅ Definido (requisito del equipo) |

### Formato de error

🟡 **Propuesta:** `ProblemDetails` de ASP.NET (`application/problem+json`). El FE muestra `detail`.

```json
{
  "status": 400,
  "title": "Solicitud inválida",
  "detail": "Por favor, complete todos los campos"
}
```

Errores de validación por campo: además se incluye `errors` (campo → lista de mensajes).

```json
{
  "status": 400,
  "title": "Solicitud inválida",
  "detail": "Hay campos inválidos",
  "errors": { "horaFin": ["..."] }
}
```

- ✅ **Definido:** solo existen tres textos de error, los de login (sección 2).
- 🔴 **Pendiente:** el texto del resto de errores (validación de horarios, solapamiento, no encontrado). Decide la persona dueña de cada área: Maite (horarios) y Majo (puentes).

### Token JWT

| Aspecto | Valor | Estado |
|---|---|---|
| Header | `Authorization: Bearer <token>` | ✅ Definido |
| Claims | `sub` = id del usuario; `rol` = rol del usuario | 🟡 Propuesta, a confirmar por Amira |
| Duración | 60 minutos, sin refresh token; la respuesta de login no incluye `expiresAt` | 🟡 Propuesta, a confirmar por Amira |
| Sin token / token inválido o vencido | `401` | 🟡 Propuesta |

### CORS y URLs de desarrollo

| Aspecto | Valor | Estado |
|---|---|---|
| URL del backend en desarrollo | `http://localhost:5092` (perfil `http` de `launchSettings.json`) | ✅ Definido (existe en el repo) |
| Origen del FE permitido por CORS | `http://localhost:3000` (puerto por defecto de Next.js) | 🟡 Propuesta, a confirmar por Ariana |

## 2. Auth (HU1 — responsable: Amira)

### `POST /api/auth/login`

Requiere JWT: **no**. Origen: #9, criterios 1 a 5. Estado del endpoint: ✅ Definido.

**Request**

| Campo | Tipo | Validación |
|---|---|---|
| `correo` | string | Obligatorio (✅ #9 criterio 4); dominio `@ucb.edu.bo` (✅ #9 criterio 2) |
| `password` | string | Obligatorio (✅ #9 criterio 4) |

```json
{
  "correo": "estudiante@ucb.edu.bo",
  "password": "********"
}
```

Orden de validación: campos vacíos → dominio → credenciales (🟡 Propuesta).

**Respuesta 200**

🟡 **Propuesta** (la redirección al panel principal es del FE, #9 criterio 5):

```json
{
  "token": "<jwt>",
  "usuario": {
    "id": 1,
    "nombre": "Nombre Apellido",
    "correo": "estudiante@ucb.edu.bo",
    "rol": "ESTUDIANTE"
  }
}
```

**Errores**

| Código | Caso | `detail` | Estado |
|---|---|---|---|
| 400 | Correo o contraseña vacíos | `Por favor, complete todos los campos` | ✅ Definido (texto y caso); código 🟡 Propuesta |
| 400 | Correo fuera de `@ucb.edu.bo` | `Solo se permite el acceso con un correo institucional (@ucb.edu.bo)` | ✅ Definido (texto y caso); código 400 confirmado por Ariana |
| 401 | Contraseña incorrecta | `Correo o contraseña incorrectos` | ✅ Definido (texto y caso); código 🟡 Propuesta |
| 401 | Correo no registrado | `Correo o contraseña incorrectos` (mismo mensaje) | 🟡 Propuesta, a confirmar por Amira |
| 401 | Usuario con `activo = 0` | `Correo o contraseña incorrectos` (mismo mensaje) | 🟡 Propuesta, a confirmar por Amira y Ariana |

### Decisiones de auth

| Duda | Valor por defecto | Estado |
|---|---|---|
| Claims, duración y refresh del token | Ver "Token JWT" arriba | 🟡 Propuesta, a confirmar por Amira |
| Algoritmo de hash de contraseña (`password_hash` es `VARCHAR(255)`) | BCrypt (paquete BCrypt.Net-Next; versión por confirmar). El paquete lo agrega Amira en #30 | ✅ Definido (decisión de Ariana, base) |
| ¿El login exige `correoVerificado = true`? | No en el sprint 1: no existe flujo de verificación y bloquearía al usuario de prueba | 🟡 Propuesta, a confirmar por Amira y Ariana |
| Creación de usuarios | Sin endpoint de registro en el sprint 1; el usuario de prueba se define aparte (ver #139) | ✅ Definido (alcance del sprint) |

## 3. Horarios (HU2 — responsable: Maite)

Todos requieren JWT y operan solo sobre los bloques del usuario del token. Origen: #10 ("Alta/edición/eliminación de bloques de clase", "Validación de solapamientos", "Persistencia por usuario").

Campos del bloque (columnas de `horario`, `schema.sql`):

| Campo | Tipo | Validación | Estado |
|---|---|---|---|
| `id` | entero | Solo en respuestas | ✅ Definido |
| `materia` | string | Obligatorio, hasta 100 caracteres | ✅ Definido (schema) |
| `diaSemana` | entero | 1 a 6 (1 = lunes … 6 = sábado) | ✅ Definido (`ck_horario_dia`) |
| `horaInicio` | hora | Obligatoria | ✅ Definido (schema) |
| `horaFin` | hora | Obligatoria, posterior a `horaInicio` | ✅ Definido (`ck_horario_horas`) |
| `aula` | string o `null` | Opcional, hasta 30 caracteres | ✅ Definido (schema) |
| `origen` | string | Solo en respuestas. En el sprint 1 es siempre `MANUAL`; valores válidos en la BD: `MANUAL`, `FOTO` (`ck_horario_origen`). El cliente no lo envía | ✅ Definido (valores y `MANUAL` en sprint 1); 🟡 Propuesta (que el cliente no lo envíe) |

### Regla de solapamiento

- ✅ **Definido:** debe validarse que no haya solapamientos (#10) y la BD impide dos bloques del mismo usuario, día y hora de inicio (`uq_horario_slot`).
- 🟡 **Propuesta, a confirmar por Maite:** dos bloques del mismo usuario y del mismo día se solapan si `inicioA < finB` y `inicioB < finA`. Dos bloques **contiguos** (fin de uno = inicio del otro) **no** se consideran solapados. En `PUT` el bloque que se edita no se compara consigo mismo.

### `GET /api/horarios`

🔎 **Inferido** (el FE necesita leer los bloques para mostrar la cuadrícula y editarlos; se apoya en "Persistencia por usuario").

**Respuesta 200:** lista de bloques, ordenada por `diaSemana` y `horaInicio` (🟡 Propuesta). Sin bloques: `[]`.

```json
[
  {
    "id": 12,
    "materia": "Cálculo I",
    "diaSemana": 1,
    "horaInicio": "08:00:00",
    "horaFin": "09:30:00",
    "aula": "A-101",
    "origen": "MANUAL"
  }
]
```

**Errores:** `401` sin token o token inválido.

### `POST /api/horarios`

Alta de un bloque (#10).

**Request:**

```json
{
  "materia": "Cálculo I",
  "diaSemana": 1,
  "horaInicio": "08:00",
  "horaFin": "09:30",
  "aula": "A-101"
}
```

**Respuesta 201** (🟡 Propuesta: incluye header `Location: /api/horarios/{id}`): el bloque creado.

```json
{
  "id": 12,
  "materia": "Cálculo I",
  "diaSemana": 1,
  "horaInicio": "08:00:00",
  "horaFin": "09:30:00",
  "aula": "A-101",
  "origen": "MANUAL"
}
```

**Efecto secundario:** los puentes del usuario se recalculan automáticamente (ver sección 4).

**Errores**

| Código | Caso | Estado |
|---|---|---|
| 400 | Campo obligatorio vacío, `diaSemana` fuera de 1–6, `horaFin` no posterior a `horaInicio`, longitud excedida | ✅ Definido (reglas del schema); código 🟡 Propuesta |
| 401 | Sin token o token inválido | 🟡 Propuesta |
| 409 | Solapamiento con otro bloque del usuario, o bloque con el mismo día y hora de inicio (`uq_horario_slot`) | ✅ Definido (la validación); código 409 🟡 Propuesta |

### `PUT /api/horarios/{id}`

Edición de un bloque (#10). 🟡 **Propuesta:** `PUT` con reemplazo completo.

**Request:** los mismos campos que `POST` (sin `id` en el body).

**Respuesta 200:** el bloque actualizado, con la misma forma que en `POST`.

**Efecto secundario:** recálculo automático de puentes (sección 4).

**Errores**

| Código | Caso | Estado |
|---|---|---|
| 400 | Igual que `POST` | ✅ Definido (reglas); código 🟡 Propuesta |
| 401 | Sin token o token inválido | 🟡 Propuesta |
| 404 | El bloque no existe o no pertenece al usuario (no se distinguen) | 🟡 Propuesta |
| 409 | Solapamiento o bloque duplicado, igual que `POST` | ✅ Definido (la validación); código 🟡 Propuesta |

🟡 **Propuesta, a confirmar por Ariana y Maite:** quién fija `updated_at` al editar (el esquema no tiene `ON UPDATE`): el backend, con la hora UTC actual.

### `DELETE /api/horarios/{id}`

Eliminación de un bloque (#10).

**Respuesta 204** (🟡 Propuesta), sin cuerpo. **Efecto secundario:** recálculo automático de puentes.

**Errores:** `401` sin token o token inválido; `404` si el bloque no existe o no es del usuario (🟡 Propuesta).

## 4. Puentes (HU3 — responsable: Majo)

### Regla de detección

- ✅ **Definido** (#11): el sistema calcula automáticamente los bloques libres entre las clases del estudiante; si duran **45 minutos o más** (incluido exactamente 45) son un "puente"; si duran menos, no son un puente ni generan ningún evento.
- ✅ **Definido** (#11, criterio 4): al guardar un horario modificado o recargado, los puentes se recalculan solos, sin que el estudiante lo pida. Por eso **no existe un endpoint de recálculo**: ocurre dentro de `POST`, `PUT` y `DELETE` de `/api/horarios`.
- 🟡 **Propuesta, a confirmar por Majo:**
  - Solo cuentan huecos entre clases del mismo día; el tramo antes de la primera clase y después de la última no es puente (#11 dice "entre sus clases").
  - El mínimo de 45 minutos vive en un único lugar de configuración del backend, sin tocar el schema (`schema.sql` lo deja como parámetro, no como `CHECK`).
  - El recálculo es síncrono, dentro de la misma operación de `POST`, `PUT` o `DELETE`, y reemplaza todos los puentes del usuario.

### `GET /api/puentes`

🔎 **Inferido** (el FE necesita leer los puentes detectados; origen: #11 criterios 1 y 2). Requiere JWT.

**Respuesta 200:** lista de puentes del usuario, ordenada por `diaSemana` y `horaInicio` (🟡 Propuesta). Sin puentes: `[]`.

| Campo | Tipo | Estado |
|---|---|---|
| `id` | entero | ✅ Definido (columna `puente.id`) |
| `diaSemana` | entero, 1 a 6 | ✅ Definido (`ck_puente_dia`) |
| `horaInicio` | hora | ✅ Definido |
| `horaFin` | hora, posterior a `horaInicio` | ✅ Definido (`ck_puente_horas`) |

```json
[
  {
    "id": 5,
    "diaSemana": 1,
    "horaInicio": "09:30:00",
    "horaFin": "10:30:00"
  }
]
```

**Errores:** `401` sin token o token inválido (🟡 Propuesta).

🟡 **Propuesta, a confirmar por Majo y el FE:** no se incluye un campo de duración; el FE puede calcularla.

## 5. Endpoints existentes (fuera de `/api`)

| Endpoint | JWT | Respuesta | Estado |
|---|---|---|---|
| `GET /health` | No | `200`, sin cuerpo | ✅ Definido (existe en el backend) |
| `GET /health/db` | No | `200` si conecta a MySQL; `503` si no. Sin cuerpo | ✅ Definido (existe en el backend) |

## 6. Resumen de decisiones por responsable

| Responsable | Tema | Valor por defecto | Estado |
|---|---|---|---|
| Amira | Claims y duración del JWT, refresh | `sub` + `rol`; 60 min; sin refresh ni `expiresAt` | 🟡 Propuesta, a confirmar por Amira |
| Amira | Hash de contraseña | BCrypt (paquete BCrypt.Net-Next; versión por confirmar) | ✅ Definido (decisión de Ariana, base) |
| Amira | Correo no registrado | Mismo 401 y mismo mensaje que contraseña incorrecta | 🟡 Propuesta, a confirmar por Amira |
| Amira, Ariana | `correoVerificado` y `activo = 0` en el login | No se exige `correoVerificado`; `activo = 0` da el mismo 401 | 🟡 Propuesta, a confirmar por Amira y Ariana |
| Maite | Solapamiento | Contiguos (fin = inicio) no se solapan | 🟡 Propuesta, a confirmar por Maite |
| Maite | Hora local del campus | Sin conversión a UTC | 🟡 Propuesta, a confirmar por Maite |
| Maite, Majo | Paginación | Ninguna | 🟡 Propuesta, a confirmar por Maite y Majo |
| Maite | Texto de errores de validación y solapamiento | — | 🔴 Pendiente (Maite) |
| Ariana, Maite | `updated_at` | Lo fija el backend con la hora UTC actual | 🟡 Propuesta, a confirmar por Ariana y Maite |
| Majo | Tramos antes de la primera y después de la última clase | No son puente | 🟡 Propuesta, a confirmar por Majo |
| Majo, Ariana | Parámetro de 45 min | Un único lugar de configuración del backend | 🟡 Propuesta, a confirmar por Majo y Ariana |
| Majo, Maite | Recálculo de puentes | Síncrono, dentro de `POST`/`PUT`/`DELETE` de horarios; reemplaza los puentes del usuario | 🟡 Propuesta, a confirmar por Majo y Maite |
| Majo | Campo de duración en `GET /api/puentes` | No se incluye | 🟡 Propuesta, a confirmar por Majo |
| Majo | Texto de errores de puentes | — | 🔴 Pendiente (Majo) |
| Ariana | Origen CORS del FE | `http://localhost:3000` | 🟡 Propuesta, a confirmar por Ariana |
| Ariana | Usuario de prueba y creación de usuarios | Sin registro en el sprint 1; usuario de prueba aparte (ver #139) | 🔴 Pendiente (Ariana) |

# Contrato de API — BREAKU

**Borrador v0.1 — Sprint 1**

Alcance documentado: autenticación (HU1), horario (HU2), puentes (HU3) y ubicación (HU5).
Parte del issue #139 (ítem "Contrato de endpoints").

> **Nota (octubre 2026):** este documento es el borrador v0.1 del sprint 1. La API **implementada** está documentada en [`docs/api/openapi.yaml`](api/openapi.yaml) y en `/swagger`; donde difieran, esa es la referencia vigente. Diferencias conocidas entre este borrador y el código:
>
> | Tema | Este borrador | Implementado |
> |---|---|---|
> | Ruta de horarios | `/api/horarios` | `/api/horario` |
> | Login: cuerpo | `correo` | `email` |
> | Login: respuesta | `{ token, usuario: { id, nombre, correo, rol } }` | `{ token, userId, nombre, email }` |
> | Registro | Sin endpoint en el sprint 1 | Existe `POST /api/auth/register` |
> | Formato de errores | `ProblemDetails` con `detail` | Auth: `{ "message" }`. Horarios: `{ "codigo", "mensaje" }` |
> | Mensaje de login incorrecto (401) | «Correo o contraseña incorrectos» | «Credenciales inválidas.» (el frontend muestra el texto de #9 por su cuenta) |
> | Login con campos vacíos | 400 «Por favor, complete todos los campos» | Correo vacío: 400 con el mensaje de dominio institucional. Contraseña vacía: 401 |
> | Login y `activo` | Exige `activo = 1` | No se verifica |
> | JWT | Claims `sub` y `role`; 60 minutos | Claims `sub`, `email` y `nombre`; 24 horas |

## Leyenda

Cada decisión del documento lleva una de estas marcas:

| Marca | Significado |
|---|---|
| ✅ **Definido** | Sale de un criterio de aceptación (#9, #10, #11), o de `docs/database/schema.sql`. |
| 🟡 **Propuesta** | Decisión tomada para poder avanzar; la confirma la persona indicada. |
| 🔴 **Pendiente** | Nadie decidió todavía; se indica quién decide. |
| 🔎 **Inferido** | Endpoint que ningún criterio nombra literalmente, pero que el flujo necesita. |

Responsables: **Amira** = HU1 (auth), **Maite** = HU2 (horarios), **Majo** = HU3 (puentes), **Ariana** = BD y base del proyecto.

## Cómo usar este contrato

**Para FE (Amira #29, Maite #32, Majo #35)**
- Consume solo los endpoints de este documento. Si necesitas otro, pídelo a la persona responsable del área: no lo inventes.
- Muestra al usuario el campo `detail` de los errores tal cual viene; los tres mensajes de login están definidos en #9.
- Los campos marcados 🟡 o 🔴 pueden cambiar: no los dejes fijos en el código sin avisar.

**Para BE (Amira #30, Maite #33, Majo #36)**
- Implementa exactamente estas rutas, campos y códigos. Los DTOs no exponen entidades ni campos internos.
- Si algo contradice este documento, consulta antes de cambiarlo y actualiza el contrato en el mismo PR.

**Cambios al contrato:** se hacen por PR a `main`, con revisión de la persona dueña del área.

> **Usuarios:** sin endpoint de registro en el sprint 1; el usuario de prueba para desarrollo se define aparte (ver #139).

## 1. Convenciones generales

| Tema | Regla | Estado |
|---|---|---|
| Prefijo | Todos los endpoints de negocio cuelgan de `/api` | 🟡 Propuesta (confirmada por Ariana) |
| Nombres de rutas | Español y plural: `horarios`, `puentes` | 🟡 Propuesta (confirmada por Ariana) |
| JSON | camelCase | ✅ Definido (convención del equipo) |
| Idioma de los mensajes | Español | 🟡 Propuesta |
| Fechas con hora | UTC, ISO 8601, p. ej. `2026-10-06T13:07:07Z` | ✅ Definido (`schema.sql`, convención del equipo) |
| Horas de clase (`horaInicio`, `horaFin`) | Hora local del campus, sin conversión a UTC (columnas `TIME` sin fecha) | 🟡 Propuesta, a confirmar por Maite |
| Formato de hora | El backend responde `"HH:mm:ss"` (24 h), p. ej. `"14:30:00"`. Acepta `"HH:mm"` y `"HH:mm:ss"`; rechaza `"2:30 PM"` | 🟡 Propuesta (comportamiento de `TimeOnly` verificado en .NET 10) |
| Autenticación | Header `Authorization: Bearer <token>` en todo `/api`, salvo el login | ✅ Definido (JWT, decisión del equipo) |
| Usuario de la petición | Sale del token, nunca del body ni de la URL | 🟡 Propuesta |
| Paginación | Ninguna: las listas devuelven todo | 🟡 Propuesta, a confirmar por Maite y Majo |
| DTOs | No exponen entidades. Nunca salen `passwordHash`, `correoVerificado`, `consentimientoUbicacion`, `fechaConsentimiento`, `createdAt`, `updatedAt`, `updatedBy` ni `activo` | ✅ Definido (requisito del equipo) |

### Formato de error

🟡 **Propuesta:** `ProblemDetails` de ASP.NET (`application/problem+json`), basado en RFC 9457 (Problem Details for HTTP APIs). Es un único formato para todos los endpoints, incluido el login. El FE muestra `detail`.

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
| Claims | `sub` (id del usuario), `role` (rol del usuario), `iat` y `exp`. Sin `email` ni `nombre`: la respuesta de login ya devuelve `usuario` | 🟡 Propuesta, a confirmar por Amira |
| Duración | 60 minutos, configurable. Nombre de la clave de configuración: 🔴 **Pendiente** (Amira) | 🟡 Propuesta, a confirmar por Amira |
| Refresh token | Sin refresh en el sprint 1; la respuesta de login no incluye `expiresAt` | 🔴 **Pendiente** (Amira y Ariana) |
| Sin token / token inválido o vencido | `401` | 🟡 Propuesta |

Fuentes y motivos:
- **Claims:** RFC 7519 §4.1 define `sub`, `iat` y `exp` como claims registrados (todos opcionales). Se usa `role` y no `rol` porque ASP.NET Core traduce el nombre corto `role` al tipo de claim de roles por defecto (`MapInboundClaims` es `true` por defecto); con `rol` habría que configurar `RoleClaimType` aparte. El campo JSON `usuario.rol` de la respuesta de login no cambia.
- **Duración:** #9 no fija ningún valor y ningún criterio de HU1 pide sesión persistente (solo entrar y ser redirigido al panel, criterios 1 y 5). RFC 7519 no fija una duración. La OWASP Session Management Cheat Sheet da 4 a 8 horas como rango de tope absoluto para una app de uso durante una jornada completa; por eso se propone 60 minutos y no 24 horas.

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
| 401 | Correo no registrado | `Correo o contraseña incorrectos` (mismo mensaje) | ✅ Definido (mismo mensaje de #9 criterio 3; decisión de Ariana, base, según OWASP Authentication Cheat Sheet: no revelar qué correos existen) |
| 401 | Usuario con `activo = 0` (el login exige `activo = 1`) | `Correo o contraseña incorrectos` (mismo mensaje) | ✅ Definido (decisión de Ariana, base; borrado lógico con `activo`) |

### Decisiones de auth

| Duda | Valor por defecto | Estado |
|---|---|---|
| Claims, duración y refresh del token | Ver "Token JWT" arriba | 🟡 Propuesta, a confirmar por Amira; refresh y nombre de la clave de duración: 🔴 Pendiente (Amira) |
| Mensaje de error de login | `Correo o contraseña incorrectos`, el mismo para contraseña incorrecta, correo inexistente y usuario con `activo = 0` | ✅ Definido (#9 criterio 3) |
| Cuerpo de los errores de login | `ProblemDetails` (RFC 9457); el texto va en `detail` | 🟡 Propuesta, a confirmar por Amira |
| Algoritmo de hash de contraseña (`password_hash` es `VARCHAR(255)`) | BCrypt (paquete BCrypt.Net-Next; versión por confirmar). El paquete lo agrega Amira en #30 | ✅ Definido (decisión de Ariana, base) |
| ¿El login exige `correoVerificado = true`? | No en el sprint 1 (no existe flujo de verificación). Sí exige `activo = 1`. Si más adelante se agrega verificación, se vuelve a definir | ✅ Definido (decisión de Ariana, base); 🔴 Pendiente (Amira) si se agrega verificación |
| Creación de usuarios | Sin endpoint de registro en el sprint 1 (el alcance de HU1 es el login; #9 no tiene criterio de registro) | ✅ Definido (alcance del sprint); 🔴 Pendiente (Amira) si más adelante se agrega registro |

## 3. Horarios (HU2 — responsable: Maite)

Todos requieren JWT y operan solo sobre los bloques del usuario del token. Origen: #10 ("Alta/edición/eliminación de bloques de clase", "Validación de solapamientos", "Persistencia por usuario").

Campos del bloque (columnas de `horario`, `schema.sql`):

| Campo | Tipo | Validación | Estado |
|---|---|---|---|
| `id` | entero | Solo en respuestas | ✅ Definido |
| `materia` | string | Obligatorio, hasta 100 caracteres | ✅ Definido (schema) |
| `diaSemana` | entero | 1 a 6 (1 = lunes … 6 = sábado) | ✅ Definido (`ck_horario_dia`) |
| `horaInicio` | hora | Obligatoria, desde las **07:15** (la clase más temprana de la UCB). Antes: 400 «Las clases empiezan desde las 07:15.» | ✅ Definido (`ck_horario_rango`) |
| `horaFin` | hora | Obligatoria, posterior a `horaInicio` y hasta las **21:00** (la más tarde). Después: 400 «Las clases terminan, como muy tarde, a las 21:00.» | ✅ Definido (`ck_horario_horas`, `ck_horario_rango`) |
| `aula` | string o `null` | Opcional, hasta 30 caracteres | ✅ Definido (schema) |
| `origen` | string | Solo en respuestas. `MANUAL` (alta a mano) o `FOTO` (confirmado desde la foto del horario); valores válidos en la BD: `MANUAL`, `FOTO` (`ck_horario_origen`). El cliente no lo envía | ✅ Definido (valores); 🟡 Propuesta (que el cliente no lo envíe) |
| `asiste` | booleano | Opcional al enviar: al crear, `true` si no se envía; al editar, se conserva el valor actual. En respuestas siempre viene. `false` = el estudiante no asiste a ese bloque: no cuenta como tiempo ocupado y no choca con otros | ✅ Definido |
| `autorizacionCarrera` | booleano | Opcional al enviar (se conserva si no se envía). El estudiante confirma que su carrera autoriza que este bloque coincida con otro. Solo informativo: no cambia ningún cálculo | ✅ Definido |
| `coincide` | lista | Solo en respuestas. Los otros bloques del mismo día que se cruzan con éste, asista o no el estudiante: `{ id, materia, horaInicio, horaFin, asiste }` | ✅ Definido |
| `advertencias` | lista de textos | Solo en respuestas. Un aviso por cada bloque con el que choca de verdad (el estudiante asiste a los dos), por ejemplo `Coincide con «MAT-241» (14:15-15:45).` No impide guardar | ✅ Definido |

### Regla de choques (clases que coinciden)

- ✅ **Definido:** el horario oficial puede traer dos materias en la misma franja (por ejemplo MAT-133 y MAT-241, lunes a jueves 14:15-15:45). **El sistema no bloquea los choques**: guarda los bloques y devuelve una advertencia.
- ✅ **Definido:** dos bloques del mismo día chocan si se cruzan (`inicioA < finB` y `inicioB < finA`) **y el estudiante asiste a los dos** (`asiste = true`). Dos bloques contiguos (fin de uno = inicio del otro) no se cruzan. Un bloque con `asiste = false` nunca choca.
- ✅ **Definido:** lo único que se rechaza es repetir la misma materia el mismo día y a la misma hora de inicio (`409` con `codigo` = `DUPLICADO`; la BD lo respalda con `uq_horario_bloque`).
- ✅ **Definido:** el tiempo ocupado de un día es la **unión** de los intervalos de los bloques con `asiste = true`. Dos clases iguales, una dentro de otra o encadenadas no generan huecos negativos ni repetidos.
- ✅ **Definido:** en el flujo de la foto, el lector devuelve todos los bloques, también los que coinciden. Tras importar, la app pregunta «Estas clases coinciden, ¿a cuáles asistes?» y envía la respuesta a `PATCH /api/horario/asistencia`.

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
    "origen": "MANUAL",
    "asiste": true,
    "autorizacionCarrera": false,
    "coincide": [],
    "advertencias": []
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
  "origen": "MANUAL",
  "asiste": true,
  "autorizacionCarrera": false,
  "coincide": [],
  "advertencias": []
}
```

El `POST` acepta además `asiste` y `autorizacionCarrera` (opcionales). Si el bloque coincide con otro al que el estudiante asiste, la respuesta es igual de `201` y trae el aviso en `advertencias` y el otro bloque en `coincide`.

**Efecto secundario:** los puentes del usuario se recalculan automáticamente con los bloques a los que asiste (ver sección 4).

**Errores**

| Código | Caso | Estado |
|---|---|---|
| 400 | Campo obligatorio vacío, `diaSemana` fuera de 1–6, `horaFin` no posterior a `horaInicio`, longitud excedida | ✅ Definido (reglas del schema); código 🟡 Propuesta |
| 401 | Sin token o token inválido | 🟡 Propuesta |
| 409 | La misma materia ya existe ese día a esa hora de inicio (`codigo` = `DUPLICADO`, `uq_horario_bloque`). Un choque con otra materia **no** es error | ✅ Definido |

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
| 409 | Bloque duplicado, igual que `POST` | ✅ Definido |

🟡 **Propuesta, a confirmar por Ariana y Maite:** quién fija `updated_at` al editar (el esquema no tiene `ON UPDATE`): el backend, con la hora UTC actual.

### `PATCH /api/horario/{id}/asistencia`

Marca si el estudiante asiste a un bloque. Request: `{ "asiste": false, "autorizacionCarrera": true }` (`autorizacionCarrera` es opcional y, si no se envía, se conserva).

**Respuesta 200:** el bloque actualizado, con `coincide` y `advertencias` al día. **Efecto secundario:** recálculo de puentes con los bloques a los que asiste.

**Errores:** `401` sin token; `404` (`NO_ENCONTRADO`) si el bloque no existe o no es del usuario.

### `PATCH /api/horario/asistencia`

Cambia la asistencia de varios bloques de una vez (hasta 40). Request:

```json
{ "bloques": [{ "id": 12, "asiste": true }, { "id": 13, "asiste": false, "autorizacionCarrera": true }] }
```

**Respuesta 200:** la lista de bloques actualizados. Si alguno no existe o es de otro usuario no se cambia ninguno (`404`); una lista vacía o de más de 40 da `400 VALIDACION`. Los puentes se recalculan una sola vez.

### `DELETE /api/horarios/{id}`

Eliminación de un bloque (#10).

**Respuesta 204** (🟡 Propuesta), sin cuerpo. **Efecto secundario:** recálculo automático de puentes.

**Errores:** `401` sin token o token inválido; `404` si el bloque no existe o no es del usuario (🟡 Propuesta).

### Foto del horario

| Método | Ruta | Qué hace |
|---|---|---|
| `POST` | `/api/horario/foto` | `multipart/form-data` con `foto` (JPG, PNG o WebP, hasta 8 MB). Un modelo de visión (`LLM_MODEL_IMAGEN`) lee las clases y responde `{ clases, descartadas }` sin guardar nada. `400 VALIDACION` o `503 LECTOR_NO_DISPONIBLE` |
| `POST` | `/api/horario/lote` | `{ bloques: [...] }` (hasta 40, mismo formato que `POST /api/horario`). Guarda los válidos con `origen = FOTO` y responde `{ guardados, rechazados }`. Los que coinciden con otra clase **se guardan** y traen `coincide` y `advertencias`; solo se rechazan los inválidos y los repetidos (`DUPLICADO`), con el motivo de cada rechazo |
| `PATCH` | `/api/horario/asistencia` | Elige a cuáles de las clases que coinciden asiste el estudiante (ver arriba) |

## 4. Puentes (HU3 — responsable: Majo)

### Regla de detección

- ✅ **Definido** (#11): el sistema calcula automáticamente los bloques libres entre las clases del estudiante; si duran **45 minutos o más** (incluido exactamente 45) son un "puente"; si duran menos, no son un puente ni generan ningún evento.
- ✅ **Definido:** solo cuentan los bloques con `asiste = true`, y el tiempo ocupado de cada día es la unión de sus intervalos (ver «Regla de choques»).
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

## 6. Ubicación (HU5)

Todos los endpoints requieren JWT. El usuario siempre se obtiene del claim `sub`.
Las coordenadas se envían una sola vez al backend para encontrar la zona más cercana
dentro de su `radio_max_m`; no se guardan ni se devuelven. Solo se persiste el campus
seleccionado y, cuando corresponde, la fecha y el consentimiento explícito.

Los errores usan el mismo formato que horarios y recomendaciones: `{ "codigo": "NO_ENCONTRADO", "mensaje": "..." }`
(`NO_ENCONTRADO` con 404 y `FUERA_DE_ZONA` con 422).

### `GET /api/ubicacion/zonas`

Lista de las sedes **activas** disponibles para selección manual (las inactivas se conservan en la BD pero no se ofrecen). Respuesta `200`:

```json
[{ "id": 1, "nombre": "Campus central" }]
```

### `GET /api/ubicacion`

Estado de consentimiento y zona activa del estudiante. Respuesta `200`:

```json
{
  "consentimientoActivo": false,
  "zonaActual": { "id": 1, "nombre": "Campus central" }
}
```

`zonaActual` es `null` cuando todavía no se seleccionó un campus.

### `PUT /api/ubicacion`

Se llama después de informar el uso y obtener una aceptación explícita en la UI,
y después de que el navegador autorice la geolocalización. Busca la zona más cercana
que cubra las coordenadas y guarda únicamente su id, el consentimiento y la fecha.

Request:

```json
{ "latitud": -16.5, "longitud": -68.15 }
```

Respuesta `200`: el mismo formato de `GET /api/ubicacion`. Coordenadas fuera de rango
dan `400`; si no hay zona configurada que cubra el punto, da `422`.

### `PUT /api/ubicacion/zona`

Selección manual de un campus. No solicita ni registra coordenadas; guarda la zona
elegida y deja el consentimiento de geolocalización desactivado.

Request:

```json
{ "zonaId": 1 }
```

Respuesta `200`: el mismo formato de `GET /api/ubicacion`. Un id inexistente o de una sede inactiva da `404`.

### `DELETE /api/ubicacion`

Revoca el consentimiento de la aplicación y elimina la zona vinculada al usuario.
Respuesta `204`, sin cuerpo. La autorización del navegador se administra por separado
desde la configuración del navegador.

## 6b. Administración del campus

Requiere JWT con rol **`ADMIN`** (claim `role`). Con otro rol responde `403`. El login (`POST /api/auth/login`) devuelve el campo `rol` (`ESTUDIANTE` o `ADMIN`); el administrador de prueba está en `docs/database/seed-dev.sql`.

| Método | Ruta | Qué hace |
|---|---|---|
| `GET` | `/api/admin/resumen` | Lugares activos y en revisión, estudiantes activos, comunidades de hoy, consultas al asistente de la semana y reportes pendientes |
| `GET` | `/api/admin/lugares` | Catálogo completo (primero `EN_REVISION`) con cuántas veces se recomendó cada lugar |
| `PATCH` | `/api/admin/lugares/{id}/estado` | Cambia el estado a `ACTIVO`, `EN_REVISION` o `INACTIVO`; queda en `historial_cambio`. `204`, `400 VALIDACION` o `404 NO_ENCONTRADO` |
| `GET` | `/api/admin/reportes` | Reportes de estudiantes (hasta 100, primero `PENDIENTE`) |
| `GET` | `/api/admin/auditoria-rag?limite=50` | Últimas consultas al asistente con entrada, estado, modelo y latencia (1 a 200) |
| `GET` | `/api/admin/metricas` | Últimos 7 días: consultas por resultado, latencia promedio y los 5 lugares más recomendados |

## 7. Lugares viables (HU6 — responsable: Ariana)

Requiere JWT. El usuario siempre sale del token. Los errores usan `{ "codigo", "mensaje" }`.

### Regla de búsqueda

- ✅ **Definido** (#14): se muestran solo lugares viables para un puente. Un lugar es viable si cumple las tres condiciones:
  1. Está en estado `ACTIVO` (aprobado por el administrador) y es `recomendable`. `EN_REVISION` e `INACTIVO` nunca se muestran. Los puntos de referencia (campus, parqueo, bloques) tienen `recomendable = false`: están en el catálogo pero nunca se recomiendan.
  2. Queda dentro del radio (`radio_max_m`) de la zona (campus) del estudiante, que se define en HU5. La zona es el área verificada.
  3. El traslado de ida y vuelta más la actividad no supera la duración del puente. Si suman exactamente lo mismo, sí cabe.
- ✅ **Definido:** la sede activa se elige con el campo `zona.activa`. Solo las sedes activas se ofrecen en `GET /api/ubicacion/zonas` y reciben búsquedas: si la zona del estudiante quedó inactiva, la búsqueda responde `SIN_ZONA` hasta que elija otra. Hoy la sede activa es **UCB Cochabamba** y La Paz queda guardada pero inactiva (`docs/database/seed-dev-cochabamba.sql`).
- ✅ **Definido:** `validado = false` significa que el equipo todavía no revisó los datos del lugar. Se sigue mostrando, con la etiqueta «Por validar».
- 🟡 **Propuesta:** el traslado se calcula con la distancia en línea recta desde el centro de la zona y una velocidad de 80 m por minuto (unos 4,8 km/h), redondeando hacia arriba. La actividad dura `duracion_estimada_min` (RN03).
- 🟡 **Propuesta:** orden por distancia, del más cercano al más lejano. Máximo 50 lugares.
- 🔴 **Pendiente:** si el catálogo debe considerar `hora_apertura` y `hora_cierre` (hoy están como [CONFIRMAR] en el esquema y no se usan).

### `GET /api/lugares/viables?puenteId={id}`

`puenteId` debe ser un puente del propio estudiante (`GET /api/puentes`).

**Respuesta 200:**

```json
{
  "estado": "OK",
  "mensaje": null,
  "zona": { "id": 1, "nombre": "Campus UCB La Paz" },
  "radioAplicadoM": 800,
  "minutosDisponibles": 120,
  "lugares": [
    {
      "lugarId": 2,
      "nombre": "Biblioteca Central",
      "categoria": "Estudio",
      "descripcion": "Salas silenciosas y computadoras para estudiar.",
      "distanciaM": 55,
      "tiempoTrasladoMin": 2,
      "duracionEstimadaMin": 45,
      "validado": false,
      "tipoLugar": "campus",
      "modalidad": ["solitario", "grupal"],
      "nivelEnergia": ["bajo"],
      "actividadesSugeridas": ["estudiar", "leer", "adelantar tareas"]
    }
  ]
}
```

`validado`, `tipoLugar`, `modalidad`, `nivelEnergia` y `actividadesSugeridas` vienen del relevamiento de lugares; las listas vienen vacías y `tipoLugar` en `null` si el lugar no los tiene.

| `estado` | Cuándo | `mensaje` |
|---|---|---|
| `OK` | Hay al menos un lugar viable | `null` |
| `SIN_RESULTADOS` | Ningún lugar cumple tiempo, cercanía y verificación | `No encontramos opciones cercanas para este tiempo disponible.` (texto de la HU) |
| `SIN_ZONA` | El estudiante todavía no eligió ni compartió su campus (HU5); `zona` y `radioAplicadoM` vienen en `null` | `Elige tu campus para ver lugares cercanos.` |

**Errores**

| Código | Caso |
|---|---|
| 400 | Falta `puenteId` o no es un número positivo (`VALIDACION`) |
| 401 | Sin token o token inválido |
| 404 | El puente no existe o no es del usuario (`NO_ENCONTRADO`) |

## 7b. Matching de compañeros con puente compatible (HU12 — responsable: Maite)

Compañeros de tu misma sede que están libres al mismo tiempo que tú. Sin IA: es un cruce de horarios.

### Reglas

| Regla | Valor | Estado |
|---|---|---|
| Cruce por franja horaria | Dos puentes son compatibles si caen el **mismo día** y comparten **30 minutos o más**. Se devuelve el tramo en común: inicio = el más tardío de los dos inicios; fin = el más temprano de los dos fines. Si solo se tocan (fin = inicio), no se cruzan | ✅ Definido |
| Orden por compatibilidad | Primero quien comparte más minutos; en empate, quien coincide en más puentes de tu semana. `porcentaje` = minutos en común ÷ duración de tu puente | ✅ Definido |
| Quién aparece | Estudiantes activos, de tu misma sede (y la sede activa), que aceptaron aparecer (`usuario.visible_matching`). Nunca tú mismo | ✅ Definido |
| Reciprocidad | Para ver a otros, tienes que aceptar aparecer. Si no, el resultado es `NO_VISIBLE` | ✅ Definido |
| Privacidad | El consentimiento arranca desactivado. No se devuelve el correo ni el id de nadie. Cada cambio queda en `historial_cambio` | ✅ Definido |
| Tope | Hasta 20 compañeros por puente | 🟡 Propuesta (Maite) |

### `GET /api/matching?puenteId={id}`

Siempre responde **200** cuando el puente es tuyo; el campo `estado` dice qué pasó.

| `estado` | Cuándo | `mensaje` |
|---|---|---|
| `OK` | Hay compañeros | `null` |
| `SIN_ZONA` | No elegiste campus o tu sede ya no está activa | `Elige tu campus para encontrar compañeros con tiempo libre compatible.` |
| `NO_VISIBLE` | No aceptaste aparecer | `Para ver a tus compañeros, primero acepta que ellos también te encuentren.` |
| `SIN_RESULTADOS` | Nadie está libre contigo | `Por ahora nadie de tu campus está libre contigo en este puente.` |

```json
{
  "estado": "OK",
  "mensaje": null,
  "puente": { "id": 5, "diaSemana": 1, "horaInicio": "09:30:00", "horaFin": "11:00:00" },
  "minutosPuente": 90,
  "companeros": [
    { "nombre": "Ana Quiroga", "carrera": "Ingeniería de Sistemas", "semestre": 5,
      "horaInicio": "09:30:00", "horaFin": "11:00:00", "minutosEnComun": 90, "porcentaje": 100, "franjasCompartidas": 1 }
  ]
}
```

**Errores**

| Código | Caso |
|---|---|
| 400 | Falta `puenteId` o no es un número positivo (`VALIDACION`) |
| 401 | Sin token o token inválido |
| 404 | El puente no existe o no es del usuario (`NO_ENCONTRADO`) |

### `GET /api/matching/configuracion` y `PUT /api/matching/configuracion`

`GET` devuelve `{ "visible": false, "desde": null }`. `PUT` recibe `{ "visible": true }` (obligatorio; sin él responde 400 `VALIDACION`) y devuelve lo mismo con la fecha del consentimiento. Al retirarlo, `desde` vuelve a `null`.

## 7c. Valoración de recomendaciones (HU9 — responsable: Amira)

| Regla | Valor | Estado |
|---|---|---|
| Valores | `LIKE` (me gusta) o `DISLIKE` (no me sirve); no distingue mayúsculas ni espacios | ✅ Definido (`ck_reco_feedback`) |
| Asociación | El voto va en la fila de `recomendacion`, que pertenece a una consulta (`log_auditoria_rag.usuario_id`). Solo se valoran recomendaciones propias; la de otro responde 404 | ✅ Definido |
| Sin duplicados | Una sola valoración por recomendación: votar de nuevo reemplaza la anterior y actualiza `fecha_feedback`; repetir el mismo voto no escribe | ✅ Definido |
| Para qué sirve | `GET /api/admin/metricas` muestra me gusta y no me sirve de la semana, en total y por lugar | ✅ Definido |

### `PATCH /api/recomendaciones/{id}/feedback`

Body: `{ "feedback": "LIKE" }`. Respuesta **200**:

```json
{ "recomendacionId": 87, "feedback": "LIKE", "fechaFeedback": "2026-10-31T14:00:04.512Z" }
```

Cada recomendación de `POST /api/recomendaciones` trae `feedback` (`LIKE`, `DISLIKE` o `null`).

**Errores**

| Código | Caso |
|---|---|
| 400 | El voto no es `LIKE` ni `DISLIKE` (`VALIDACION`) |
| 401 | Sin token o token inválido |
| 404 | La recomendación no existe o no es del usuario (`NO_ENCONTRADO`) |

## 8. Resumen de decisiones por responsable

| Responsable | Tema | Valor por defecto | Estado |
|---|---|---|---|
| HU5 | Ubicación precisa | Solo se transmite durante la resolución de zona; no se persiste | ✅ Definido |
| HU5 | Consentimiento | Desactivado inicialmente; selección manual no concede permiso GPS | ✅ Definido |
| HU5 | Catálogo | Lo administra la base de datos en `zona`; sin zonas activas no puede resolverse la ubicación | ✅ Definido |

| Responsable | Tema | Valor por defecto | Estado |
|---|---|---|---|
| Amira | Claims del JWT | `sub`, `role`, `iat`, `exp` | 🟡 Propuesta, a confirmar por Amira |
| Amira | Duración del JWT | 60 min, configurable | 🟡 Propuesta, a confirmar por Amira |
| Amira | Nombre de la clave de configuración de la duración | — | 🔴 Pendiente (Amira) |
| Amira, Ariana | Refresh token | Sin refresh en el sprint 1 | 🔴 Pendiente (Amira y Ariana) |
| Amira | Hash de contraseña | BCrypt (paquete BCrypt.Net-Next; versión por confirmar) | ✅ Definido (decisión de Ariana, base) |
| Amira | Mensaje de login (contraseña incorrecta, correo inexistente, `activo = 0`) | `Correo o contraseña incorrectos` | ✅ Definido (#9 criterio 3) |
| Amira | Cuerpo de error de login | `ProblemDetails`; el texto va en `detail` | 🟡 Propuesta, a confirmar por Amira |
| Amira | `correoVerificado` y `activo` en el login | No se exige `correoVerificado`; se exige `activo = 1` | ✅ Definido (decisión de Ariana, base); 🔴 Pendiente (Amira) si se agrega verificación |
| Amira | Registro de usuarios | Sin endpoint en el sprint 1 | ✅ Definido (alcance); 🔴 Pendiente (Amira) si se agrega más adelante |
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
| Ariana | Usuario de prueba | `docs/database/seed-dev.sql` (solo desarrollo; ver README) | ✅ Definido (en `main`, PR #144) |
| Amira | Token de desarrollo | — | 🔴 Pendiente (depende del JWT de HU1, Amira) |

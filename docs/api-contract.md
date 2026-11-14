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

### `POST /api/auth/google`

Requiere JWT: **no**. Estado: ✅ Definido. Solo funciona si el servidor tiene `GOOGLE_CLIENT_ID`; si no, **503** y el
frontend no muestra el botón (le falta `NEXT_PUBLIC_GOOGLE_CLIENT_ID`).

```json
{ "credencial": "<ID token que Google Identity Services le da al frontend>" }
```

El servidor verifica el token con las claves públicas de Google (firma, emisor, audiencia = nuestro Client ID y
vencimiento; no hace falta client secret) y decide (`Domain/ReglasCuenta.DecidirGoogle`):

| Caso | Resultado |
|---|---|
| Ya entró con Google antes (`usuario.google_sub`) | **200**, entra a esa cuenta |
| Hay una cuenta con ese correo, sin Google | **200**, se enlaza (Google ya verificó que el correo es suyo) y entra |
| No hay cuenta | **200**, se crea como `ESTUDIANTE`, con el nombre de Google y **sin contraseña** (la puede crear en Perfil → Seguridad) |
| Correo que no es `@ucb.edu.bo`, o sin verificar en Google | **400** «Entra con tu cuenta de Google institucional (@ucb.edu.bo).» / «Tu cuenta de Google no tiene el correo verificado.» |
| Cuenta `ADMIN` | **400** «El administrador entra con su correo y contraseña.» (nadie se vuelve admin por una cuenta de Google) |
| Cuenta desactivada, o el correo ya enlazado a otra cuenta de Google | **400** |
| Token inválido, vencido o de otra app | **401** «No pudimos verificar tu cuenta de Google. Inténtalo de nuevo.» |

La respuesta 200 es la misma del login. Login y Google comparten un límite de **30 intentos por minuto por IP** (alto
porque en el campus muchos salen por la misma IP) → **429**.

**Configurar Google (una vez):** en [console.cloud.google.com](https://console.cloud.google.com) crear un proyecto,
configurar la pantalla de consentimiento (Externo; mientras está en «Testing» solo entran los usuarios de prueba que se
agreguen, hasta 100) y crear un **ID de cliente de OAuth → Aplicación web** con `http://localhost:3000` (y la URL del
despliegue) en «Orígenes de JavaScript autorizados». El Client ID va en `GOOGLE_CLIENT_ID` (backend) y en
`NEXT_PUBLIC_GOOGLE_CLIENT_ID` (frontend).

### Decisiones de auth

| Duda | Valor por defecto | Estado |
|---|---|---|
| Claims, duración y refresh del token | Ver "Token JWT" arriba | 🟡 Propuesta, a confirmar por Amira; refresh y nombre de la clave de duración: 🔴 Pendiente (Amira) |
| Mensaje de error de login | `Correo o contraseña incorrectos`, el mismo para contraseña incorrecta, correo inexistente y usuario con `activo = 0` | ✅ Definido (#9 criterio 3) |
| Cuerpo de los errores de login | `ProblemDetails` (RFC 9457); el texto va en `detail` | 🟡 Propuesta, a confirmar por Amira |
| Algoritmo de hash de contraseña (`password_hash` es `VARCHAR(255)`) | BCrypt (paquete BCrypt.Net-Next; versión por confirmar). El paquete lo agrega Amira en #30 | ✅ Definido (decisión de Ariana, base) |
| Cuenta sin contraseña (entra solo con Google) que intenta el login normal | **401** «Credenciales inválidas.» (antes, BCrypt fallaba y respondía 500) | ✅ Definido |
| Contraseña (registro y cambio) | De 8 a 72 bytes (lo que usa BCrypt), con al menos una letra y un número, sin espacios al inicio ni al final (`ReglasCuenta.ValidarNueva`) | ✅ Definido |
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
| Duración | — | Al menos **45 minutos**: una clase empieza como muy tarde a las 20:15 y termina como muy temprano a las 08:00. Menos: 400 «Una clase dura al menos 45 minutos.» | ✅ Definido (`ck_horario_duracion`) |
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
- ✅ **Definido** (Majo y Maite): además de los puentes, cada día con clases tiene un tramo **después de clases**: desde que termina la última clase a la que asiste hasta las **21:00** (`ReglasHorario.FinMasTarde`), si son 45 minutos o más. Se guarda en `puente` con `tipo = DESPUES_DE_CLASES`, así que recomendaciones, lugares, matching, invitaciones y avisos lo usan como cualquier otro puente. El tiempo antes de la primera clase y los días sin clases no cuentan.
- ✅ **Definido** (#11, criterio 4): al guardar un horario modificado o recargado, los puentes se recalculan solos, sin que el estudiante lo pida. Por eso **no existe un endpoint de recálculo**: ocurre dentro de `POST`, `PUT` y `DELETE` de `/api/horarios`.
- 🟡 **Propuesta, a confirmar por Majo:**
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
| `tipo` | `ENTRE_CLASES` o `DESPUES_DE_CLASES` | ✅ Definido (`ck_puente_tipo`) |

```json
[
  {
    "id": 5,
    "diaSemana": 1,
    "horaInicio": "09:30:00",
    "horaFin": "10:30:00",
    "tipo": "ENTRE_CLASES"
  }
]
```

**Errores:** `401` sin token o token inválido (🟡 Propuesta).

🟡 **Propuesta, a confirmar por Majo y el FE:** no se incluye un campo de duración; el FE puede calcularla.

### Avisos de puente (HU4)

| Tema | Regla | Estado |
|---|---|---|
| Cuándo se crea | Un proceso del servidor revisa cada 15 segundos los puentes de hoy (hora de Bolivia) que **ya empezaron y siguen en curso**, y crea el aviso que falte (`PUENTE_DISPONIBLE` o `DESPUES_DE_CLASES`). Antes solo miraba el primer minuto del puente: si ningún servidor estaba prendido justo en ese minuto, el aviso no llegaba nunca | ✅ Definido |
| A quién | A **todos** los estudiantes con avisos encendidos, estén o no usando la app. Queda sin leer en `GET /api/notificaciones` y se ve al entrar | ✅ Definido |
| Demasiado tarde | Si al puente le quedan menos de **10 minutos**, ya no se avisa | ✅ Definido |
| Texto | A tiempo: «¡Puente disponible! Tienes 2 h libres, de 10:00 a 12:00.». Atrasado: «¡Estás en un puente! Te quedan 1 h 30 min libres, hasta las 12:00.». Después de clases: «¡Terminaste tus clases! Tienes … libres, hasta las 21:00.» (con lo que queda) | ✅ Definido |
| Sin duplicados | Uno por estudiante, día e inicio del puente (`uq_notif_puente`), aunque haya varios servidores prendidos contra la misma base | ✅ Definido |
| Domingo | Sin avisos | ✅ Definido |

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
| `PATCH` | `/api/admin/lugares/{id}/validacion` | Marca un lugar como validado (`{ "validado": true }`) o le quita la validación (`false`). Es independiente del estado: un lugar `ACTIVO` puede seguir «Por validar» hasta que el equipo revise sus datos. Queda en `historial_cambio`. `204`, `400 VALIDACION` o `404 NO_ENCONTRADO` |
| `GET` | `/api/admin/categorias` | Categorías del catálogo (para el formulario de lugar) |
| `GET` | `/api/admin/campus` | Campus activos con su centro (`latitud`, `longitud`) y su radio (`radioMaxM`): un lugar tiene que quedar dentro de alguno |
| `GET` | `/api/admin/lugares/{id}` | Un lugar con todos sus datos editables. `200` o `404 NO_ENCONTRADO` |
| `POST` | `/api/admin/lugares` | Crea un lugar (queda `ACTIVO` y validado). **`201`** `{ "id": … }`; `400 VALIDACION`; `409 NOMBRE_REPETIDO`. Queda `CREATE` en `historial_cambio` |
| `PUT` | `/api/admin/lugares/{id}` | Edita un lugar con las mismas reglas. **`204`**; `400`; `404`; `409 NOMBRE_REPETIDO`. Queda `UPDATE` (antes y después) en `historial_cambio` |
| `DELETE` | `/api/admin/lugares/{id}` | Elimina un lugar **que nunca se usó**. **`204`**; `404`; `409 EN_USO` si ya aparece en recomendaciones, reportes o comunidades (hay que desactivarlo). Queda `DELETE` en `historial_cambio` |
| `PUT` | `/api/admin/lugares/{id}/foto` | Sube o reemplaza la **foto real** del lugar: `multipart/form-data` con el archivo en `foto`, JPG, PNG o WebP de hasta **2 MB** (el panel la achica antes). El tipo se revisa por el contenido, no por la extensión. **`200`** `{ "fotoUrl": "/api/lugares/21/foto?v=…" }`; `400 VALIDACION`; `404 NO_ENCONTRADO`. Queda `UPDATE` `{"foto": …}` en `historial_cambio` (sin los bytes) |
| `DELETE` | `/api/admin/lugares/{id}/foto` | Quita la foto; vuelve la ilustración de la categoría. **`204`** (también si no tenía); `404 NO_ENCONTRADO` |
| `GET` | `/api/admin/reportes` | Reportes de estudiantes (hasta 100, primero `PENDIENTE`). Los que no son de un lugar ni de una comunidad se ven con objeto «General» |
| `PATCH` | `/api/admin/reportes/{id}` | Body `{ "estado": "RESUELTO" }` o `"DESCARTADO"`. Cierra un reporte `PENDIENTE` y guarda al admin como `revisor_id` y la fecha en `revisado_en`. **204**; 400 `VALIDACION` (otra decisión, incluida `PENDIENTE`: no se reabre); 404 `NO_ENCONTRADO`; 409 `YA_REVISADO` (ya tenía decisión: no se cambia) |
| `GET` | `/api/admin/auditoria-rag?limite=50` | Últimas **conversaciones** con el asistente (1 a 200): los turnos de una misma conversación (crear una actividad: «jugar cartas» → «¿A qué hora?» → «5») vienen juntos, con `entrada` (el primero), `respuestas` (los siguientes) y `turnos`. Estado, modelo y latencia sumada. Un rechazo de seguridad en cualquier turno marca toda la conversación |
| `GET` | `/api/admin/metricas` | Últimos 7 días: consultas por resultado, latencia promedio, los 5 lugares más recomendados y `rendimiento` de las recomendaciones (P50, P95, % por debajo de 3,5 s y respuestas del plan B, HU19) |

**Reglas del catálogo** (`ReglasLugar`, y en la base `uq_lugar_nombre` y `ck_lugar_horario`):

| Campo | Regla |
|---|---|
| `nombre` | De 3 a 100 caracteres, sin espacios de más; único en el catálogo |
| `descripcion` | Opcional, hasta 500 caracteres |
| `categoriaId` | Una de `GET /api/admin/categorias` |
| `latitud`, `longitud` | Dentro del radio de un campus activo (el mismo radio con el que se buscan lugares viables) |
| `duracionEstimadaMin` | De 1 a 240 minutos |
| `horaApertura`, `horaCierre` | `HH:mm`, las dos o ninguna; el cierre, después de la apertura |
| `minAPieDesdeBloqueA` | Opcional, de 0 a 60 |

Body de `POST` y `PUT`:

```json
{
  "nombre": "Sala de Estudio Norte", "descripcion": "Mesas grandes y enchufes", "categoriaId": 2,
  "latitud": -17.3716, "longitud": -66.1439, "duracionEstimadaMin": 40,
  "horaApertura": "08:00", "horaCierre": "20:00", "minAPieDesdeBloqueA": 3
}
```

## 6b-bis. Cambiar la contraseña desde el perfil

### `PUT /api/perfil/contrasena`

Requiere JWT. Perfil → Seguridad.

```json
{ "actual": "********", "nueva": "nueva2026", "confirmacion": "nueva2026" }
```

| Caso | Respuesta |
|---|---|
| Correcto | **204** (sin cuerpo). Queda en `historial_cambio` como `{"contrasena": "(oculta)"}` → `{"contrasena": "cambiada"}`, sin la contraseña |
| La cuenta entra solo con Google (no tiene contraseña) | `actual` no hace falta: la contraseña se **crea** (`"creada"` en el historial) y desde ahí también puede entrar con correo y contraseña |
| Falta `actual` | **400** `VALIDACION` «Escribe tu contraseña actual.» |
| `actual` incorrecta | **400** `CONTRASENA_INCORRECTA` «La contraseña actual no es correcta.» |
| La nueva no cumple las reglas, no coincide con `confirmacion` o es igual a la actual | **400** `VALIDACION` con el motivo |
| Más de 5 intentos en un minuto | **429** `DEMASIADAS_SOLICITUDES` «Hiciste muchos intentos seguidos. Espera un minuto y vuelve a intentarlo.» |

`GET /api/perfil` trae además `tieneContrasena` (false si entra solo con Google) y `conGoogle` (si la cuenta está enlazada).

## 6c. Reportes de los estudiantes

Perfil → Ajustes → «Cuéntanos qué pasó». Requiere JWT. Los reportes quedan `PENDIENTE` y los revisa un administrador
(`GET /api/admin/reportes` y `PATCH /api/admin/reportes/{id}`).

| Regla | Valor | Estado |
|---|---|---|
| Motivos | `ERROR_APP` (algo no funciona), `SUGERENCIA` y `LUGAR_INCORRECTO` (información incorrecta de un lugar). La base admite además `COMUNIDAD_INAPROPIADA` para más adelante | ✅ Definido |
| Objeto | `LUGAR_INCORRECTO` lleva `lugarId`; los otros dos van sin lugar (en el panel se ven como «General»). Lo exige también la base (`ck_rep_objeto`) | ✅ Definido |
| Descripción | De 10 a 500 caracteres (`ck_rep_descripcion`) | ✅ Definido |
| Sin abusos | Un mismo lugar no se reporta otra vez mientras siga pendiente; como mucho 3 reportes pendientes por estudiante | ✅ Definido |

| Método | Ruta | Qué hace |
|---|---|---|
| `GET` | `/api/reportes/lugares` | Lugares activos por nombre, para elegir cuál reportar |
| `POST` | `/api/reportes` | `{ "motivo": "LUGAR_INCORRECTO", "lugarId": 4, "descripcion": "…" }` → **`201`** `{ "id": 12, "estado": "PENDIENTE" }`; `400 VALIDACION`; `404 NO_ENCONTRADO` (lugar); `409 YA_REPORTADO` o `DEMASIADOS_PENDIENTES` |

## 7. Lugares viables (HU6 — responsable: Ariana)

Requiere JWT. El usuario siempre sale del token. Los errores usan `{ "codigo", "mensaje" }`.

### Regla de búsqueda

- ✅ **Definido** (#14): se muestran solo lugares viables para un puente. Un lugar es viable si cumple las tres condiciones:
  1. Está en estado `ACTIVO` (aprobado por el administrador) y es `recomendable`. `EN_REVISION` e `INACTIVO` nunca se muestran. Los puntos de referencia (campus, parqueo, bloques) tienen `recomendable = false`: están en el catálogo pero nunca se recomiendan.
  2. Queda dentro del radio (`radio_max_m`) de la zona (campus) del estudiante, que se define en HU5. La zona es el área verificada.
  3. El traslado de ida y vuelta más la actividad no supera la duración del puente. Si suman exactamente lo mismo, sí cabe.
- ✅ **Definido:** la sede activa se elige con el campo `zona.activa`. Solo las sedes activas se ofrecen en `GET /api/ubicacion/zonas` y reciben búsquedas: si la zona del estudiante quedó inactiva, la búsqueda responde `SIN_ZONA` hasta que elija otra. Hoy la sede activa es **UCB Cochabamba** y La Paz queda guardada pero inactiva (`docs/database/seed-dev-cochabamba.sql`).
- ✅ **Definido:** `validado = false` significa que el equipo todavía no revisó los datos del lugar. Se sigue mostrando, con la etiqueta «Por validar».
- ✅ **Definido** (HU8, #16): un lugar sin nombre, categoría, distancia o duración válida no se devuelve, ni en `GET /api/lugares/viables` ni en las recomendaciones.
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

### Fotos de los lugares (admin)

| Tema | Regla | Estado |
|---|---|---|
| Dónde se guardan | En la base, tabla `lugar_foto` (una por lugar, migración 019), no en el disco del servidor: así todo el equipo las ve desde la base compartida. Las listas no cargan los bytes | ✅ Definido |
| Cómo se muestran | `fotoUrl` en `GET /api/lugares/viables`, en las recomendaciones y en el detalle del panel. `null` = sin foto: se muestra la ilustración de la categoría. La foto se pide con `GET /api/lugares/{id}/foto?v=…` | ✅ Definido |
| `GET /api/lugares/{id}/foto` | **Público** (la etiqueta `img` no manda el token). Con `?v=` responde `Cache-Control: public, max-age=31536000, immutable`; la versión cambia cada vez que el admin sube otra. `404 NO_ENCONTRADO` si no tiene | ✅ Definido |
| Tamaño | Hasta 2 MB por foto (`ck_foto_bytes`). El panel la achica a 1600 px de lado como máximo y la convierte a WebP antes de subirla | ✅ Definido |

## 7b. Libres contigo: matching por tiempo libre y afinidad (HU12 — responsable: Maite)

Compañeros de tu misma sede que están libres al mismo tiempo que tú **y** con quienes tienes cosas en común según un test
corto (unos 2 minutos). El objetivo es conectar: puedes saludar a quien te sale y, si acepta, los dos ven el correo del otro.
Sin IA: es un cruce de horarios y de respuestas.

### Reglas

| Regla | Valor | Estado |
|---|---|---|
| Cruce por franja horaria | Dos puentes son compatibles si caen el **mismo día** y comparten **30 minutos o más**. Se devuelve el tramo en común: inicio = el más tardío de los dos inicios; fin = el más temprano de los dos fines. Si solo se tocan (fin = inicio), no se cruzan | ✅ Definido |
| Test de afinidad | 11 preguntas de opción múltiple en 5 bloques: tus gustos (qué harías con una hora libre, de qué hablarías horas, música), tu forma de ser (en un grupo nuevo, tu puente ideal), cómo conectas (qué te da confianza, qué buscas), tu día a día (energía, cómo estudias) y valores y creencias (**opcional**). Las preguntas salen del backend (`Domain/ReglasAfinidad.cs`, `GET /api/afinidad/test`) | ✅ Definido (Maite) |
| Sin test, nadie | Si no hiciste el test, el resultado es `SIN_TEST` y no se muestra a nadie. Solo aparece quien también lo hizo | ✅ Definido |
| Afinidad (0–100) | Por pregunta respondida por los dos: varias respuestas → con al menos una en común, 0,4 + 0,6 × Dice; una respuesta → igual 1, vecina en escala 0,5, distinta 0. Promedio ponderado (gustos y cómo conectas pesan más). Las opcionales solo cuentan si las respondieron los dos | ✅ Definido |
| Coincidir en las dos cosas | Se muestra solo si coinciden en tiempo (30 min) **y** en el test (afinidad ≥ 35 y al menos una cosa en común). Si falla una, no aparece | ✅ Definido |
| Compatibilidad | La que se muestra: 60 % afinidad + 40 % tiempo compartido (`porcentaje`). Orden: compatibilidad, luego minutos en común | ✅ Definido |
| Lo que los conecta | `enComun`: hasta 3 respuestas compartidas, de las preguntas de más peso. «Otra religión» no se muestra (no implica la misma) | ✅ Definido |
| Quién aparece | Estudiantes activos, de tu misma sede (y la sede activa), que aceptaron aparecer (`usuario.visible_matching`). Nunca tú mismo | ✅ Definido |
| Reciprocidad | Para ver a otros, tienes que aceptar aparecer. Si no, el resultado es `NO_VISIBLE` | ✅ Definido |
| Privacidad | Antes de conectar solo se ve el nombre corto («Ana Q.»), la carrera, el semestre, la compatibilidad y lo que los conecta; nunca el correo. Se devuelve el id interno, que hace falta para saludar. Al aceptar un saludo, los dos ven el nombre completo y el correo institucional. El rechazo es silencioso | ✅ Definido |
| Saludos | Solo a quien el matching te muestra en ese momento para ese puente (se recalcula al saludar). Una conexión por pareja, en cualquier sentido (`uq_conexion_par`). Hasta 5 saludos sin respuesta | ✅ Definido |
| Tope | Hasta 20 compañeros por puente | 🟡 Propuesta (Maite) |

### `GET /api/matching?puenteId={id}`

Siempre responde **200** cuando el puente es tuyo; el campo `estado` dice qué pasó.

| `estado` | Cuándo | `mensaje` |
|---|---|---|
| `OK` | Hay compañeros | `null` |
| `SIN_ZONA` | No elegiste campus o tu sede ya no está activa | `Elige tu campus para encontrar compañeros con tiempo libre compatible.` |
| `NO_VISIBLE` | No aceptaste aparecer | `Para ver a tus compañeros, primero acepta que ellos también te encuentren.` |
| `SIN_TEST` | No hiciste el test de afinidad | `Haz el test de afinidad (unos 2 minutos) para ver con quién coincides, además de en tiempo libre.` |
| `SIN_RESULTADOS` | Nadie está libre contigo | `Por ahora nadie de tu campus está libre contigo a esa hora.` |
| `SIN_RESULTADOS` | Hay gente libre, pero nadie coincide en el test | `Hay gente libre a esa hora, pero nadie con quien coincidas también en el test. Prueba con otro de tus puentes.` |

```json
{
  "estado": "OK",
  "mensaje": null,
  "puente": { "id": 21, "diaSemana": 1, "horaInicio": "09:30:00", "horaFin": "10:30:00", "tipo": "ENTRE_CLASES" },
  "minutosPuente": 60,
  "companeros": [
    { "usuarioId": 4, "nombre": "Lucía R.", "carrera": "Psicología", "semestre": 7,
      "horaInicio": "09:30:00", "horaFin": "10:30:00", "minutosEnComun": 60, "porcentaje": 100, "franjasCompartidas": 1,
      "afinidad": 74, "compatibilidad": 84, "enComun": ["Videojuegos", "El mismo humor", "Hacer cosas juntos"],
      "conexion": { "id": 2, "estado": "PENDIENTE", "enviadaPorMi": true } }
  ]
}
```

`conexion` es `null` si todavía no hay saludo. Si la otra persona rechazó tu saludo, igual se te muestra `PENDIENTE`.

**Errores**

| Código | Caso |
|---|---|
| 400 | Falta `puenteId` o no es un número positivo (`VALIDACION`) |
| 401 | Sin token o token inválido |
| 404 | El puente no existe o no es del usuario (`NO_ENCONTRADO`) |

### `GET /api/matching/configuracion` y `PUT /api/matching/configuracion`

`GET` devuelve `{ "visible": false, "desde": null }`. `PUT` recibe `{ "visible": true }` (obligatorio; sin él responde 400 `VALIDACION`) y devuelve lo mismo con la fecha del consentimiento. Al retirarlo, `desde` vuelve a `null`.

### `GET /api/afinidad/test` y `PUT /api/afinidad/test`

`GET` devuelve las preguntas y, si ya lo hiciste, tus respuestas (para editarlas):

```json
{
  "version": 1, "minutosEstimados": 2, "completado": false, "respuestas": null, "actualizadoEn": null,
  "preguntas": [
    { "id": "planes", "bloque": "Tus gustos", "texto": "¿Qué harías con una hora libre?", "tipo": "MULTIPLE", "maximo": 3,
      "opcional": false, "opciones": [ { "id": "deporte", "texto": "Hacer deporte" } ] }
  ]
}
```

`PUT` recibe `{ "respuestas": { "planes": ["videojuegos", "musica"], "grupo": ["rompo"], ... } }` y devuelve lo mismo con
`completado: true`. Rehacerlo reemplaza las respuestas (conserva `completado_en`).

| Código | Caso |
|---|---|
| 400 | Falta una obligatoria, pregunta u opción que no existe, repetidas, más del máximo o más de una en una de `UNICA` (`VALIDACION`, con el motivo) |
| 401 | Sin token o token inválido |

### `GET /api/conexiones`

```json
{
  "recibidas": [ { "id": 3, "estado": "PENDIENTE", "enviadaPorMi": false, "nombre": "Maite A.", "carrera": "Ingeniería de Sistemas",
                   "semestre": 5, "afinidad": 100, "enComun": ["Videojuegos"], "diaSemana": 1,
                   "horaInicio": "09:30:00", "horaFin": "10:30:00", "correo": null, "creadaEn": "2026-11-14T15:00:00Z" } ],
  "enviadas": [],
  "aceptadas": [ { "id": 1, "estado": "ACEPTADA", "nombre": "Ana Quiroga", "correo": "ana.quiroga@ucb.edu.bo" } ]
}
```

`recibidas` y `enviadas` son las pendientes; `aceptadas`, en cualquier sentido, con nombre completo y correo. `afinidad` es la
del test (se guarda al saludar en `conexion.afinidad`): es la misma vista desde los dos lados, a diferencia de la
compatibilidad del matching, que depende del puente de cada uno.

### `POST /api/conexiones`

Body `{ "usuarioId": 4, "puenteId": 21 }`. Responde **201** con la conexión (`PENDIENTE`, nombre corto, sin correo) y avisa
al otro con `CONEXION_SOLICITADA` (si tiene los avisos encendidos): «Maite A. (100 % de afinidad) quiere coincidir contigo el
lunes de 09:30 a 10:30. Respóndele en Libres contigo.».

| Código | Caso |
|---|---|
| 400 | Faltan `usuarioId` o `puenteId`, o te saludas a ti mismo (`VALIDACION`) |
| 404 | El puente no es tuyo (`NO_ENCONTRADO`) |
| 409 | Ya hay una conexión con esa persona, en cualquier sentido (`YA_EXISTE`); ya tienes 5 saludos sin respuesta (`DEMASIADOS_PENDIENTES`); ya no coinciden en ese puente o no te sale en el matching (`NO_COINCIDEN`) |

### `PUT /api/conexiones/{id}`

Body `{ "aceptar": true }`. Solo quien recibió el saludo responde, una vez. Al aceptar, devuelve la conexión con el nombre
completo y el correo de quien saludó, y le avisa con `CONEXION_ACEPTADA`. El aviso del saludo se apaga. Rechazar no avisa.

| Código | Caso |
|---|---|
| 400 | Falta `aceptar`, o intentas responder tu propio saludo (`VALIDACION`) |
| 404 | El saludo no existe o no es tuyo (`NO_ENCONTRADO`) |
| 409 | Ya estaba respondido (`YA_RESPONDIDA`) |

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

## 7d. Extracción estructurada de actividades (HU11 — responsable: Majo)

Convierte lo que el estudiante escribe en sus palabras en datos consistentes de una actividad. **Solo extrae y valida: no crea nada.** La pantalla de resumen, la confirmación y la creación de la comunidad son de HU10 (Ariana), que llama a este endpoint.

| Regla | Valor | Estado |
|---|---|---|
| Tool calling | Se ofrece al LLM una sola función, `registrar_actividad`, con esquema estricto, y se le obliga a llamarla (`tool_choice`). Si un modelo local no hace tool calling pero responde el mismo JSON como texto, también se acepta | ✅ Definido |
| Quién decide | El LLM solo propone. Qué es válido, qué falta y qué se pregunta lo decide el backend (`ReglasExtraccion`) | ✅ Definido |
| Texto | Obligatorio, máximo 200 caracteres. Con menos de 3 letras no se llama al LLM y se responde `NO_ENTENDIDO` | ✅ Definido |
| Hora | De hoy, en hora de Bolivia, entre 07:15 y 21:00. «En 20 minutos» se calcula en el servidor. Si no la dice, empieza **ahora** y se avisa en `supuestos`. Una hora que ya pasó (más de 5 min) se vuelve a preguntar | 🟡 Propuesta, a confirmar por Majo y Ariana |
| Duración | De 10 a 240 minutos, sin pasar de las 21:00 | 🟡 Propuesta, a confirmar por Majo y Ariana |
| Participantes | De 2 a 30 personas, contando a quien la crea (`ck_com_max` exige 2 o más). Si no lo dice y la actividad tiene un número habitual (cartas = 4), el modelo puede proponerlo | 🟡 Propuesta, a confirmar por Majo y Ariana |
| Ambigüedad | Un dato que falta o no es válido va en `faltantes`; `mensaje` pregunta **solo por el primero**. Para responder se manda la respuesta y lo ya entendido en `anterior`, sin reescribir todo (HU10, criterio 2) | ✅ Definido |
| Seguridad | Los mismos guardrails de HU17, antes del LLM (texto y `anterior`) y después (la actividad propuesta). Al LLM no se le envía el id, el nombre ni la ubicación del estudiante | ✅ Definido |
| Auditoría | Toda extracción queda en `log_auditoria_rag` con tipo `EXTRACCION_COMUNIDAD` y estado `OK`, `INCOMPLETO`, `SIN_RESULTADOS` (no entendida), `RECHAZADO_SEGURIDAD` o `ERROR`. `consultaId` es su `id`: HU10 lo guarda como `comunidad_efimera.log_id` | ✅ Definido |
| Sin clave de LLM | Se usa un extractor simulado con patrones (frases comunes), para probar sin costo | ✅ Definido |

### `POST /api/actividades/extraer`

Body: `{ "texto": "Quiero jugar cartas a las 4 por una hora", "anterior": null }`. Respuesta **200**:

```json
{
  "consultaId": 58,
  "estado": "COMPLETO",
  "mensaje": null,
  "actividad": { "actividad": "jugar cartas", "horaInicio": "16:00", "duracionMin": 60, "maxParticipantes": 4 },
  "faltantes": [],
  "supuestos": []
}
```

Si falta un dato (`INCOMPLETO`), `mensaje` trae la pregunta y el dato va en `null`:

```json
{
  "consultaId": 59,
  "estado": "INCOMPLETO",
  "mensaje": "¿Cuánto tiempo va a durar la actividad?",
  "actividad": { "actividad": "tomar un café", "horaInicio": "17:00", "duracionMin": null, "maxParticipantes": null },
  "faltantes": ["duracionMin", "maxParticipantes"],
  "supuestos": []
}
```

Para responder: `{ "texto": "una hora con 2 amigos", "anterior": { "actividad": "tomar un café", "horaInicio": "17:00", "duracionMin": null, "maxParticipantes": null }, "consultaAnteriorId": 57 }` (el `actividad` y el `consultaId` de la respuesta anterior, tal cual). `consultaAnteriorId` une los dos turnos en una sola consulta en la Auditoría RAG (`log_auditoria_rag.consulta_origen_id`, migración 020); solo se tiene en cuenta si la consulta anterior es tuya, de crear actividad y de hace menos de 30 minutos. `POST /api/actividades/revisar` acepta el mismo campo.

Si no se reconoce ninguna actividad (`NO_ENTENDIDO`), `mensaje` es «No entendimos tu actividad, ¿puedes describirla con más detalle?» y `faltantes` es `["actividad"]`.

**Preguntas** (`mensaje` con `INCOMPLETO`)

| Campo | Pregunta |
|---|---|
| `horaInicio` | «¿A qué hora quieres empezar?» · fuera de rango: «Las actividades en el campus van de 07:15 a 21:00. ¿A qué hora quieres empezar?» · pasada: «Esa hora ya pasó y las actividades son para hoy. ¿A qué hora quieres empezar?» |
| `duracionMin` | «¿Cuánto tiempo va a durar la actividad?» · fuera de rango: «La actividad puede durar entre 10 minutos y 4 horas. ¿Cuánto tiempo va a durar?» · pasa las 21:00: «Así terminaría después de las 21:00. ¿Cuánto tiempo va a durar?» |
| `maxParticipantes` | «¿Cuántas personas pueden participar, contándote a ti?» · fuera de rango: «El grupo puede ser de 2 a 30 personas, contándote a ti. ¿Cuántas personas pueden participar?» |

**Errores**

| Código | Caso |
|---|---|
| 400 | Falta el texto o supera los 200 caracteres (`VALIDACION`), o no pasa los guardrails (`SOLICITUD_NO_PERMITIDA`, con el mismo mensaje que HU17) |
| 401 | Sin token o token inválido |
| 429 | Más de 10 consultas en un minuto (`DEMASIADAS_SOLICITUDES`, HU19) |
| 503 | El LLM no respondió (`ASISTENTE_NO_DISPONIBLE`); queda registrado como `ERROR` |

### Corregir lo que la IA entendió mal

Desde el resumen, el estudiante puede corregir sin empezar de nuevo, de dos formas:

| Forma | Cómo | Estado |
|---|---|---|
| En palabras | `POST /api/actividades/extraer` con `texto` = la corrección («mejor a las 5 y somos 6») y `anterior` = el `actividad` del resumen. Cambia solo lo que dice la corrección | ✅ Definido |
| Editando cada dato | `POST /api/actividades/revisar` con los cuatro datos tal como los dejó. Las mismas reglas, **sin llamar al LLM**; queda en la auditoría con `modelo` = `manual` | ✅ Definido |

En los dos casos la respuesta trae un `consultaId` nuevo: **ese** es el que se manda a `POST /api/actividades` (HU10), que solo crea a partir de una extracción `COMPLETO`.

### `POST /api/actividades/revisar`

Body: `{ "texto": "Quiero jugar cartas por una hora", "actividad": { "actividad": "jugar ajedrez", "horaInicio": "17:00", "duracionMin": 90, "maxParticipantes": 2 } }` (`texto` es la descripción original). Respuesta **200**: el mismo formato que `POST /api/actividades/extraer` (`COMPLETO`, o `INCOMPLETO` con la pregunta por el dato que no es válido).

**Errores**

| Código | Caso |
|---|---|
| 400 | Falta la descripción o la actividad (`VALIDACION`: «Escribe qué actividad vas a hacer.»), o no pasa los guardrails (`SOLICITUD_NO_PERMITIDA`) |
| 401 | Sin token o token inválido |

## 7e. Crear la actividad (HU10 — responsable: Ariana; ajustes: Maite)

Cuando el estudiante revisa el resumen que dejó `POST /api/actividades/extraer` (estado `COMPLETO`) y pulsa **Confirmar y crear**, se llama a este endpoint. Si no confirma, no se llama y no se crea nada.

| Regla | Valor | Estado |
|---|---|---|
| Qué se crea | Una fila en `comunidad_efimera` con `creador_id` = el usuario del token, `log_id` = el `consultaId` de la extracción, `texto_original`, `actividad`, `inicio` y `fin` en UTC, `max_participantes` y `estado` = `ACTIVA` | ✅ Definido |
| Validación | Se vuelven a aplicar las reglas de la extracción (`ReglasExtraccion`): hora de hoy entre 07:15 y 21:00 en hora de Bolivia, duración de 10 a 240 minutos que no pase de las 21:00, y de 2 a 30 personas. Una hora que ya pasó hace más de 5 minutos se rechaza | ✅ Definido |
| La extracción | Tiene que ser del propio usuario (una ajena responde 404, igual que si no existiera), estar `COMPLETA` (`log_auditoria_rag.estado` = `OK`), **ser de hoy** (hora de Bolivia) y no haberse usado antes | ✅ Definido |
| Se crea el resumen confirmado | La extracción guarda su resumen validado en `log_auditoria_rag.datos_extraidos` (migración 013), también cuando se corrige a mano (`/api/actividades/revisar`). La actividad se crea **con ese resumen**: si lo que llega no coincide (otra actividad, hora, duración o cantidad), responde 409 `RESUMEN_DISTINTO` y no se crea nada. Las extracciones anteriores a la migración no tienen resumen y se validan como antes | ✅ Definido |
| Doble clic | Si dos pedidos llegan a la vez con la misma extracción, la base deja entrar uno (`uq_com_log`) y el otro responde 409 `DUPLICADO` | ✅ Definido |
| Última barrera en la base | `ck_com_duracion` (10 a 240 minutos) y `ck_com_horario` (07:15 a 21:00 en hora de Bolivia, mismo día) en `comunidad_efimera` | ✅ Definido |
| Guardrails | El texto y la actividad pasan por los guardrails de HU17 | ✅ Definido |
| Quién la invita | Esta historia solo crea la actividad. Avisar a compañeros compatibles y las confirmaciones son de HU13 y HU14 | 🟡 Propuesta, a confirmar por Amira |

### `POST /api/actividades`

Body (los datos son los del resumen de la extracción):

```json
{
  "texto": "quiero jugar cartas por una hora",
  "actividad": "jugar cartas",
  "horaInicio": "16:00",
  "duracionMin": 60,
  "maxParticipantes": 4,
  "consultaId": 58
}
```

Respuesta **201**:

```json
{ "id": 12, "actividad": "jugar cartas", "horaInicio": "16:00", "horaFin": "17:00", "duracionMin": 60, "maxParticipantes": 4, "estado": "ACTIVA" }
```

**Errores**

| Código | Caso |
|---|---|
| 400 | Datos que no cumplen las reglas, texto vacío o de más de 200 caracteres, consulta incompleta o de otro día (`VALIDACION`); o texto que no pasa los guardrails (`SOLICITUD_NO_PERMITIDA`) |
| 401 | Sin token o token inválido |
| 404 | La consulta no existe o es de otro estudiante (`NO_ENCONTRADO`) |
| 409 | Esa consulta ya se usó para crear una actividad (`DUPLICADO`, también en el doble clic) o los datos no son los del resumen confirmado (`RESUMEN_DISTINTO`) |

## 7f. Armado automático de la comunidad y notificación (HU13 — responsable: Maite)

Une HU10, HU12 y HU4: cuando `POST /api/actividades` (HU10) crea la actividad, el servidor busca **solo** a los compañeros
libres a esa hora, los invita y les avisa. Sin IA: es un cruce de horarios, como el matching.

### Reglas

| Regla | Valor | Estado |
|---|---|---|
| Cuándo | Automático, en la misma petición que crea la actividad. El resultado viene en `invitacion` de la respuesta de HU10 | ✅ Definido |
| Coincidencias | Compañeros de la misma sede activa que aceptaron el matching (`visible_matching`), con un puente **ese día** que comparta **30 min o más** con la actividad (si dura menos de 30, la actividad entera). Mismo cruce que HU12 | ✅ Definido |
| A quién se invita | A los más compatibles (más minutos en común; en empate, por id), **uno por lugar libre** (`maxParticipantes` menos quienes ya están) | 🟡 Propuesta (Maite) |
| Ocupados | No se invita a quien ya creó o confirmó otra comunidad activa que se cruza con ese horario, ni a quien ya está en esta | ✅ Definido |
| Reciprocidad | Sin campus → `SIN_ZONA`; sin aceptar el matching → `NO_VISIBLE` (quien no aparece tampoco invita). La actividad queda creada igual | ✅ Definido |
| Participaciones | Quien crea queda `CONFIRMADO` (con `respondido_en`); los invitados, `PENDIENTE` | ✅ Definido |
| Notificación | Un aviso `INVITACION_COMUNIDAD` por invitado, con `comunidadId`, en `GET /api/notificaciones`. Quien apagó los avisos (HU4) queda invitado pero sin aviso. Uno por comunidad (`uq_notif_comunidad`) | ✅ Definido |
| Atomicidad | Participaciones y avisos en una sola transacción. Si invitar falla, la actividad ya quedó creada: `invitacion.estado` = `ERROR` y se puede volver a buscar | ✅ Definido |

### En `POST /api/actividades` (HU10)

La respuesta 201 agrega `invitacion`:

```json
{
  "id": 12, "actividad": "jugar cartas", "horaInicio": "16:00", "horaFin": "17:00", "duracionMin": 60, "maxParticipantes": 4, "estado": "ACTIVA",
  "invitacion": {
    "estado": "INVITADOS",
    "mensaje": "Invitamos a 2 compañeros libres a esa hora.",
    "invitados": [
      { "nombre": "Diego Mamani", "carrera": "Ingeniería Industrial", "libreDesde": "16:00", "libreHasta": "17:00", "minutosEnComun": 60 },
      { "nombre": "Mateo Vargas", "carrera": "Arquitectura", "libreDesde": "16:00", "libreHasta": "16:30", "minutosEnComun": 30 }
    ]
  }
}
```

Cada invitado recibe: «Camila te invita a «jugar cartas» hoy de 16:00 a 17:00. Coincide con tu tiempo libre.»

| `invitacion.estado` | Cuándo | `mensaje` |
|---|---|---|
| `INVITADOS` | Se invitó a alguien | `Invitamos a N compañeros libres a esa hora.` |
| `SIN_COINCIDENCIAS` | Nadie libre a esa hora | `Por ahora nadie de tu campus está libre a esa hora. Puedes volver a buscar más tarde.` |
| `SIN_ZONA` | No eligió campus | `Elige tu campus para invitar a compañeros libres a esa hora.` |
| `NO_VISIBLE` | No aceptó el matching | `Para invitar a tus compañeros, primero acepta que ellos también te encuentren.` |
| `COMPLETA` | No quedan lugares | `El grupo ya está completo.` |
| `ERROR` | No se pudo invitar | `Tu actividad quedó creada, pero no pudimos invitar a tus compañeros. Vuelve a buscar en un momento.` |

### `POST /api/comunidades/{id}/invitaciones`

Vuelve a buscar con las mismas reglas (después de aceptar el matching, de elegir campus o si antes no había nadie). Responde
**200** con el mismo objeto que `invitacion`. Solo invita a los lugares que quedan y nunca dos veces a la misma persona.

**Errores**

| Código | Caso |
|---|---|
| 400 | La actividad ya terminó (`VALIDACION`) |
| 401 | Sin token o token inválido |
| 404 | La actividad no existe o no es tuya (`NO_ENCONTRADO`) |

## 7f-bis. Descubrir comunidades activas (adición a HU11 — responsable: Majo)

Comunidades → **Descubrir** muestra las actividades reales de hoy, incluida la que el estudiante acaba de crear. Unirse o responder una invitación (RSVP) es de HU14: ver la sección 7f-ter.

| Regla | Valor | Estado |
|---|---|---|
| Cuáles | `ACTIVA`, que todavía no terminaron y empiezan hoy (hasta la medianoche de Bolivia). De la más próxima a la más lejana, máximo 50 | ✅ Definido |
| Siempre visibles | Las que creó el estudiante y aquellas en las que está en `participacion` (lo invitaron) | ✅ Definido |
| De los demás | Solo si el estudiante eligió campus **y** aceptó el matching (HU12), y de creadores de su misma sede que también lo aceptaron: el mismo consentimiento recíproco que las invitaciones de HU13 | ✅ Definido |
| Qué se muestra del creador | Nombre e inicial del apellido («Ana P.»), o «Tú». Sin ids ni correos | ✅ Definido |
| `confirmados` | Personas `CONFIRMADO`, contando siempre a quien la creó | ✅ Definido |

### `GET /api/comunidades`

Respuesta **200**:

```json
{
  "estado": "OK",
  "mensaje": null,
  "comunidades": [
    { "id": 12, "actividad": "jugar cartas", "horaInicio": "16:00", "horaFin": "17:00", "maxParticipantes": 4,
      "confirmados": 1, "creador": "Ana P.", "esMia": false, "miEstado": "PENDIENTE" }
  ]
}
```

| `estado` | Cuándo | `mensaje` |
|---|---|---|
| `OK` | Hay comunidades | `null` |
| `SIN_RESULTADOS` | Campus y matching aceptado, pero hoy no hay ninguna | «Hoy todavía no hay comunidades activas en tu campus. ¡Crea la primera!» |
| `SIN_ZONA` | No eligió campus (igual ve las suyas y aquellas a las que lo invitaron) | «Elige tu campus para ver las comunidades de tus compañeros.» |
| `NO_VISIBLE` | No aceptó el matching (igual ve las suyas y aquellas a las que lo invitaron) | «Para ver las comunidades de tus compañeros, primero acepta que ellos también te encuentren.» |

`miEstado`: `PENDIENTE`, `CONFIRMADO` o `RECHAZADO` si al estudiante lo invitaron o se unió (HU14); `null` si no está en ella o es suya.

**Errores**

| Código | Caso |
|---|---|
| 401 | Sin token o token inválido |

## 7f-ter. Confirmar o rechazar la asistencia (RSVP) (HU14 — responsable: Maite)

Cierra el ciclo de HU13: el invitado (`PENDIENTE`) dice si va o no, y quien creó la actividad ve con claridad quiénes van.
También permite **unirse** desde Descubrir a una comunidad de un compañero sin haber sido invitado.

### Reglas

| Regla | Valor | Estado |
|---|---|---|
| Quién responde | Quien fue invitado (`PENDIENTE`), quien ya respondió (puede cambiar de idea) y quien la ve en Descubrir (mismo campus y matching aceptado por los dos: el mismo consentimiento recíproco que HU13). Una comunidad que el estudiante no puede ver responde 404, igual que si no existiera | ✅ Definido |
| Quien la creó | Siempre va (`CONFIRMADO`); no responde (400) | ✅ Definido |
| Cambiar de idea | `PENDIENTE` → `CONFIRMADO` o `RECHAZADO`; `CONFIRMADO` → `RECHAZADO` (se baja); `RECHAZADO` → `CONFIRMADO` (si queda lugar). Cada cambio actualiza `respondido_en`. Responder lo mismo otra vez no escribe nada | ✅ Definido |
| Cupo | No más confirmados que `max_participantes`, contando a quien la creó. Si dos confirman a la vez el último lugar, la comunidad se bloquea (`SELECT … FOR UPDATE`) y entra solo uno; el otro recibe 409 `COMPLETA` | ✅ Definido |
| Cruce | No se confirma si a esa hora el estudiante ya creó o confirmó otra comunidad activa (409 `CRUCE`, dice cuál y a qué hora). Bajarse siempre se puede | ✅ Definido |
| Hasta cuándo | Mientras la actividad no haya terminado (también si ya empezó) | ✅ Definido |
| Avisos | Al responder, sus avisos de esa comunidad quedan leídos. Cuando el grupo se llena, los invitados que siguen `PENDIENTE` (y tienen los avisos encendidos) reciben `COMUNIDAD_CERRADA`: ««jugar cartas» ya se llenó. Si alguien se baja, todavía puedes confirmar desde Comunidades.» Si alguien se baja y se libera un lugar, ese aviso se apaga | ✅ Definido |
| Privacidad de la lista | Quien la creó ve a todos (confirmados, pendientes y los que no van) con nombre completo y carrera. Un invitado o participante ve solo a quienes van, con nombre e inicial del apellido, y a sí mismo («Tú»). Los demás, 404 | ✅ Definido |
| Base de datos | `participacion` ya tenía los tres estados y `ck_part_respuesta` (`respondido_en` solo cuando no está `PENDIENTE`). Migración 015: índice `ix_part_usuario_estado` para revisar el cruce | ✅ Definido |

### `PUT /api/comunidades/{id}/asistencia`

Body: `{ "asisto": true }` (voy) o `{ "asisto": false }` (no voy). Respuesta **200**:

```json
{ "comunidadId": 12, "miEstado": "CONFIRMADO", "confirmados": 3, "maxParticipantes": 4,
  "mensaje": "¡Listo! Te esperamos en «jugar cartas» hoy de 16:00 a 17:00." }
```

Con `asisto: false`, `miEstado` = `RECHAZADO` y `mensaje` = «Listo, quedó registrado que no vas.».

**Errores**

| Código | Caso |
|---|---|
| 400 | Falta `asisto` («Indica si vas o no.»), eres quien la creó, la actividad ya terminó o rechazas una comunidad en la que no estás (`VALIDACION`) |
| 401 | Sin token o token inválido |
| 404 | La comunidad no existe, está archivada o no la puedes ver (`NO_ENCONTRADO`) |
| 409 | No quedan lugares (`COMPLETA`) o a esa hora ya vas a otra comunidad (`CRUCE`: «A esa hora ya vas a «estudiar Redes» (10:30–11:30).») |

### `GET /api/comunidades/{id}/participantes`

Respuesta **200** (vista de quien la creó):

```json
{
  "comunidadId": 12, "actividad": "jugar cartas", "horaInicio": "16:00", "horaFin": "17:00",
  "confirmados": 3, "maxParticipantes": 4, "esMia": true,
  "participantes": [
    { "nombre": "Tú", "carrera": null, "estado": "CONFIRMADO", "esCreador": true, "esTu": true },
    { "nombre": "Lucía Rojas", "carrera": "Derecho", "estado": "CONFIRMADO", "esCreador": false, "esTu": false },
    { "nombre": "Mateo Vargas", "carrera": "Arquitectura", "estado": "PENDIENTE", "esCreador": false, "esTu": false },
    { "nombre": "Diego Mamani", "carrera": "Ingeniería Industrial", "estado": "RECHAZADO", "esCreador": false, "esTu": false }
  ]
}
```

Orden: quien la creó, después los que van, los pendientes y los que no van (cada grupo por orden de respuesta).

**Errores**

| Código | Caso |
|---|---|
| 401 | Sin token o token inválido |
| 404 | La comunidad no existe, está archivada o no estás en ella (`NO_ENCONTRADO`) |

## 7f-quater. Cierre automático de la comunidad al vencer el bloque (HU15 — Amira; aviso en vivo: Ariana; avisos: Maite)

No tiene endpoint: lo hace el servidor solo (`CierreComunidadesWorker`, cada 15 segundos).

| Regla | Valor | Estado |
|---|---|---|
| Cuándo se cierra | Cuando llega la hora de fin (`comunidad_efimera.fin <= ahora`, en UTC): `estado` pasa de `ACTIVA` a `ARCHIVADA` (`ck_com_estado`, índice `ix_comunidad_estado_fin`) | ✅ Definido |
| Qué deja de funcionar | Desaparece de `GET /api/comunidades`; asistencia, participantes e invitaciones responden 404 o 400 «La actividad ya terminó.» | ✅ Definido |
| Aviso en vivo | SignalR `/hubs/comunidades`, evento `comunidadActualizada` `{ comunidadId, estado: "ARCHIVADA", confirmados: 0 }` a quienes la siguen (Descubrir la quita al momento) | ✅ Definido |
| Avisos de la campanita | Los de esa comunidad (`INVITACION_COMUNIDAD`, `COMUNIDAD_CERRADA`) quedan `activa = FALSE`: ya no aparecen en `GET /api/notificaciones` ni cuentan como no leídos. Al arrancar, el servidor apaga también los que quedaron de comunidades archivadas antes | ✅ Definido |
| Historial | La comunidad y sus participaciones no se borran (quedan para métricas y auditoría) | ✅ Definido |

## 7f-quinquies. Actualización en tiempo real (HU18 — Rocío y Ariana; ampliación: Maite)

SignalR en **`/hubs/comunidades`**, con el mismo JWT (`?access_token=` en la conexión). Los eventos son solo señales: el
cliente vuelve a pedir lo suyo a la API, que aplica los permisos de siempre.

| Método del cliente | Para qué |
|---|---|
| `Suscribirse(comunidadId)` / `Desuscribirse(comunidadId)` | Seguir una comunidad que puedes ver (si no, `HubException`) |
| `SeguirDescubrir()` | Recibir las comunidades nuevas de tu sede (solo con sede activa y matching aceptado) |

| Evento del servidor | A quién | Cuándo |
|---|---|---|
| `comunidadActualizada` `{ comunidadId, estado, confirmados }` | Quienes siguen esa comunidad | Alguien responde «Voy»/«No voy» (`ACTIVA` con el cupo nuevo) o se archiva (`ARCHIVADA`) |
| `comunidadesCambiaron` (sin datos) | Quienes miran Descubrir en la sede de quien la creó | Se crea una comunidad (`POST /api/actividades`) |
| `avisosCambiaron` (sin datos) | El estudiante | Le llega una invitación (al crear o con «Buscar de nuevo»), el grupo se llena o se reabre, la comunidad termina o empieza un puente |

Si el aviso en vivo falla, la operación sigue igual y la campanita lo ve en su consulta de cada 15 segundos. El cliente
reintenta la conexión sin rendirse y, al reconectar, vuelve a pedir todo (lo que pasó mientras estuvo desconectado).

## 7g. Protección contra prompt injection y solicitudes no permitidas (HU17 — Amira y Ariana; ajustes: Maite)

Todo texto que llega al asistente o sale de él pasa por los guardrails (`ReglasSeguridadIa`). Lo que no pasa se rechaza
**sin llamar al LLM** (no gasta tokens) y queda en la auditoría con su motivo.

| Regla | Valor | Estado |
|---|---|---|
| Qué se revisa | La entrada del estudiante en recomendaciones (HU7), extracción (HU11) y creación de actividades (HU10); la **respuesta de la IA** en la extracción (la actividad propuesta) y en las recomendaciones (cada explicación) | ✅ Definido |
| Normalización | Minúsculas, sin tildes, letras separadas unidas («i g n o r a») y números que reemplazan letras («1gn0r4») antes de comparar | ✅ Definido |
| Respuesta | 400 `SOLICITUD_NO_PERMITIDA` con el mensaje de la categoría; si la respuesta de la IA no pasa, se descarta y se responde `SIN_RESULTADOS` | ✅ Definido |
| Auditoría | `log_auditoria_rag.estado` = `RECHAZADO_SEGURIDAD` y `motivo_rechazo` con la categoría (migración 012) | ✅ Definido |
| Foto del horario | Lo que la IA lee en la imagen no son instrucciones: se descarta la clase cuya materia o aula intenta manipular al asistente (`INYECCION` o `FUERA_DE_ALCANCE`). El filtro de reglamento no aplica (una materia real puede llamarse «Educación Sexual») | ✅ Definido |
| Delimitación | La preferencia va en `<preferencia>…</preferencia>` y se le quitan `<` y `>` para que no pueda cerrar el bloque | ✅ Definido |

| `motivo_rechazo` | Cuándo | Mensaje al estudiante |
|---|---|---|
| `INYECCION` | Intenta cambiar las reglas, ver el prompt o cambiar de rol («ignora tus instrucciones», «modo desarrollador», «DAN») | «La solicitud intenta modificar las instrucciones del asistente.» |
| `FUERA_DE_ALCANCE` | Pide datos internos: contraseñas, tokens, API keys, código fuente, datos personales | «Solo puedo recomendar actividades y lugares del campus para tu puente.» |
| `BIENESTAR` | Menciona autolesión | Deriva al área de bienestar universitario |
| `FUERA_DE_REGLAMENTO` | Alcohol, drogas, apuestas, contenido sexual, ocultismo, violencia, deshonestidad académica, groserías | «Solo puedo recomendar actividades académicas y respetuosas, dentro del reglamento de la universidad.» |
| `FUERA_DE_CONTEXTO` | La IA respondió con lugares que no se recuperaron, cifras, enlaces o una explicación que no pasa los guardrails | (no se le muestra nada: `SIN_RESULTADOS`) |

**Panel admin:** `GET /api/admin/auditoria-rag` trae `motivoRechazo` en cada consulta y `GET /api/admin/metricas` trae
`rechazosPorMotivo` (últimos 7 días).

## 7h. Rendimiento y disponibilidad (HU19 — Rocío y Ariana; ampliación: Maite)

| Regla | Valor | Estado |
|---|---|---|
| Objetivo | `POST /api/recomendaciones` responde en **menos de 3,5 s** | ✅ Definido |
| Presupuesto de la IA | 2,8 s (`ReglasRendimiento.PresupuestoIaMs`); el resto es para recuperar, validar y guardar | ✅ Definido |
| Plan B | Si la IA no contesta en ese tiempo o falla, se recomiendan hasta 3 lugares de los ya recuperados (HU6): primero los que entran con ida, actividad y vuelta en el puente, después los más cercanos. Responde `200` `OK` con `mensaje` = «El asistente estaba muy solicitado, así que te mostramos las opciones más cercanas para tu puente.» (con la IA, `mensaje` es `null`). Queda en la auditoría como `OK` con `modelo` = `respaldo-sin-ia` | ✅ Definido |
| `latencia_ms` | En las recomendaciones, el tiempo total que espera el estudiante (recuperar + IA), no solo la IA | ✅ Definido |
| Razonamiento | Con OpenAI (`api.openai.com`) se manda `reasoning_effort: "none"`, como en la extracción (HU11) | ✅ Definido |
| Límite en horas pico | 10 consultas por minuto por estudiante entre `POST /api/recomendaciones`, `/api/actividades/extraer`, `/api/actividades/revisar` y `/api/horario/foto` (cada una cuenta por separado; guardar el horario no tiene límite). Al pasarse: **429** `{ "codigo": "DEMASIADAS_SOLICITUDES", "mensaje": "Hiciste muchas consultas seguidas. Espera un minuto y vuelve a intentarlo." }` con `Retry-After` | ✅ Definido |
| Observabilidad | `/health/ready` (base en 2 s) y `/health/metrics` (Prometheus, en memoria) de Rocío y Ariana; `GET /api/admin/metricas` → `rendimiento` `{ consultas, objetivoMs, p50Ms, p95Ms, dentroDelObjetivoPct, respaldos }` desde la auditoría (no se pierde al reiniciar) | ✅ Definido |

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
| Majo | Tramo antes de la primera clase | No es puente | 🟡 Propuesta, a confirmar por Majo |
| Majo, Ariana | Parámetro de 45 min | Un único lugar de configuración del backend | 🟡 Propuesta, a confirmar por Majo y Ariana |
| Majo, Maite | Recálculo de puentes | Síncrono, dentro de `POST`/`PUT`/`DELETE` de horarios; reemplaza los puentes del usuario | 🟡 Propuesta, a confirmar por Majo y Maite |
| Majo | Campo de duración en `GET /api/puentes` | No se incluye | 🟡 Propuesta, a confirmar por Majo |
| Majo | Texto de errores de puentes | — | 🔴 Pendiente (Majo) |
| Ariana | Origen CORS del FE | `http://localhost:3000` | 🟡 Propuesta, a confirmar por Ariana |
| Ariana | Usuario de prueba | `docs/database/seed-dev.sql` (solo desarrollo; ver README) | ✅ Definido (en `main`, PR #144) |
| Amira | Token de desarrollo | — | 🔴 Pendiente (depende del JWT de HU1, Amira) |

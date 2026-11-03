# BREAKU

Proyecto de la materia SIS-227 (UCB). Plataforma web que detecta "puentes" (huecos libres de 45 minutos o más entre clases), recomienda lugares verificados del campus y forma comunidades efímeras.

## Stack

- Backend: C# con ASP.NET Core (.NET 10)
- Acceso a datos: EF Core
- Autenticación: JWT
- Base de datos: MySQL 8.4 LTS (propuesta, por confirmar)
- Frontend: Next.js 16 + React 19 con TypeScript
- Asistente de IA (HU7): cualquier LLM con API compatible con OpenAI; para desarrollo, [Ollama](https://ollama.com) en local (gratis). Ver [Asistente de IA](#asistente-de-ia-hu7-opcional).
- Planificado para el sprint 2: SignalR

## Estructura del repositorio

- `backend/` — proyecto ASP.NET Core único (`Breaku.csproj`, solución `Breaku.slnx`) con las capas como carpetas: `Presentation/`, `Application/`, `Domain/`, `Infrastructure/`
- `frontend/` — aplicación Next.js (TypeScript, App Router, `src/`)
- `docs/` — documentación
  - `docs/api/` — especificación OpenAPI de la API (`openapi.yaml`)
  - `docs/api-contract.md` — borrador del contrato de API del sprint 1 (la referencia vigente es `openapi.yaml`)
  - `docs/database/` — esquema (`schema.sql`), migraciones (`migraciones/`) y datos de prueba (`seed-dev.sql`, `seed-dev-catalogo.sql`, `seed-dev-cochabamba.sql`, `seed-dev-matching.sql`)

Archivos en la raíz: `README.md`, `.gitignore`.

## Arquitectura

- Cliente-servidor de 3 niveles.
- Backend: monolito modular en capas con inversión de dependencias, en un solo proyecto con las capas como carpetas (`Presentation`, `Application`, `Domain`, `Infrastructure`).
  - `Domain` no depende de nadie; `Application` solo de `Domain`; `Infrastructure` implementa las interfaces de `Application`; `Presentation` llama a `Application` (por convención).
  - Solo `Infrastructure` accede a la base de datos y a servicios externos.
  - El servidor de Next.js no accede a la base de datos; todo pasa por la API.

## Base de datos

Esquema completo (14 tablas): [`docs/database/schema.sql`](docs/database/schema.sql)

## Equipo

- Ariana Aylen Pita Vargas
- Maria Jose Sandoval Orellana
- Gabriela Maite Arauco Porrez
- Amira del Rocio Choque Carrasco

## Cómo ejecutarlo

### Backend

Necesitas el SDK de .NET 10 (lo probamos con la 10.0.401).

```bash
cd backend
dotnet run
```

`dotnet run` compila y usa el primer perfil de `Properties/launchSettings.json` (`http`, entorno `Development`). Desde la raíz del repositorio, usa `dotnet run --project backend`. Para detenerla: **Ctrl + C**. Al cambiar `backend/.env` hay que reiniciarla, porque se lee solo al arrancar.

La API queda en `http://localhost:5092`. Para ver si está corriendo:

```bash
curl -i http://localhost:5092/health
```

Tiene que responder `200 OK`.

### Documentación de la API (Swagger / OpenAPI)

La API está documentada con **OpenAPI 3** y se ve con **Swagger UI** (paquete Swashbuckle.AspNetCore). Con el backend corriendo:

| Qué | URL |
|---|---|
| Swagger UI (interactivo, con botón **Authorize**) | `http://localhost:5092/swagger` |
| Documento OpenAPI en JSON | `http://localhost:5092/swagger/v1/swagger.json` |
| Copia estática en YAML (para mirarla sin levantar el backend) | [`docs/api/openapi.yaml`](docs/api/openapi.yaml) |

Para probar los endpoints protegidos (`/api/horario`, `/api/puentes`, `/api/notificaciones`, `/api/recomendaciones` y, con el administrador de prueba, `/api/admin/*`):

1. En Swagger UI, abre `POST /api/auth/login`, pulsa **Try it out** y envía el usuario de prueba (mira más abajo) o uno que hayas registrado.
2. Copia el valor de `token` de la respuesta.
3. Pulsa **Authorize**, pega el token (sin escribir `Bearer`) y confirma. El token se queda guardado aunque recargues la página.

Swagger solo aparece en `Development` (el perfil `http` de `launchSettings.json` ya lo es). Para activarlo en otro entorno, pon `SWAGGER_ENABLED=true` en `backend/.env`.

La documentación sale de los comentarios `///` de los controladores y DTOs (el proyecto genera `Breaku.xml` al compilar). **Cuando agregues o cambies un endpoint**, actualiza sus comentarios y `docs/api/openapi.yaml` en el mismo PR. Para regenerar el YAML con la API corriendo, exporta `/swagger/v1/swagger.json`.

### Base de datos

Necesitas una instancia de MySQL con el esquema de [`docs/database/schema.sql`](docs/database/schema.sql) ya cargado (EF Core solo mapea; no hay migraciones de EF).

> **Ojo al actualizar:** si tu base la creaste con una versión vieja de `schema.sql`, ejecuta en orden los scripts de [`docs/database/migraciones/`](docs/database/migraciones/) que todavía no corriste (por ejemplo, `001-hu4-notificaciones.sql` para HU4, `002-hu5-ubicacion.sql` para HU5, `003-hu21-perfil.sql` para HU21, `004-horario-asistencia.sql` para las clases que coinciden y `005-sede-cochabamba.sql` para la sede activa y `006-hu12-matching.sql` para el matching de compañeros `007-hu9-feedback.sql` para la valoración de recomendaciones y `008-hu2-rango-horario.sql` para el horario de clases de 07:15 a 21:00), con tu base seleccionada, igual que `seed-dev.sql`. Una base nueva creada con `schema.sql` no los necesita. La versión de MySQL que usa el código es la 8.0.46, fija en `backend/Infrastructure/DependencyInjection.cs`. Falta confirmar la versión del equipo.

La conexión se configura en `backend/.env` (no se sube al repositorio):

```bash
cp backend/.env.example backend/.env
```

Completa `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER` y `DB_PASSWORD`, más `FRONTEND_ORIGIN` (el origen del frontend que permite CORS; ejemplo: `http://localhost:3000`) y `JWT_SECRET` (una clave de 32 caracteres o más para firmar los tokens). No hay valores por defecto: si falta alguna, la API no arranca y te dice cuál falta. El archivo `.env` se busca en la carpeta del proyecto (`backend/`), así que puedes ejecutar desde cualquier carpeta.

> **Ojo al actualizar:** cada uno tiene que agregar `FRONTEND_ORIGIN` y `JWT_SECRET` a su `backend/.env` (mira `backend/.env.example`); sin esas claves la API no arranca.

La ubicación de HU5 utiliza el catálogo `zona` de `docs/database/schema.sql`. Antes de usar la selección de campus, el catálogo tiene que incluir las zonas oficiales con centro (`latitud`, `longitud`) y radio (`radio_max_m`) configurados por el equipo administrador. La plataforma no incluye coordenadas de campus inventadas. Una base que ya tenías tiene que aplicar `docs/database/migraciones/002-hu5-ubicacion.sql`; una base nueva ya incluye estos cambios en `schema.sql`.

Para ver si conecta a MySQL, con la API corriendo:

```bash
curl -i http://localhost:5092/health/db
```

Responde `200 OK` si conecta y `503 Service Unavailable` si no.

#### Usuario de prueba (solo desarrollo)

> **Ojo:** solo para desarrollo local, nunca para producción. La contraseña es pública en este repositorio.

| Dato | Valor |
|---|---|
| Rol | Correo | Contraseña |
|---|---|---|
| Estudiante | `dev.estudiante@ucb.edu.bo` | `Breaku-Dev-2026!` |
| Administrador | `dev.admin@ucb.edu.bo` | `Breaku-Admin-2026!` |

El script [`docs/database/seed-dev.sql`](docs/database/seed-dev.sql) inserta esos dos usuarios (roles `ESTUDIANTE` y `ADMIN`, correo verificado, activos). El administrador entra al panel de administración del campus. Es idempotente: se puede ejecutar varias veces. **No incluye `USE`**: hay que ejecutarlo con tu base de datos ya seleccionada (el nombre es el de tu `DB_NAME`).

**Opción 1: MySQL Workbench.** Abre `docs/database/seed-dev.sql`, deja tu base de datos como esquema por defecto (clic derecho sobre ella en el panel de esquemas → *Set as Default Schema*) y ejecuta el script.

**Opción 2: línea de comandos**, pasando el nombre de la base como parámetro:

```bash
mysql -u <usuario> -p <nombre_de_la_base> < docs/database/seed-dev.sql
```

En Windows, `mysql` puede no estar en el `PATH`; en ese caso usa la ruta completa a `mysql.exe` (carpeta `bin` de tu instalación de MySQL) o la opción 1.

Token de desarrollo: se obtiene con `POST /api/auth/login` usando ese usuario (campo `token` de la respuesta) y dura 24 horas. En Swagger, pegalo en **Authorize** (ver [Documentación de la API](#documentación-de-la-api-swagger--openapi)).

#### Catálogo de prueba (solo desarrollo)

[`docs/database/seed-dev-catalogo.sql`](docs/database/seed-dev-catalogo.sql) carga una zona (campus), cuatro categorías y siete lugares de ejemplo para probar las recomendaciones (`POST /api/recomendaciones`, HU7). Se ejecuta igual que `seed-dev.sql` y **después** de él, porque usa al usuario de prueba como autor. Es idempotente. Dos lugares están ahí para comprobar los filtros y nunca deben recomendarse: uno queda fuera del radio y otro está `EN_REVISION`.

Para probarlo: elige tu campus (en la app, `/configuracion/ubicacion`, o con `PUT /api/ubicacion/zona`), registra un horario con un hueco de 45 minutos o más y toma el `id` del puente en `GET /api/puentes`. Con ese `id` puedes ver los lugares viables en `GET /api/lugares/viables?puenteId=<id>` (HU6) o pedir recomendaciones enviando `{ "puenteId": <id> }` a `POST /api/recomendaciones` (HU7).

**Lugares viables (HU6).** Un lugar solo se muestra si está `ACTIVO` (aprobado por el administrador), queda dentro del radio de tu campus y el traslado de ida y vuelta (caminando, unos 80 m por minuto) más la actividad cabe en tu puente. Sin campus elegido la búsqueda responde `SIN_ZONA`; si ningún lugar cumple, responde `SIN_RESULTADOS` con el mensaje «No encontramos opciones cercanas para este tiempo disponible.». La misma búsqueda es la que usa el asistente de HU7.

#### Sede UCB Cochabamba (solo desarrollo)

[`docs/database/seed-dev-cochabamba.sql`](docs/database/seed-dev-cochabamba.sql) carga la sede **Campus UCB Cochabamba** con los 13 lugares del relevamiento (`docs/datos/lugares_ucb_cochabamba.json`). Se ejecuta igual que los otros seeds y **después** de `seed-dev.sql`. Es idempotente y, al correrlo, deja las demás sedes (La Paz) guardadas pero **inactivas**: no aparecen para elegir ni reciben recomendaciones. Para volver a La Paz: `UPDATE zona SET activa = (nombre = 'Campus UCB La Paz');`. La sede activa se decide con el campo `zona.activa`; no hace falta ninguna variable de entorno.

- Los 13 lugares quedan con `validado = FALSE`: son datos del relevamiento que el equipo todavía tiene que revisar. La app los muestra con la etiqueta «Por validar», y el panel de administración también.
- Cinco son puntos de referencia (campus, parqueo, bloques A, B y C) con `recomendable = FALSE`: quedan guardados, pero nunca se recomiendan, así que su categoría no se ve.
- Las categorías del relevamiento se asignan a las de la app así: `comer` y `cafe_te` → Comida, `estudio` → Estudio, `descanso` → Descanso, `deporte_aire_libre` → Deporte y `campus` → Estudio (los puntos de referencia). No se crea ninguna categoría nueva.
- Si cambias el JSON, regenera el seed con `python docs/datos/generar_seed_cochabamba.py`.

#### Compañeros para el matching (HU12, solo desarrollo)

[`docs/database/seed-dev-matching.sql`](docs/database/seed-dev-matching.sql) crea cinco estudiantes de prueba en la sede Cochabamba, con horario y puentes, para probar el matching de compañeros con puente compatible. Se ejecuta **después** de `seed-dev.sql`, `seed-dev-cochabamba.sql` y la migración `006-hu12-matching.sql`. Es idempotente. Usan la misma contraseña que el estudiante de prueba.

- Ana, Diego, Lucía y Mateo aceptaron aparecer en el matching. Sofía **no**: tiene los mismos puentes que Ana, pero nunca debe aparecer.
- Para verlos: entra como el estudiante de prueba, elige la sede Cochabamba, activa «Dejar que compañeros con tiempo compatible me encuentren» y registra un horario con un puente, por ejemplo el lunes de 09:30 a 11:00.

### Asistente de IA (HU7, opcional)

Las recomendaciones de `POST /api/recomendaciones` las redacta un LLM, que solo puede elegir entre los lugares recuperados del catálogo. **Es opcional:** sin configurarlo, la API arranca igual y usa un asistente simulado, sin LLM, que también recomienda solo lugares del catálogo (con explicaciones genéricas).

#### Con Ollama (recomendado para desarrollo)

Gratis, sin límite de consultas y sin enviar datos fuera de tu computadora.

1. Instala Ollama desde [ollama.com/download](https://ollama.com/download) o con `winget install Ollama.Ollama`. Queda corriendo en segundo plano en `http://localhost:11434` y arranca solo al prender la PC.
2. Descarga el modelo (unos 2 GB, una sola vez):

   ```bash
   ollama pull qwen2.5:3b
   ```

   Para comprobar que responde: `ollama run qwen2.5:3b "hola"`. Alternativa si recomienda poco: `llama3.2:3b`.
3. Agrega a `backend/.env`:

   ```bash
   LLM_API_KEY=ollama
   LLM_BASE_URL=http://localhost:11434/v1/
   LLM_MODEL=qwen2.5:3b
   LLM_TIMEOUT_SECONDS=90
   ```

   Ollama no usa clave, pero `LLM_API_KEY` es lo que activa el LLM: basta cualquier texto.
4. Reinicia el backend. En `log_auditoria_rag.modelo` debería aparecer `qwen2.5:3b` en vez de `simulado`.

Sin tarjeta gráfica dedicada el modelo corre en el procesador: cada recomendación tarda entre 10 y 40 segundos, y la primera más, porque carga el modelo en memoria. Por eso `LLM_TIMEOUT_SECONDS=90`.

Para cerrar Ollama: ícono de la llama junto al reloj → **Quit Ollama**, o `Get-Process ollama* | Stop-Process` en PowerShell. Con Ollama cerrado (y el LLM configurado), `POST /api/recomendaciones` responde `503` con `codigo` = `ASISTENTE_NO_DISPONIBLE`.

#### Variables y otros proveedores

| Variable | Obligatoria | Qué es |
|---|---|---|
| `LLM_API_KEY` | No | Activa el LLM. Sin ella se usa el asistente simulado |
| `LLM_BASE_URL` | Si hay `LLM_API_KEY` | URL base de la API del proveedor |
| `LLM_MODEL` | Si hay `LLM_API_KEY` | Nombre del modelo en ese proveedor |
| `LLM_MODEL_IMAGEN` | No (por defecto, `LLM_MODEL`) | Modelo con visión para leer la foto del horario (`POST /api/horario/foto`). Sin `LLM_API_KEY`, la foto no está disponible y el registro manual sigue igual |
| `LLM_TIMEOUT_SECONDS` | No (por defecto 20) | Segundos de espera al LLM antes de responder `503` |

Sirve cualquier proveedor con API compatible con OpenAI (Chat Completions) que soporte salida JSON con esquema:

| Proveedor | `LLM_BASE_URL` | Ejemplo de `LLM_MODEL` | Costo |
|---|---|---|---|
| Ollama | `http://localhost:11434/v1/` | `qwen2.5:3b` | Gratis |
| Google Gemini (clave en [aistudio.google.com](https://aistudio.google.com)) | `https://generativelanguage.googleapis.com/v1beta/openai/` | `gemini-2.5-flash` | Gratis, con límite de consultas |
| OpenAI (configuración del equipo) | `https://api.openai.com/v1/` | `gpt-6-luna` | Pago |

**El equipo usa OpenAI con el modelo de texto `gpt-6-luna` y el de imagen `gpt-6.1-terra`** (ver `backend/.env.example`). La clave se comparte por un medio privado y debe tener permiso de uso de modelos (*Model capabilities: Write*); si OpenAI responde `missing_scope`, a la clave le falta ese permiso. Las claves de Gemini u OpenAI van solo en `backend/.env`, nunca en el repositorio. Al LLM se le envían únicamente los lugares recuperados y la preferencia del estudiante, nunca su ubicación ni su identidad.

### Frontend

Necesitas Node.js 20.9 o superior y npm (lo probamos con Node 24.13.0 y npm 11.6.2).

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

El frontend queda en `http://localhost:3000`. `NEXT_PUBLIC_API_URL` (en `frontend/.env.local`, no se sube al repositorio) es la URL base del backend; ejemplo: `http://localhost:5092`. Para que el navegador pueda llamar a la API, el backend tiene que tener `FRONTEND_ORIGIN=http://localhost:3000` en su `.env`.

Build de producción: `npm run build`. Linter: `npm run lint`.

### Pruebas

El backend tiene pruebas unitarias (xUnit) en [`backend/Breaku.Tests`](backend/Breaku.Tests). No usan red, ni base de datos, ni el asistente de IA, así que **no gastan crédito de OpenAI**:

```bash
cd backend/Breaku.Tests
dotnet test
```

Cubren la unión de intervalos, la detección de choques y los huecos libres con clases que coinciden (una sola clase, alternar por día y ambas marcadas).

Para probar la app completa sin gastar crédito, deja `LLM_API_KEY` sin definir en `backend/.env`: se usa el asistente simulado y la foto del horario responde «no disponible» sin llamar a ningún proveedor.

Estilos con CSS Modules (`*.module.css` junto a cada componente) y variables de color y tipografía en `src/app/globals.css`. No se usan librerías de UI ni de estado.

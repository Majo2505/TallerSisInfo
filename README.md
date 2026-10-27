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
  - `docs/database/` — esquema (`schema.sql`), migraciones (`migraciones/`) y datos de prueba (`seed-dev.sql`, `seed-dev-catalogo.sql`)

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

Requisito: SDK de .NET 10 (probado con 10.0.401).

```bash
cd backend
dotnet run
```

`dotnet run` compila y usa el primer perfil de `Properties/launchSettings.json` (`http`, entorno `Development`). Desde la raíz del repositorio, usá `dotnet run --project backend`. Para detenerla: **Ctrl + C**. Al cambiar `backend/.env` hay que reiniciarla, porque se lee solo al arrancar.

La API queda en `http://localhost:5092`. Para comprobar que corre:

```bash
curl -i http://localhost:5092/health
```

Debe responder `200 OK`.

### Documentación de la API (Swagger / OpenAPI)

La API se documenta con **OpenAPI 3** y se muestra con **Swagger UI** (paquete Swashbuckle.AspNetCore). Con el backend en marcha:

| Qué | URL |
|---|---|
| Swagger UI (interactivo, con botón **Authorize**) | `http://localhost:5092/swagger` |
| Documento OpenAPI en JSON | `http://localhost:5092/swagger/v1/swagger.json` |
| Copia estática en YAML (para revisar sin correr el backend) | [`docs/api/openapi.yaml`](docs/api/openapi.yaml) |

Para probar los endpoints protegidos (`/api/horario`, `/api/puentes`, `/api/notificaciones`, `/api/recomendaciones`):

1. En Swagger UI, abrí `POST /api/auth/login`, pulsá **Try it out** y enviá el usuario de prueba (ver más abajo) o uno que hayas registrado.
2. Copiá el valor de `token` de la respuesta.
3. Pulsá **Authorize**, pegá el token (sin escribir `Bearer`) y confirmá. El token se conserva al recargar la página.

Swagger solo se publica en `Development` (el perfil `http` de `launchSettings.json` ya lo es). Para habilitarlo en otro entorno, definí `SWAGGER_ENABLED=true` en `backend/.env`.

La documentación sale de los comentarios `///` de los controladores y DTOs (el proyecto genera `Breaku.xml` al compilar). **Al agregar o cambiar un endpoint**, actualizá sus comentarios y `docs/api/openapi.yaml` en el mismo PR. Para regenerar el YAML desde la API en marcha, exportá `/swagger/v1/swagger.json`.

### Base de datos

Requiere una instancia de MySQL con el esquema de [`docs/database/schema.sql`](docs/database/schema.sql) ya cargado (EF Core solo mapea; no hay migraciones de EF).

> **Al actualizar:** si tu base se creó con una versión anterior de `schema.sql`, ejecutá en orden los scripts de [`docs/database/migraciones/`](docs/database/migraciones/) que todavía no aplicaste (por ejemplo, `001-hu4-notificaciones.sql` para HU4), con tu base seleccionada, igual que `seed-dev.sql`. Una base nueva creada con `schema.sql` no los necesita. La versión de MySQL usada en el código es 8.0.46, fija en `backend/Infrastructure/DependencyInjection.cs`. Pendiente: confirmar la versión del equipo.

La conexión se configura en `backend/.env` (no se sube al repositorio):

```bash
cp backend/.env.example backend/.env
```

Completá `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER` y `DB_PASSWORD`, y `FRONTEND_ORIGIN` (origen del frontend permitido por CORS; ejemplo: `http://localhost:3000`). No hay valores por defecto: si falta alguna clave, la API no arranca y indica cuál falta. El archivo `.env` se busca en la carpeta del proyecto (`backend/`), así que se puede ejecutar desde cualquier carpeta.

**Asistente de IA (HU7, opcional).** Con `LLM_API_KEY` las recomendaciones las redacta un LLM, y entonces `LLM_BASE_URL` y `LLM_MODEL` también son obligatorios. Sirve cualquier proveedor con API compatible con OpenAI (Chat Completions) que soporte salida JSON con esquema:

| Proveedor | `LLM_BASE_URL` | Costo |
|---|---|---|
| Google Gemini (clave en [aistudio.google.com](https://aistudio.google.com)) | `https://generativelanguage.googleapis.com/v1beta/openai/` | Gratis, con límite de consultas |
| Ollama (local; `LLM_API_KEY` puede ser cualquier texto, p. ej. `ollama`) | `http://localhost:11434/v1/` | Gratis |
| OpenAI | `https://api.openai.com/v1/` | Pago |

`LLM_MODEL` es el nombre del modelo en ese proveedor (ejemplo para Gemini: `gemini-2.5-flash`). `LLM_TIMEOUT_SECONDS` (opcional, por defecto 20) es cuánto se espera al LLM antes de responder `503`.

Para usar **Ollama** (gratis, sin límites, y nada sale de tu computadora):

1. Instalalo desde [ollama.com/download](https://ollama.com/download) o con `winget install Ollama.Ollama`. Queda corriendo en segundo plano en `http://localhost:11434`.
2. Descargá un modelo chico (unos 2 GB): `ollama pull qwen2.5:3b`. Alternativa: `llama3.2:3b`.
3. En `backend/.env`: `LLM_API_KEY=ollama`, `LLM_BASE_URL=http://localhost:11434/v1/`, `LLM_MODEL=qwen2.5:3b` y `LLM_TIMEOUT_SECONDS=90` (sin tarjeta gráfica dedicada el modelo corre en el procesador y tarda más; la primera consulta, además, carga el modelo en memoria).

Sin clave, la API arranca igual y usa un asistente simulado sin LLM, que también recomienda solo lugares del catálogo. La clave es personal: nunca la subas al repositorio.

> **Al actualizar:** cada integrante debe agregar `FRONTEND_ORIGIN` a su `backend/.env` (ver `backend/.env.example`); sin esa clave la API no arranca.

Para comprobar la conexión a MySQL con la API en marcha:

```bash
curl -i http://localhost:5092/health/db
```

Responde `200 OK` si conecta y `503 Service Unavailable` si no.

#### Usuario de prueba (solo desarrollo)

> ⚠️ **SOLO para desarrollo local, nunca para producción.** La contraseña es pública en este repositorio.

| Dato | Valor |
|---|---|
| Correo | `dev.estudiante@ucb.edu.bo` |
| Contraseña | `Breaku-Dev-2026!` |

El script [`docs/database/seed-dev.sql`](docs/database/seed-dev.sql) inserta ese usuario (rol `ESTUDIANTE`, correo verificado, activo). Es idempotente: se puede ejecutar varias veces. **No incluye `USE`**: hay que ejecutarlo con tu base de datos ya seleccionada (el nombre es el de tu `DB_NAME`).

**Opción 1: MySQL Workbench.** Abrí `docs/database/seed-dev.sql`, dejá tu base de datos como esquema por defecto (clic derecho sobre ella en el panel de esquemas → *Set as Default Schema*) y ejecutá el script.

**Opción 2: línea de comandos**, pasando el nombre de la base como parámetro:

```bash
mysql -u <usuario> -p <nombre_de_la_base> < docs/database/seed-dev.sql
```

En Windows, `mysql` puede no estar en el `PATH`; en ese caso usá la ruta completa a `mysql.exe` (carpeta `bin` de tu instalación de MySQL) o la opción 1.

Token de desarrollo: **Pendiente** (depende del JWT de HU1, Amira).

#### Catálogo de prueba (solo desarrollo)

[`docs/database/seed-dev-catalogo.sql`](docs/database/seed-dev-catalogo.sql) carga una zona (campus), cuatro categorías y siete lugares de ejemplo para probar las recomendaciones (`POST /api/recomendaciones`, HU7). Se ejecuta igual que `seed-dev.sql` y **después** de él, porque usa al usuario de prueba como autor. Es idempotente. Dos lugares están ahí para comprobar los filtros y nunca deben recomendarse: uno queda fuera del radio y otro está `EN_REVISION`.

Para probarlo: registrá un horario con un hueco de 45 minutos o más, tomá el `id` del puente en `GET /api/puentes` y enviá `{ "puenteId": <id> }` a `POST /api/recomendaciones`.

> **Provisional (HU7):** por ahora la búsqueda en el catálogo usa la primera zona registrada (`RecuperadorLugaresProvisional`). Se reemplaza por la búsqueda de HU6 en `backend/Infrastructure/DependencyInjection.cs`. Sin `LLM_API_KEY`, el campo `modelo` del log queda como `simulado`.

### Frontend

Requisitos: Node.js 20.9 o superior y npm (probado con Node 24.13.0 y npm 11.6.2).

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

El frontend queda en `http://localhost:3000`. `NEXT_PUBLIC_API_URL` (en `frontend/.env.local`, no se sube al repositorio) es la URL base del backend; ejemplo: `http://localhost:5092`. Para que el navegador pueda llamar a la API, el backend debe tener `FRONTEND_ORIGIN=http://localhost:3000` en su `.env`.

Build de producción: `npm run build`. Pendiente: estilos, librerías de UI y de estado (el stack no las define).

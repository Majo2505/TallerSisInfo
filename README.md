# BREAKU

Proyecto de la materia SIS-227 (UCB). Plataforma web que detecta "puentes" (huecos libres de 45 minutos o más entre clases), recomienda lugares verificados del campus y forma comunidades efímeras.

## Stack

- Backend: C# con ASP.NET Core (.NET 10 LTS — propuesta, por confirmar)
- Acceso a datos: EF Core
- Autenticación: JWT
- Base de datos: MySQL 8.4 LTS (propuesta, por confirmar)
- Frontend: Next.js + React con TypeScript (propuesta, por confirmar)
- Planificado para el sprint 2: SignalR y OpenAI (visión, tool calling)

## Estructura del repositorio

Carpetas existentes hoy:

- `.github/` — configuración de GitHub (incluye `agents/`)
- `backend/` — proyecto ASP.NET Core único (`Breaku.csproj`, solución `Breaku.slnx`) con las capas como carpetas: `Presentation/`, `Application/`, `Domain/`, `Infrastructure/`
- `frontend/` — aplicación Next.js (TypeScript, App Router, `src/`)
- `docs/` — documentación
  - `docs/api/` — especificación OpenAPI de la API (`openapi.yaml`)
  - `docs/database/` — esquema de la base de datos

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
dotnet build Breaku.slnx
dotnet run --project Breaku.csproj --launch-profile http
```

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

Para probar los endpoints protegidos (`/api/horario`, `/api/puentes`, `/api/notificaciones`):

1. En Swagger UI, abre `POST /api/auth/login`, pulsa **Try it out** y envía el usuario de prueba (mira más abajo) o uno que hayas registrado.
2. Copia el valor de `token` de la respuesta.
3. Pulsa **Authorize**, pega el token (sin escribir `Bearer`) y confirma. El token se queda guardado aunque recargues la página.

Swagger solo aparece en `Development` (el perfil `http` de `launchSettings.json` ya lo es). Para activarlo en otro entorno, pon `SWAGGER_ENABLED=true` en `backend/.env`.

La documentación sale de los comentarios `///` de los controladores y DTOs (el proyecto genera `Breaku.xml` al compilar). **Cuando agregues o cambies un endpoint**, actualiza sus comentarios y `docs/api/openapi.yaml` en el mismo PR. Para regenerar el YAML con la API corriendo, exporta `/swagger/v1/swagger.json`.

### Base de datos

Necesitas una instancia de MySQL con el esquema de [`docs/database/schema.sql`](docs/database/schema.sql) ya cargado (EF Core solo mapea; no hay migraciones de EF).

> **Ojo al actualizar:** si tu base la creaste con una versión vieja de `schema.sql`, ejecuta en orden los scripts de [`docs/database/migraciones/`](docs/database/migraciones/) que todavía no corriste (por ejemplo, `001-hu4-notificaciones.sql` para HU4), con tu base seleccionada, igual que `seed-dev.sql`. Una base nueva creada con `schema.sql` no los necesita. La versión de MySQL que usa el código es la 8.0.46, fija en `backend/Infrastructure/DependencyInjection.cs`. Falta confirmar la versión del equipo.

La conexión se configura en `backend/.env` (no se sube al repositorio):

```bash
cp backend/.env.example backend/.env
```

Completa `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER` y `DB_PASSWORD`, más `FRONTEND_ORIGIN` (el origen del frontend que permite CORS; ejemplo: `http://localhost:3000`) y `JWT_SECRET` (una clave de 32 caracteres o más para firmar los tokens). No hay valores por defecto: si falta alguna, la API no arranca y te dice cuál falta. El archivo `.env` se busca en la carpeta del proyecto (`backend/`), así que puedes ejecutar desde cualquier carpeta.

> **Ojo al actualizar:** cada uno tiene que agregar `FRONTEND_ORIGIN` y `JWT_SECRET` a su `backend/.env` (mira `backend/.env.example`); sin esas claves la API no arranca.

Para ver si conecta a MySQL, con la API corriendo:

```bash
curl -i http://localhost:5092/health/db
```

Responde `200 OK` si conecta y `503 Service Unavailable` si no.

#### Usuario de prueba (solo desarrollo)

> **Ojo:** solo para desarrollo local, nunca para producción. La contraseña es pública en este repositorio.

| Dato | Valor |
|---|---|
| Correo | `dev.estudiante@ucb.edu.bo` |
| Contraseña | `Breaku-Dev-2026!` |

El script [`docs/database/seed-dev.sql`](docs/database/seed-dev.sql) inserta ese usuario (rol `ESTUDIANTE`, correo verificado, activo). Es idempotente: se puede ejecutar varias veces. **No incluye `USE`**: hay que ejecutarlo con tu base de datos ya seleccionada (el nombre es el de tu `DB_NAME`).

**Opción 1: MySQL Workbench.** Abre `docs/database/seed-dev.sql`, deja tu base de datos como esquema por defecto (clic derecho sobre ella en el panel de esquemas → *Set as Default Schema*) y ejecuta el script.

**Opción 2: línea de comandos**, pasando el nombre de la base como parámetro:

```bash
mysql -u <usuario> -p <nombre_de_la_base> < docs/database/seed-dev.sql
```

En Windows, `mysql` puede no estar en el `PATH`; en ese caso usa la ruta completa a `mysql.exe` (carpeta `bin` de tu instalación de MySQL) o la opción 1.

Token de desarrollo: sácalo con `POST /api/auth/login` (en Swagger o con `curl`) usando el usuario de prueba.

### Frontend

Necesitas Node.js 20.9 o superior y npm (lo probamos con Node 24.13.0 y npm 11.6.2).

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

El frontend queda en `http://localhost:3000`. `NEXT_PUBLIC_API_URL` (en `frontend/.env.local`, no se sube al repositorio) es la URL base del backend; ejemplo: `http://localhost:5092`. Para que el navegador pueda llamar a la API, el backend tiene que tener `FRONTEND_ORIGIN=http://localhost:3000` en su `.env`.

Build de producción: `npm run build`. Falta definir librerías de UI y de estado (el stack todavía no las define).

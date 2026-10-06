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
  - `docs/database/` — esquema de la base de datos

Archivos en la raíz: `AGENTS.md` (contexto compartido para IAs), `CLAUDE.md`, `README.md`, `.gitignore`.

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
dotnet build Breaku.slnx
dotnet run --project Breaku.csproj --launch-profile http
```

La API queda en `http://localhost:5092`. Para comprobar que corre:

```bash
curl -i http://localhost:5092/health
```

Debe responder `200 OK`.

### Base de datos

Requiere una instancia de MySQL con el esquema de [`docs/database/schema.sql`](docs/database/schema.sql) ya cargado (EF Core solo mapea; no hay migraciones). La versión de MySQL usada en el código es 8.0.46, fija en `backend/Infrastructure/DependencyInjection.cs`. Pendiente: confirmar la versión del equipo.

La conexión se configura en `backend/.env` (no se sube al repositorio):

```bash
cp backend/.env.example backend/.env
```

Completá `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER` y `DB_PASSWORD`, y `FRONTEND_ORIGIN` (origen del frontend permitido por CORS; ejemplo: `http://localhost:3000`). No hay valores por defecto: si falta alguna clave, la API no arranca y indica cuál falta.

> **Al actualizar:** cada integrante debe agregar `FRONTEND_ORIGIN` a su `backend/.env` (ver `backend/.env.example`); sin esa clave la API no arranca. El archivo `.env` se busca en la carpeta del proyecto (`backend/`), así que se puede ejecutar desde cualquier carpeta.

Para comprobar la conexión a MySQL con la API en marcha:

```bash
curl -i http://localhost:5092/health/db
```

Responde `200 OK` si conecta y `503 Service Unavailable` si no.

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

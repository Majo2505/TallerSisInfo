# AGENTS.md — Contexto compartido para IAs (BREAKU)

Este archivo da el mismo contexto a todas las IAs que usa el equipo. **Leelo y revisá `docs/` antes de proponer cambios.**

> Regla fundamental: no inventar tecnologías, tablas, endpoints ni comandos. Lo que no existe o no está decidido figura como **Pendiente**.

## Qué es BREAKU

Proyecto de la materia SIS-227 (UCB). Plataforma web que detecta "puentes" (huecos libres de 45 minutos o más entre clases), recomienda lugares verificados del campus y forma comunidades efímeras.

## Stack

| Capa | Tecnología |
|---|---|
| Backend | C# con ASP.NET Core (.NET) — **.NET 10 LTS: propuesta, por confirmar** |
| Acceso a datos | EF Core |
| Autenticación | JWT |
| Base de datos | MySQL — **MySQL 8.4 LTS: propuesta, por confirmar** |
| Frontend | Next.js + React con TypeScript — **TypeScript (versión): propuesta, por confirmar** |

Planificado para el **sprint 2** (NO en el sprint 1): SignalR y OpenAI (visión, tool calling).

## Arquitectura

- Cliente-servidor de 3 niveles.
- Backend: monolito modular en capas con inversión de dependencias, con 4 proyectos: `Api`, `Application`, `Domain`, `Infrastructure`.
  - `Domain` no depende de nada.
  - Solo `Infrastructure` toca la BD y los servicios externos.
  - El servidor de Next.js no accede a la BD.
- **NO** usar microservicios ni agregar componentes que no estén en este diseño.

## Base de datos

- Esquema: [`docs/database/schema.sql`](docs/database/schema.sql) (14 tablas).
- Fechas en UTC.
- Borrado lógico en `usuario` con la columna `activo`.
- Auditoría: `created_at` / `updated_at` / `created_by` / `updated_by` solo donde el esquema los define; `historial_cambio` para USUARIO, LUGAR y ZONA.
- El esquema solo lo modifica Ariana (BD). Si alguien necesita un cambio, lo pide.

## Alcance del Sprint 1 (sin IA)

- **HU1** — Login con JWT.
- **HU2** — Horario cargado a mano (cuadrícula, editar/eliminar, sin solapamientos).
- **HU3** — Detección de puentes.

Fuera del sprint: foto con IA, RAG, comunidades, notificaciones, tiempo real.

## Convenciones y responsables

- Una rama por issue: `feature/<issue>-descripcion`.
- PR a `main` con al menos 1 revisión. Nada de push directo a `main`.
- Responsables: HU1 Amira, HU2 Maite, HU3 Majo, BD y base Ariana.

## Convenciones de nombres

- **BD:** snake_case, tal como está en [`docs/database/schema.sql`](docs/database/schema.sql). No se cambia.
- **JSON de la API:** camelCase.
- **C#:** PascalCase para clases, propiedades y métodos; camelCase para variables locales y parámetros; `_camelCase` para campos privados. Las entidades mapean las columnas snake_case con configuración de EF Core, sin renombrar la BD.
- **TypeScript/React:** camelCase para variables y funciones; PascalCase para componentes.

## Instrucciones para las IAs

1. Leer este archivo y `docs/` antes de proponer cambios.
2. No inventar endpoints ni tablas.
3. Si algo contradice el diseño, consultar con la persona responsable.
4. Si algo no está definido, escribir "Pendiente" en vez de suponerlo.

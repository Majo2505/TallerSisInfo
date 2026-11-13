# Pruebas de la base de datos (Fase 0)

Verifican que las migraciones `022` a `028`, el `schema.sql` y los seeds nuevos dejan la base como se acordó, **sin tocar ninguna
base real**. No llevan credenciales: la conexión sale de variables de entorno y los scripts **se niegan a correr** contra un host
que no sea `127.0.0.1` o `localhost` (nunca contra Aiven).

| Archivo | Qué es |
|---|---|
| `verificar-fase0.sh` | El script principal. Crea y borra las bases `fase0_viejo`, `fase0_nuevo` y `fase0_migrado` en un MySQL local. |
| `casos.txt` | Inserts válidos e inválidos (evento sin lugar, PENDIENTE o RECHAZADO sin motivo, avisos sin su referencia, sanciones mal armadas...). Cada inválido debe fallar por **su** CHECK o FK. |
| `datos-viejos.sql` | Filas con el esquema anterior (4 tipos de aviso, 4 motivos de reporte, 2 tipos de log, historial) para comprobar que sobreviven. |
| `consultas-esquema.sql`, `estado-datos.sql` | Consultas de solo lectura con las que se compara el esquema y los datos. |
| `smoke-api.sh` | Arranca el backend de `main` sin cambios contra la base migrada y recorre los endpoints que tocan las tablas modificadas. |

## Qué comprueba `verificar-fase0.sh`

1. `schema.sql` de `main` + migraciones `022` a `028` equivale al `schema.sql` nuevo (columnas, índices, CHECK, FK y vista).
2. Las filas que ya existían sobreviven a la migración.
3. Los seeds nuevos se pueden correr dos veces; el de eventos refresca las fechas «hoy» y «próximos días».
4. Los casos de `casos.txt` (válidos aceptados, inválidos rechazados por el CHECK o la FK correctos).
5. La reparación de tildes (`028`) arregla una fila rota a propósito y es idempotente.
6. Las reversas, con sus guardas (abortan si ya hay filas de los tipos nuevos), devuelven el esquema anterior, y las migraciones se pueden volver a aplicar.

## Cómo correrlo

Necesita un MySQL 8 **local** (pensado para `sql_mode` ANSI y `sql_require_primary_key = ON`, como Aiven) y el cliente `mysql`, `git`, `diff` y `sed`.
Con Docker, por ejemplo (cambia `8.0` por `8.4` para probar la otra versión):

```bash
docker run -d --name fase0-mysql -e MYSQL_ROOT_PASSWORD=<una-clave-local> -p 3307:3306 mysql:8.0 \
  --sql-mode=ANSI --sql-require-primary-key=ON --character-set-server=utf8mb4 --collation-server=utf8mb4_0900_ai_ci
MYSQL_HOST=127.0.0.1 MYSQL_PORT=3307 MYSQL_USER=root MYSQL_PWD=<una-clave-local> docs/database/pruebas/verificar-fase0.sh
docker rm -f fase0-mysql
```

El `schema.sql` anterior sale de `origin/main` (`BASE_REF=<otra rama>` para cambiarlo). Con `--conservar` no se borra `fase0_migrado`,
para probar la API:

```bash
DB_PORT=3307 DB_USER=<usuario con permisos sobre fase0_migrado> DB_PASSWORD=<su clave> docs/database/pruebas/smoke-api.sh
```

`smoke-api.sh` necesita el SDK de .NET 10 y las cuentas de prueba de `seed-dev.sql`. Sin clave de LLM la API usa las versiones simuladas.
Una línea `[AVISO]` es informativa y no cuenta como falla.

#!/usr/bin/env bash
# Verificación de la Fase 0 de la base de datos (migraciones 022 a 028, schema.sql y seeds nuevos).
#
# Qué comprueba, en una base MySQL 8 LOCAL de pruebas (crea y borra las bases fase0_viejo, fase0_nuevo y fase0_migrado):
#   1. schema.sql actual de main + migraciones 022 a 028 = el schema.sql nuevo (columnas, índices, CHECK, FK y vista).
#   2. Las filas que ya existían antes (4 tipos de aviso, 4 motivos de reporte, 2 tipos de log, historial) sobreviven.
#   3. Los seeds nuevos (etiquetas y eventos) se pueden correr dos veces y el de eventos refresca las fechas.
#   4. Los casos de casos.txt: inserts inválidos rechazados por el CHECK correcto (evento sin lugar, PENDIENTE y RECHAZADO sin
#      motivo, etc.) y válidos aceptados.
#   5. La reparación de tildes (028) arregla una fila rota a propósito y es idempotente.
#   6. Las reversas (con sus guardas) devuelven la base al esquema anterior y las migraciones se pueden volver a aplicar.
#
# Uso (no lleva credenciales: se leen del entorno). Solo corre contra 127.0.0.1 o localhost; se niega a correr contra otra base.
#   MYSQL_HOST=127.0.0.1 MYSQL_PORT=3306 MYSQL_USER=root MYSQL_PWD=... ./verificar-fase0.sh [--conservar]
#   --conservar: no borra fase0_migrado al terminar (sirve para probar la API contra esa base con smoke-api.sh).
# Variables opcionales: BASE_REF (la rama de la que sale el schema.sql anterior; por defecto origin/main).
# Necesita el cliente `mysql`, `git`, `diff` y `sed`.

set -u

HOST="${MYSQL_HOST:-127.0.0.1}"
PUERTO="${MYSQL_PORT:-3306}"
USUARIO="${MYSQL_USER:-root}"
export MYSQL_PWD="${MYSQL_PWD:-}"
BASE_REF="${BASE_REF:-origin/main}"
CONSERVAR=0
[ "${1:-}" = "--conservar" ] && CONSERVAR=1

case "$HOST" in
  127.0.0.1|localhost|::1) ;;
  *) echo "Este script solo corre contra una base local de pruebas (127.0.0.1 o localhost), no contra '$HOST'." >&2; exit 2 ;;
esac

AQUI="$(cd "$(dirname "$0")" && pwd)"
DB="$(cd "$AQUI/.." && pwd)"
RAIZ="$(cd "$DB/../.." && pwd)"
MIG="$DB/migraciones"
REV="$MIG/reversa"
TMP="$(mktemp -d)"
FALLOS=0
PASOS=0

mq() { mysql --no-defaults -h"$HOST" -P"$PUERTO" -u"$USUARIO" --default-character-set=utf8mb4 "$@"; }
ok()  { PASOS=$((PASOS + 1)); echo "  [OK]    $1"; }
mal() { PASOS=$((PASOS + 1)); FALLOS=$((FALLOS + 1)); echo "  [FALLA] $1"; }
titulo() { echo; echo "== $1"; }
limpiar() { if [ "$CONSERVAR" = 0 ]; then for b in fase0_viejo fase0_nuevo fase0_migrado; do mq -e "DROP DATABASE IF EXISTS $b" 2>/dev/null; done; fi; rm -rf "$TMP"; }
trap limpiar EXIT

abortar() { echo "No se pudo seguir: $1" >&2; exit 3; }
cargar() { # cargar <base> <archivo>
  local salida; salida="$(mq "$1" < "$2" 2>&1)" || { echo "$salida" | head -5; return 1; }
}

mq -e "SELECT 1" >/dev/null 2>&1 || abortar "no hay conexión con MySQL en $HOST:$PUERTO (revisa MYSQL_PORT y MYSQL_PWD)"
echo "MySQL: $(mq -N -e 'SELECT VERSION()')  sql_mode: $(mq -N -e 'SELECT @@sql_mode' | cut -c1-60)...  sql_require_primary_key: $(mq -N -e 'SELECT @@sql_require_primary_key')"

titulo "1. Preparación"
for b in fase0_viejo fase0_nuevo fase0_migrado; do
  mq -e "DROP DATABASE IF EXISTS $b; CREATE DATABASE $b CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci" || abortar "no pude crear $b"
done
git -C "$RAIZ" show "$BASE_REF:docs/database/schema.sql" | sed -E '/^(CREATE DATABASE|USE) /d' > "$TMP/schema_viejo.sql" || abortar "no pude leer el schema.sql de $BASE_REF"
sed -E '/^(CREATE DATABASE|USE) /d' "$DB/schema.sql" > "$TMP/schema_nuevo.sql"
cargar fase0_viejo "$TMP/schema_viejo.sql"   && ok "schema.sql de $BASE_REF cargado en fase0_viejo"   || abortar "schema viejo"
cargar fase0_nuevo "$TMP/schema_nuevo.sql"   && ok "schema.sql nuevo cargado en fase0_nuevo"         || abortar "schema nuevo"
cargar fase0_migrado "$TMP/schema_viejo.sql" && ok "schema.sql de $BASE_REF cargado en fase0_migrado" || abortar "schema viejo en migrado"
for seed in seed-dev.sql seed-dev-catalogo.sql seed-dev-cochabamba.sql seed-dev-matching.sql datos-viejos.sql; do
  ruta="$DB/$seed"; [ "$seed" = datos-viejos.sql ] && ruta="$AQUI/$seed"
  cargar fase0_migrado "$ruta" && ok "cargado en fase0_migrado (esquema viejo): $seed" || abortar "$seed"
done
mq -N fase0_migrado < "$AQUI/estado-datos.sql" > "$TMP/antes.txt"

titulo "2. Migraciones 022 a 027 sobre la base con datos viejos"
for f in "$MIG"/02[2-7]-*.sql; do
  salida="$(mq fase0_migrado < "$f" 2>&1)" && ok "aplicada $(basename "$f")" || { mal "$(basename "$f"): $salida"; }
done

titulo "3. Reparación de tildes (028)"
mq fase0_migrado -e "UPDATE lugar SET actividades_sugeridas = CONVERT(CAST('[\"comida rápida\", \"té\"]' AS BINARY) USING cp850) WHERE nombre = 'Cairo Restaurante'" \
  || abortar "no pude romper la fila de prueba"
roto="$(mq -N fase0_migrado -e "SELECT HEX(actividades_sugeridas) LIKE '%E2949C%' FROM lugar WHERE nombre = 'Cairo Restaurante'")"
[ "$roto" = 1 ] && ok "fila rota a propósito (tiene E2949C, como en Aiven)" || mal "no pude romper la fila"
mq -N fase0_migrado -e "SELECT id, MD5(IFNULL(actividades_sugeridas,'')) FROM lugar WHERE nombre <> 'Cairo Restaurante' ORDER BY id" > "$TMP/otros_antes.txt"
salida="$(mq fase0_migrado < "$MIG/028-reparar-actividades-sugeridas.sql" 2>&1)" && ok "aplicada 028-reparar-actividades-sugeridas.sql" || mal "028: $salida"
texto="$(mq -N fase0_migrado -e "SELECT actividades_sugeridas FROM lugar WHERE nombre = 'Cairo Restaurante'")"
[ "$texto" = '["comida rápida", "té"]' ] && ok "la fila quedó reparada: $texto" || mal "la fila no quedó bien: $texto"
rotas="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM lugar WHERE HEX(actividades_sugeridas) LIKE '%E2949C%'")"
[ "$rotas" = 0 ] && ok "no quedan filas con E2949C" || mal "quedan $rotas filas con E2949C"
hexa="$(mq -N fase0_migrado -e "SELECT HEX(SUBSTRING(actividades_sugeridas, 11, 1)) FROM lugar WHERE nombre = 'Cairo Restaurante'")"
[ "$hexa" = "C3A1" ] && ok "la «á» de «rápida» es C3A1" || mal "la «á» quedó como $hexa"
mq -N fase0_migrado -e "SELECT id, MD5(IFNULL(actividades_sugeridas,'')) FROM lugar WHERE nombre <> 'Cairo Restaurante' ORDER BY id" > "$TMP/otros_despues.txt"
diff -q "$TMP/otros_antes.txt" "$TMP/otros_despues.txt" >/dev/null && ok "los demás lugares no cambiaron" || mal "la 028 cambió otros lugares"
mq fase0_migrado < "$MIG/028-reparar-actividades-sugeridas.sql" >/dev/null 2>&1
texto2="$(mq -N fase0_migrado -e "SELECT actividades_sugeridas FROM lugar WHERE nombre = 'Cairo Restaurante'")"
[ "$texto2" = "$texto" ] && ok "la 028 es idempotente (correrla otra vez no cambia nada)" || mal "la 028 cambió algo al correrla otra vez"

titulo "4. Las filas viejas sobreviven"
mq -N fase0_migrado < "$AQUI/estado-datos.sql" > "$TMP/despues.txt"
if diff "$TMP/antes.txt" "$TMP/despues.txt" >/dev/null; then ok "avisos, reportes, logs, historial y lugares: iguales antes y después de migrar ($(wc -l < "$TMP/antes.txt") resúmenes)"
else mal "cambió el estado de los datos viejos:"; diff "$TMP/antes.txt" "$TMP/despues.txt" | head -10; fi

titulo "5. schema.sql nuevo equivale a schema.sql anterior + migraciones"
norm() { sed -E "s/fase0_(migrado|nuevo|viejo)/DB/g"; }
mq -N -B fase0_migrado < "$AQUI/consultas-esquema.sql" | norm > "$TMP/esq_migrado.txt"
mq -N -B fase0_nuevo   < "$AQUI/consultas-esquema.sql" | norm > "$TMP/esq_nuevo.txt"
if diff "$TMP/esq_migrado.txt" "$TMP/esq_nuevo.txt" > "$TMP/dif.txt"; then ok "el esquema migrado y el schema.sql nuevo son iguales ($(wc -l < "$TMP/esq_nuevo.txt") líneas comparadas)"
else mal "el esquema migrado y el schema.sql nuevo difieren:"; head -20 "$TMP/dif.txt"; fi
cat_mig="$(mq -N fase0_migrado -e "SELECT nombre FROM etiqueta_actividad ORDER BY nombre")"
cat_nue="$(mq -N fase0_nuevo   -e "SELECT nombre FROM etiqueta_actividad ORDER BY nombre")"
[ "$cat_mig" = "$cat_nue" ] && [ "$(echo "$cat_nue" | wc -l)" = 8 ] && ok "el catálogo de 8 etiquetas es igual en las dos bases" || mal "el catálogo de etiquetas difiere"

titulo "6. Seeds nuevos (re-ejecutables)"
for i in 1 2; do
  salida="$(mq fase0_migrado < "$DB/seed-dev-etiquetas.sql" 2>&1)" && ok "seed-dev-etiquetas.sql, corrida $i" || mal "seed-dev-etiquetas.sql: $salida"
done
n="$(mq -N fase0_migrado -e "SELECT COUNT(DISTINCT lugar_id) FROM lugar_etiqueta")"; [ "$n" = 14 ] && ok "14 lugares con etiqueta" || mal "lugares con etiqueta: $n (esperado 14)"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM lugar_etiqueta")"; [ "$n" = 15 ] && ok "15 asignaciones (Cafetería Central lleva dos)" || mal "asignaciones: $n (esperado 15)"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM lugar l WHERE l.recomendable AND l.estado = 'ACTIVO' AND NOT EXISTS (SELECT 1 FROM lugar_etiqueta le WHERE le.lugar_id = l.id)")"
[ "$n" = 0 ] && ok "todo lugar ACTIVO recomendable tiene al menos una etiqueta" || mal "$n lugares ACTIVOS recomendables sin etiqueta"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM lugar_etiqueta le JOIN lugar l ON l.id = le.lugar_id WHERE l.nombre LIKE 'Kiosco Nuevo%'")"
[ "$n" = 0 ] && ok "Kiosco Nuevo (en revisión) no se etiqueta" || mal "Kiosco Nuevo quedó etiquetado"
for i in 1 2; do
  salida="$(mq fase0_migrado < "$DB/seed-dev-eventos.sql" 2>&1)" && ok "seed-dev-eventos.sql, corrida $i" || mal "seed-dev-eventos.sql: $salida"
done
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE titulo LIKE '[Prueba]%'")"; [ "$n" = 6 ] && ok "6 eventos de prueba, sin duplicados tras correrlo dos veces" || mal "eventos de prueba: $n (esperado 6)"
HOY="DATE(inicio - INTERVAL 4 HOUR) = DATE(UTC_TIMESTAMP() - INTERVAL 4 HOUR)"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE titulo LIKE '[Prueba]%' AND $HOY")"; [ "$n" = 3 ] && ok "3 eventos son de hoy (hora de Bolivia)" || mal "eventos de hoy: $n (esperado 3)"
mq fase0_migrado -e "UPDATE evento SET inicio = inicio - INTERVAL 10 DAY, fin = fin - INTERVAL 10 DAY, estado = 'CANCELADO', motivo = NULL WHERE titulo LIKE '[Prueba]%'"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE titulo LIKE '[Prueba]%' AND $HOY")"; [ "$n" = 0 ] && ok "simulé que pasaron 10 días: ningún evento es de hoy" || mal "la simulación falló ($n)"
salida="$(mq fase0_migrado < "$DB/seed-dev-eventos.sql" 2>&1)" && ok "seed-dev-eventos.sql corrido de nuevo para refrescar" || mal "refrescar: $salida"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE titulo LIKE '[Prueba]%' AND $HOY")"; [ "$n" = 3 ] && ok "las fechas se refrescaron: otra vez 3 eventos de hoy" || mal "tras refrescar, eventos de hoy: $n (esperado 3)"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE titulo LIKE '[Prueba]%'")"; [ "$n" = 6 ] && ok "siguen siendo 6 eventos (se actualizaron, no se duplicaron)" || mal "eventos tras refrescar: $n"
n="$(mq -N fase0_migrado -e "SELECT CONCAT(SUM(estado = 'PUBLICADO'), '/', SUM(estado = 'PENDIENTE'), '/', SUM(estado = 'CANCELADO')) FROM evento WHERE titulo LIKE '[Prueba]%'")"
[ "$n" = "5/1/0" ] && ok "los estados volvieron a los del seed (5 PUBLICADO, 1 PENDIENTE)" || mal "estados tras refrescar: $n"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE lugar_id IS NULL AND lugar_texto IS NOT NULL")"; [ "$n" -ge 2 ] && ok "hay eventos sin lugar del catálogo (solo con lugar_texto)" || mal "faltan eventos con lugar_texto"

titulo "7. Casos válidos e inválidos (casos.txt)"
U="(SELECT id FROM usuario WHERE correo='dev.estudiante@ucb.edu.bo')"
A="(SELECT id FROM usuario WHERE correo='dev.admin@ucb.edu.bo')"
L="(SELECT id FROM lugar WHERE nombre='Biblioteca Central')"
E="(SELECT id FROM evento WHERE titulo='[Prueba] Feria de libros')"
C="(SELECT id FROM comunidad_efimera ORDER BY id LIMIT 1)"
R="(SELECT id FROM reporte WHERE estado='PENDIENTE' ORDER BY id LIMIT 1)"
casos=0; casos_mal=0
while IFS= read -r linea; do
  case "$linea" in ''|'#'*) continue ;; esac
  esperado="${linea%% ||| *}"; sql="${linea#* ||| }"
  sql="${sql//\{U\}/$U}"; sql="${sql//\{A\}/$A}"; sql="${sql//\{L\}/$L}"; sql="${sql//\{E\}/$E}"; sql="${sql//\{C\}/$C}"; sql="${sql//\{R\}/$R}"
  salida="$(mq fase0_migrado -e "START TRANSACTION; $sql; ROLLBACK;" 2>&1)"; rc=$?
  casos=$((casos + 1))
  if [ "$esperado" = OK ]; then
    [ $rc -eq 0 ] || { casos_mal=$((casos_mal + 1)); mal "debía funcionar y falló: ${linea:0:110}... → ${salida:0:140}"; }
  else
    if [ $rc -eq 0 ]; then casos_mal=$((casos_mal + 1)); mal "debía fallar con '$esperado' y NO falló: ${linea:0:130}"
    elif ! grep -qF -- "$esperado" <<<"$salida"; then casos_mal=$((casos_mal + 1)); mal "falló, pero no por '$esperado': ${salida:0:160}"; fi
  fi
done < <(tr -d '\r' < "$AQUI/casos.txt")
[ "$casos_mal" = 0 ] && ok "los $casos casos se comportaron como se esperaba (inválidos rechazados por su CHECK o FK, válidos aceptados)"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM evento WHERE titulo IN ('Sin lugar','Lugar en blanco','Pendiente sin motivo','Rechazado sin motivo','Con lugar del catálogo')")"
[ "$n" = 0 ] && ok "los casos no dejaron datos (todo se deshizo)" || mal "los casos dejaron $n eventos"

titulo "8. Reversas (con guardas) y volver a migrar"
rev() { mq fase0_migrado < "$REV/$1" 2>&1; }
EV="(SELECT id FROM evento WHERE titulo='[Prueba] Feria de libros')"
rev 028-reparar-actividades-sugeridas-reversa.sql >/dev/null && ok "028: no tiene reversa (solo avisa)"
rev 027-hu30-etiquetas-reversa.sql >/dev/null && ok "027 revertida" || mal "reversa 027"

mq fase0_migrado -e "INSERT INTO notificacion (usuario_id, tipo, evento_id, mensaje) VALUES ($U, 'EVENTO_APROBADO', $EV, 'Aprobado.')"
salida="$(rev 026-notificaciones-eventos-reportes-sanciones-reversa.sql)"; rc=$?
{ [ $rc -ne 0 ] && grep -qF "revertir la 026" <<<"$salida"; } && ok "026: la guarda aborta si hay avisos de los tipos nuevos" || mal "026: la guarda no abortó ($salida)"
n="$(mq -N fase0_migrado -e "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'notificacion' AND column_name = 'evento_id'")"
[ "$n" = 1 ] && ok "026: al abortar no cambió nada" || mal "026: cambió algo al abortar"
mq fase0_migrado -e "DELETE FROM notificacion WHERE tipo = 'EVENTO_APROBADO'"
salida="$(rev 026-notificaciones-eventos-reportes-sanciones-reversa.sql)" && ok "026 revertida" || mal "reversa 026: $salida"
rev 025-hu28-sanciones-reversa.sql >/dev/null && ok "025 revertida" || mal "reversa 025"

mq fase0_migrado -e "INSERT INTO reporte (usuario_id, evento_id, motivo, descripcion) VALUES ($U, $EV, 'EVENTO_INAPROPIADO', 'El evento tiene una foto inapropiada.')"
salida="$(rev 024-hu27-reporte-evento-respuesta-reversa.sql)"; rc=$?
{ [ $rc -ne 0 ] && grep -qF "revertir la 024" <<<"$salida"; } && ok "024: la guarda aborta si hay reportes de eventos" || mal "024: la guarda no abortó ($salida)"
mq fase0_migrado -e "DELETE FROM reporte WHERE motivo = 'EVENTO_INAPROPIADO'"
salida="$(rev 024-hu27-reporte-evento-respuesta-reversa.sql)" && ok "024 revertida" || mal "reversa 024: $salida"

mq fase0_migrado -e "INSERT INTO log_auditoria_rag (usuario_id, tipo, estado) VALUES ($U, 'EXTRACCION_EVENTO', 'OK')"
salida="$(rev 023-hu24-log-extraccion-evento-reversa.sql)"; rc=$?
{ [ $rc -ne 0 ] && grep -qF "revertir la 023" <<<"$salida"; } && ok "023: la guarda aborta si hay registros EXTRACCION_EVENTO" || mal "023: la guarda no abortó ($salida)"
mq fase0_migrado -e "DELETE FROM log_auditoria_rag WHERE tipo = 'EXTRACCION_EVENTO'"
salida="$(rev 023-hu24-log-extraccion-evento-reversa.sql)" && ok "023 revertida" || mal "reversa 023: $salida"

mq fase0_migrado -e "INSERT INTO historial_cambio (entidad, entidad_id, accion, valores_anteriores, valores_nuevos, usuario_id) VALUES ('EVENTO', $EV, 'UPDATE', '{\"a\":1}', '{\"a\":2}', $A)"
salida="$(rev 022-hu22-eventos-reversa.sql)"; rc=$?
{ [ $rc -ne 0 ] && grep -qF "revertir la 022" <<<"$salida"; } && ok "022: la guarda aborta si hay historial de eventos" || mal "022: la guarda no abortó ($salida)"
mq fase0_migrado -e "DELETE FROM historial_cambio WHERE entidad = 'EVENTO'"
salida="$(rev 022-hu22-eventos-reversa.sql)" && ok "022 revertida" || mal "reversa 022: $salida"

mq -N -B fase0_migrado < "$AQUI/consultas-esquema.sql" 2>/dev/null | norm | sed '/^catalogo_etiquetas/,$d' > "$TMP/esq_revertido.txt"
mq -N -B fase0_viejo < "$AQUI/consultas-esquema.sql" 2>/dev/null | norm | sed '/^catalogo_etiquetas/,$d' > "$TMP/esq_viejo.txt"
if diff "$TMP/esq_viejo.txt" "$TMP/esq_revertido.txt" > "$TMP/dif2.txt"; then ok "tras las reversas, el esquema es idéntico al anterior a la Fase 0"
else mal "tras las reversas el esquema difiere del anterior:"; head -20 "$TMP/dif2.txt"; fi
mq -N fase0_migrado < "$AQUI/estado-datos.sql" > "$TMP/revertido.txt"
diff "$TMP/antes.txt" "$TMP/revertido.txt" >/dev/null && ok "tras las reversas, las filas viejas siguen intactas" || mal "las reversas cambiaron filas viejas"

for f in "$MIG"/02[2-8]-*.sql; do
  salida="$(mq fase0_migrado < "$f" 2>&1)" || mal "al volver a aplicar $(basename "$f"): $salida"
done
mq -N -B fase0_migrado < "$AQUI/consultas-esquema.sql" | norm > "$TMP/esq_remigrado.txt"
diff "$TMP/esq_remigrado.txt" "$TMP/esq_nuevo.txt" >/dev/null && ok "al volver a aplicar 022 a 028, el esquema vuelve a ser igual al schema.sql nuevo" || mal "volver a aplicar dejó un esquema distinto"
mq fase0_migrado < "$DB/seed-dev-etiquetas.sql" >/dev/null 2>&1; mq fase0_migrado < "$DB/seed-dev-eventos.sql" >/dev/null 2>&1

echo
if [ "$FALLOS" = 0 ]; then echo "RESULTADO: $PASOS comprobaciones, todas bien."; else echo "RESULTADO: $FALLOS de $PASOS comprobaciones fallaron."; fi
[ "$CONSERVAR" = 1 ] && echo "fase0_migrado se conservó (llena de datos de prueba) para probar la API."
exit "$FALLOS"

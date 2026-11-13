#!/usr/bin/env bash
# Prueba de humo: el backend ACTUAL de main (sin cambios) funciona sobre la base ya migrada.
# Arranca la API contra la base de pruebas que dejó verificar-fase0.sh --conservar (fase0_migrado), recorre los endpoints que
# tocan las tablas modificadas (notificacion, reporte, log_auditoria_rag, historial_cambio, lugar) y la apaga.
#
# Uso (sin credenciales en el repo: se leen del entorno; solo corre contra 127.0.0.1 o localhost):
#   DB_PORT=3306 DB_USER=... DB_PASSWORD=... ./smoke-api.sh
# Variables opcionales: DB_HOST (127.0.0.1), DB_NAME (fase0_migrado), API_URL (http://127.0.0.1:5199),
#   LUGAR_EVENTO_ID (id de «Sala de Estudio Grupal», que solo tiene un evento de prueba; si falta, se busca por nombre).
# Necesita dotnet (SDK 10), curl y las cuentas de prueba de seed-dev.sql. Con la clave del LLM vacía la API usa las versiones
# simuladas, así que no gasta ni llama a ningún LLM.

set -u
DB_HOST="${DB_HOST:-127.0.0.1}"
DB_NAME="${DB_NAME:-fase0_migrado}"
API_URL="${API_URL:-http://127.0.0.1:5199}"
: "${DB_PORT:?Falta DB_PORT}" "${DB_USER:?Falta DB_USER}" "${DB_PASSWORD:?Falta DB_PASSWORD}"
case "$DB_HOST" in 127.0.0.1|localhost|::1) ;; *) echo "Solo corre contra una base local de pruebas, no contra '$DB_HOST'." >&2; exit 2 ;; esac

AQUI="$(cd "$(dirname "$0")" && pwd)"
RAIZ="$(cd "$AQUI/../../.." && pwd)"
TMP="$(mktemp -d)"
FALLOS=0; PASOS=0
ok()  { PASOS=$((PASOS + 1)); echo "  [OK]    $1"; }
mal() { PASOS=$((PASOS + 1)); FALLOS=$((FALLOS + 1)); echo "  [FALLA] $1"; }
aviso() { echo "  [AVISO] $1"; }

export DB_HOST DB_PORT DB_NAME DB_USER DB_PASSWORD DB_SSL_MODE=None
export JWT_SECRET="smoke-test-secret-solo-para-esta-prueba-0123456789" FRONTEND_ORIGIN="http://localhost:3000"
export LLM_API_KEY="" ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$API_URL"

echo "Arrancando la API (dotnet run)..."
( cd "$RAIZ/backend" && dotnet run --no-launch-profile > "$TMP/api.log" 2>&1 ) &
API_PID=$!
apagar() { kill "$API_PID" 2>/dev/null; taskkill //F //IM Breaku.exe >/dev/null 2>&1; pkill -f "Breaku.dll" 2>/dev/null; rm -rf "$TMP"; }
trap apagar EXIT

for i in $(seq 1 60); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' "$API_URL/health/db" 2>/dev/null)" = 200 ] && break
  sleep 3
done

llamar() { # llamar <método> <ruta> <token|-> [cuerpo json]  → deja el código en $CODIGO y el cuerpo en $CUERPO
  local m="$1" r="$2" t="$3" b="${4:-}"
  local args=(-s -o "$TMP/cuerpo.txt" -w '%{http_code}' -X "$m" "$API_URL$r" -H 'Content-Type: application/json')
  [ "$t" != "-" ] && args+=(-H "Authorization: Bearer $t")
  [ -n "$b" ] && args+=(-d "$b")
  CODIGO="$(curl "${args[@]}")"; CUERPO="$(cat "$TMP/cuerpo.txt")"
}
esperar() { # esperar <nombre> <código esperado>
  [ "$CODIGO" = "$2" ] && ok "$1 → $CODIGO" || mal "$1 → $CODIGO (esperado $2): ${CUERPO:0:160}"
}
id_de() { # id_de <texto que debe aparecer en el objeto>: id del primer objeto JSON de $CUERPO que lo contiene
  tr '}' '\n' <<<"$CUERPO" | grep -F -- "$1" | sed -n 's/.*"id":\([0-9]*\).*/\1/p' | head -1
}
token() { sed -n 's/.*"token":"\([^"]*\)".*/\1/p' <<<"$CUERPO"; }

echo; echo "== API sobre la base migrada"
llamar GET /health/db - ""; esperar "GET /health/db" 200
[ "$CODIGO" = 200 ] || { echo "La API no arrancó. Últimas líneas del log:"; tail -15 "$TMP/api.log"; exit 3; }

llamar POST /api/auth/login - '{"email":"dev.estudiante@ucb.edu.bo","password":"Breaku-Dev-2026!"}'; esperar "login del estudiante" 200; TE="$(token)"
llamar POST /api/auth/login - '{"email":"dev.admin@ucb.edu.bo","password":"Breaku-Admin-2026!"}'; esperar "login del admin" 200; TA="$(token)"

llamar GET /api/notificaciones "$TE"; esperar "GET /api/notificaciones (avisos viejos)" 200
n="$(grep -o 'INVITACION_COMUNIDAD\|COMUNIDAD_CERRADA\|PUENTE_DISPONIBLE\|DESPUES_DE_CLASES' <<<"$CUERPO" | sort -u | wc -l)"
[ "$n" -ge 1 ] && ok "los avisos viejos se leen con el esquema nuevo ($n tipos distintos en la respuesta)" || aviso "la respuesta no trae avisos viejos: ${CUERPO:0:120}"
llamar GET /api/reportes/lugares "$TE"; esperar "GET /api/reportes/lugares" 200
llamar GET /api/admin/resumen "$TA"; esperar "GET /api/admin/resumen" 200
llamar GET /api/admin/lugares "$TA"; esperar "GET /api/admin/lugares" 200
llamar GET /api/admin/reportes "$TA"; esperar "GET /api/admin/reportes (reportes viejos del fixture)" 200
IDP="$(id_de '"estado":"PENDIENTE"')"
llamar PATCH "/api/admin/reportes/$IDP" "$TA" '{"estado":"RESUELTO"}'; esperar "PATCH /api/admin/reportes/$IDP (resolver un reporte viejo)" 204
llamar GET /api/admin/reportes "$TA"
IDP="$(id_de '"estado":"PENDIENTE"')"
llamar PATCH "/api/admin/reportes/$IDP" "$TA" '{"estado":"DESCARTADO"}'; esperar "PATCH /api/admin/reportes/$IDP (descartar otro reporte viejo)" 204
llamar GET /api/admin/reportes "$TA"
IDP="$(id_de '"estado":"PENDIENTE"')"
llamar PATCH "/api/admin/reportes/$IDP" "$TA" '{"estado":"RESUELTO"}'; esperar "PATCH /api/admin/reportes/$IDP (resolver el tercero, para poder enviar uno nuevo)" 204
llamar POST /api/reportes "$TE" '{"motivo":"ERROR_APP","descripcion":"Prueba de humo: reporte creado sobre la base migrada."}'; esperar "POST /api/reportes (reporte nuevo)" 201
IDN="$(sed -n 's/.*"id":\([0-9]*\).*/\1/p' <<<"$CUERPO" | head -1)"
llamar PATCH "/api/admin/reportes/$IDN" "$TA" '{"estado":"DESCARTADO"}'; esperar "PATCH /api/admin/reportes/$IDN (descartar el reporte nuevo)" 204
llamar GET /api/admin/auditoria-rag "$TA"; esperar "GET /api/admin/auditoria-rag" 200
llamar GET /api/admin/metricas "$TA"; esperar "GET /api/admin/metricas" 200

llamar POST /api/actividades/extraer "$TE" '{"texto":"Quiero jugar cartas hoy a las 15:00 por una hora con 4 personas"}'
{ [ "$CODIGO" = 200 ] || [ "$CODIGO" = 400 ]; } && ok "POST /api/actividades/extraer (escribe en log_auditoria_rag) → $CODIGO" || mal "POST /api/actividades/extraer → $CODIGO: ${CUERPO:0:160}"

llamar POST /api/admin/lugares "$TA" '{"nombre":"Lugar de humo borrable","descripcion":"Lugar creado por la prueba de humo.","categoriaId":1,"latitud":-17.3716,"longitud":-66.1439,"duracionEstimadaMin":20}'
[ "$CODIGO" = 201 ] || [ "$CODIGO" = 200 ] && ok "POST /api/admin/lugares → $CODIGO (escribe en historial_cambio)" || mal "POST /api/admin/lugares → $CODIGO: ${CUERPO:0:160}"
IDL="$(sed -n 's/.*"id":\([0-9]*\).*/\1/p' <<<"$CUERPO" | head -1)"
if [ -n "$IDL" ]; then llamar DELETE "/api/admin/lugares/$IDL" "$TA"; esperar "DELETE /api/admin/lugares/$IDL (lugar sin uso)" 204; fi

echo; echo "== Efecto conocido (informativo)"
llamar GET /api/admin/lugares "$TA"
LUGAR_EVENTO_ID="${LUGAR_EVENTO_ID:-$(id_de '"nombre":"Sala de Estudio Grupal"')}"
llamar DELETE "/api/admin/lugares/$LUGAR_EVENTO_ID" "$TA"
if [ "$CODIGO" = 500 ]; then
  aviso "DELETE /api/admin/lugares/$LUGAR_EVENTO_ID → 500: ese lugar tiene un evento y LugarEnUsoAsync todavía no cuenta evento.lugar_id (se corrige en HU22-BE, nota en #270)."
else
  aviso "DELETE /api/admin/lugares/$LUGAR_EVENTO_ID → $CODIGO (el lugar tiene un evento apuntándole): ${CUERPO:0:140}"
fi

echo
[ "$FALLOS" = 0 ] && echo "RESULTADO: $PASOS comprobaciones de la API, todas bien." || echo "RESULTADO: $FALLOS de $PASOS comprobaciones fallaron."
exit "$FALLOS"

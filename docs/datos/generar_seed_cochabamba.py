"""Genera docs/database/seed-dev-cochabamba.sql a partir de docs/datos/lugares_ucb_cochabamba.json.

Uso (desde la raíz del repositorio):
    python docs/datos/generar_seed_cochabamba.py

Mapeo de la categoría del relevamiento a las categorías de la app:
    comer              -> Comida
    cafe_te            -> Comida
    estudio            -> Estudio
    descanso           -> Descanso
    deporte_aire_libre -> Deporte
    campus             -> Estudio (son puntos de referencia del campus y no se recomiendan; no se crean categorías nuevas)
"""
import json
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
ENTRADA = RAIZ / "docs" / "datos" / "lugares_ucb_cochabamba.json"
SALIDA = RAIZ / "docs" / "database" / "seed-dev-cochabamba.sql"

SEDE = "Campus UCB Cochabamba"
RADIO_M = 500
CATEGORIAS = {
    "comer": "Comida",
    "cafe_te": "Comida",
    "estudio": "Estudio",
    "descanso": "Descanso",
    "deporte_aire_libre": "Deporte",
    "campus": "Estudio",
}


def texto(valor):
    return "'" + str(valor).replace("\\", "\\\\").replace("'", "''") + "'"


def nulo_o(valor, formato=texto):
    return "NULL" if valor in (None, "", []) else formato(valor)


def lugar(fila, centro):
    categoria = CATEGORIAS[fila["categoria"]]
    recomendable = bool(fila["recomendable"])
    duracion_min = int(fila["duracion_min"])
    duracion_max = int(fila["duracion_max"])
    duracion_estimada = duracion_min if duracion_min > 0 else 1
    actividades = fila["actividades_sugeridas"]
    if recomendable:
        descripcion = "Actividades: " + ", ".join(actividades) + "." if actividades else None
    else:
        descripcion = "Punto de referencia del campus. No se recomienda."
    return (
        "  SELECT "
        + ", ".join(
            [
                f"{texto(fila['nombre'])} AS nombre",
                f"{nulo_o(descripcion)} AS descripcion",
                f"{texto(categoria)} AS categoria",
                f"{fila['lat']:.6f} AS latitud",
                f"{fila['lng']:.6f} AS longitud",
                f"{duracion_estimada} AS duracion",
                f"{nulo_o(fila['tipo_lugar'])} AS tipo_lugar",
                f"{nulo_o(','.join(fila['modalidad']))} AS modalidad",
                f"{nulo_o(','.join(fila['nivel_energia']))} AS nivel_energia",
                f"{nulo_o(duracion_min if duracion_min > 0 else None, str)} AS duracion_min",
                f"{nulo_o(duracion_max if duracion_max > 0 else None, str)} AS duracion_max",
                f"{nulo_o(json.dumps(actividades, ensure_ascii=False) if actividades else None)} AS actividades",
                f"{int(fila['min_a_pie_desde_bloque_a'])} AS min_a_pie",
                f"{'TRUE' if recomendable else 'FALSE'} AS recomendable",
                f"{'TRUE' if fila['validado'] else 'FALSE'} AS validado",
            ]
        )
    )


def main():
    filas = json.loads(ENTRADA.read_text(encoding="utf-8"))
    centro = next(f for f in filas if f["categoria"] == "campus")
    selects = [lugar(f, centro) for f in filas]
    cuerpo = " UNION ALL\n".join(selects)

    sql = f"""-- BREAKU · Seed de DESARROLLO: sede UCB Cochabamba (zona, categoría de referencia y 13 lugares).
-- Generado con docs/datos/generar_seed_cochabamba.py a partir de docs/datos/lugares_ucb_cochabamba.json. No lo edites a mano.
-- SOLO para desarrollo local. NUNCA ejecutar en producción.
--
-- Requisitos: haber ejecutado seed-dev.sql (usa al usuario de prueba como created_by / updated_by) y la
-- migración 005-sede-cochabamba.sql (o haber creado la base con el schema.sql actual).
-- Se ejecuta con la base de datos ya seleccionada (sin USE). Es idempotente: se puede correr varias veces.
--
-- Qué hace:
--   · Crea (o actualiza) la sede «{SEDE}» como sede activa, con un radio de {RADIO_M} m.
--   · Deja las demás sedes (La Paz) guardadas pero inactivas: no aparecen para elegir ni reciben recomendaciones.
--   · No crea categorías: usa Comida, Estudio, Descanso y Deporte (las categorías de la app). Si no existen, las crea.
--   · Carga los lugares con validado = FALSE: el equipo tiene que revisarlos. Los puntos de referencia
--     (campus, parqueo, bloques) quedan con recomendable = FALSE y nunca se recomiendan.
--
-- Para volver a La Paz: UPDATE zona SET activa = (nombre = 'Campus UCB La Paz');

SET @autor := (SELECT id FROM usuario WHERE correo = 'dev.estudiante@ucb.edu.bo');

INSERT INTO zona (nombre, latitud, longitud, radio_max_m, activa, created_by, updated_by)
VALUES ({texto(SEDE)}, {centro['lat']:.6f}, {centro['lng']:.6f}, {RADIO_M}, TRUE, @autor, @autor) AS nueva
ON DUPLICATE KEY UPDATE
  latitud = nueva.latitud, longitud = nueva.longitud, radio_max_m = nueva.radio_max_m,
  activa = TRUE, updated_at = UTC_TIMESTAMP(6);

UPDATE zona SET activa = FALSE, updated_at = UTC_TIMESTAMP(6) WHERE nombre <> {texto(SEDE)} AND activa = TRUE;

INSERT IGNORE INTO categoria (nombre, created_by, updated_by) VALUES
  ('Comida',     @autor, @autor),
  ('Estudio',    @autor, @autor),
  ('Descanso',   @autor, @autor),
  ('Deporte',    @autor, @autor);

INSERT INTO lugar (nombre, descripcion, categoria_id, latitud, longitud, duracion_estimada_min, estado,
                   tipo_lugar, modalidad, nivel_energia, duracion_min, duracion_max, actividades_sugeridas,
                   min_a_pie_desde_bloque_a, recomendable, validado, created_by, updated_by)
SELECT d.nombre, d.descripcion, c.id, d.latitud, d.longitud, d.duracion, 'ACTIVO',
       d.tipo_lugar, d.modalidad, d.nivel_energia, d.duracion_min, d.duracion_max, d.actividades,
       d.min_a_pie, d.recomendable, d.validado, @autor, @autor
FROM (
{cuerpo}
) AS d
JOIN categoria c ON c.nombre = d.categoria
WHERE NOT EXISTS (SELECT 1 FROM lugar l WHERE l.nombre = d.nombre);
"""
    SALIDA.write_text(sql, encoding="utf-8", newline="\n")
    print(f"{len(filas)} lugares -> {SALIDA.relative_to(RAIZ)}")


if __name__ == "__main__":
    sys.exit(main())

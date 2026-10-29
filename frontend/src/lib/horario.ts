import { pedirApi } from "./api";

/** Otro bloque del mismo día que se cruza con éste, asista o no el estudiante. */
export type Coincidencia = { id: number; materia: string; horaInicio: string; horaFin: string; asiste: boolean };

export type Bloque = {
  id: number;
  materia: string;
  diaSemana: number;
  horaInicio: string; // "HH:mm"
  horaFin: string;
  aula: string | null;
  origen: string;
  asiste: boolean;
  autorizacionCarrera: boolean;
  coincide: Coincidencia[];
  advertencias: string[];
};

/** Lo que se envía para crear o editar un bloque. `asiste` y `autorizacionCarrera` se omiten para conservar lo que ya tenía. */
export type BloqueInput = {
  materia: string;
  diaSemana: number;
  horaInicio: string;
  horaFin: string;
  aula: string | null;
  asiste?: boolean;
  autorizacionCarrera?: boolean;
};

/** El estudiante asiste a este bloque y también a otro que se cruza con él. */
export const enChoque = (b: Bloque) => b.asiste && b.coincide.some((c) => c.asiste);

export const listarHorario = () => pedirApi<Bloque[]>("/api/horario");
export const crearBloque = (b: BloqueInput) => pedirApi<Bloque>("/api/horario", { method: "POST", body: JSON.stringify(b) });
export const editarBloque = (id: number, b: BloqueInput) => pedirApi<Bloque>(`/api/horario/${id}`, { method: "PUT", body: JSON.stringify(b) });
export const eliminarBloque = (id: number) => pedirApi<void>(`/api/horario/${id}`, { method: "DELETE" });

/** Marca si el estudiante asiste a un bloque; el backend recalcula los puentes con los bloques a los que asiste. */
export const cambiarAsistencia = (id: number, asiste: boolean) =>
  pedirApi<Bloque>(`/api/horario/${id}/asistencia`, { method: "PATCH", body: JSON.stringify({ asiste }) });

export type CambioAsistencia = { id: number; asiste: boolean; autorizacionCarrera?: boolean };

/** Cambia la asistencia de varios bloques de una vez (el paso «¿a cuáles asistes?» tras importar la foto). */
export const cambiarAsistencias = (bloques: CambioAsistencia[]) =>
  pedirApi<Bloque[]>("/api/horario/asistencia", { method: "PATCH", body: JSON.stringify({ bloques }) });

/** Foto del horario (HU2): el backend la lee con un modelo de visión y devuelve las clases sin guardarlas. */
export type ClaseLeida = Pick<BloqueInput, "materia" | "diaSemana" | "horaInicio" | "horaFin" | "aula">;
export type LecturaFoto = { clases: ClaseLeida[]; descartadas: number };
export type ResultadoLote = { guardados: Bloque[]; rechazados: { indice: number; materia: string; motivo: string }[] };

export const FOTO_BYTES_MAXIMOS = 8 * 1024 * 1024;
export const FOTO_TIPOS = ["image/jpeg", "image/png", "image/webp"];

export function leerFotoHorario(foto: File) {
  const datos = new FormData();
  datos.append("foto", foto);
  return pedirApi<LecturaFoto>("/api/horario/foto", { method: "POST", body: datos });
}

/** Guarda las clases confirmadas con origen FOTO; las que coinciden con otra se guardan igual y traen sus coincidencias. */
export const guardarLote = (bloques: ClaseLeida[]) =>
  pedirApi<ResultadoLote>("/api/horario/lote", { method: "POST", body: JSON.stringify({ bloques }) });

const aMinutos = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

/**
 * Reparte los bloques de un día en columnas para dibujar lado a lado los que se cruzan.
 * Devuelve, por id, la columna del bloque y cuántas columnas tiene su grupo de solapes.
 */
export function columnasPorSolape(bloques: Bloque[]): Map<number, { columna: number; columnas: number }> {
  const resultado = new Map<number, { columna: number; columnas: number }>();
  const ordenados = [...bloques].sort((a, b) => aMinutos(a.horaInicio) - aMinutos(b.horaInicio) || aMinutos(b.horaFin) - aMinutos(a.horaFin));

  let grupo: { bloque: Bloque; columna: number }[] = [];
  let finGrupo = -1;
  const cerrar = () => {
    const columnas = Math.max(1, ...grupo.map((g) => g.columna + 1));
    for (const g of grupo) resultado.set(g.bloque.id, { columna: g.columna, columnas });
    grupo = [];
  };

  for (const b of ordenados) {
    if (grupo.length > 0 && aMinutos(b.horaInicio) >= finGrupo) cerrar();
    const ocupadas = new Set(grupo.filter((g) => aMinutos(g.bloque.horaFin) > aMinutos(b.horaInicio)).map((g) => g.columna));
    let columna = 0;
    while (ocupadas.has(columna)) columna++;
    grupo.push({ bloque: b, columna });
    finGrupo = grupo.length === 1 ? aMinutos(b.horaFin) : Math.max(finGrupo, aMinutos(b.horaFin));
  }
  if (grupo.length > 0) cerrar();
  return resultado;
}

export type OpcionAsistencia = { id: number; materia: string; diaSemana: number; horaInicio: string; horaFin: string; asiste: boolean };

/**
 * Agrupa los bloques recién guardados que coinciden entre sí (y con clases que ya tenías), para preguntar a cuáles
 * asiste el estudiante. Cada grupo es un conjunto de clases del mismo día que se cruzan; los grupos de una sola clase no se incluyen.
 */
export function gruposEnChoque(guardados: Bloque[]): OpcionAsistencia[][] {
  const padre = new Map<number, number>();
  const raiz = (id: number): number => {
    const p = padre.get(id) ?? id;
    if (p === id) return id;
    const r = raiz(p);
    padre.set(id, r);
    return r;
  };
  const unir = (a: number, b: number) => padre.set(raiz(a), raiz(b));

  const opciones = new Map<number, OpcionAsistencia>();
  for (const b of guardados) {
    if (!enChoque(b)) continue;
    opciones.set(b.id, { id: b.id, materia: b.materia, diaSemana: b.diaSemana, horaInicio: b.horaInicio, horaFin: b.horaFin, asiste: b.asiste });
    for (const c of b.coincide) {
      if (!c.asiste) continue;
      if (!opciones.has(c.id)) opciones.set(c.id, { id: c.id, materia: c.materia, diaSemana: b.diaSemana, horaInicio: c.horaInicio, horaFin: c.horaFin, asiste: c.asiste });
      unir(b.id, c.id);
    }
  }

  const grupos = new Map<number, OpcionAsistencia[]>();
  for (const o of opciones.values()) {
    const r = raiz(o.id);
    grupos.set(r, [...(grupos.get(r) ?? []), o]);
  }
  return [...grupos.values()]
    .filter((g) => g.length > 1)
    .map((g) => g.sort((a, b) => a.horaInicio.localeCompare(b.horaInicio) || a.materia.localeCompare(b.materia)))
    .sort((a, b) => a[0].diaSemana - b[0].diaSemana || a[0].horaInicio.localeCompare(b[0].horaInicio));
}

/** Minutos de clase por semana: es la unión de lo ocupado en cada día, solo con los bloques a los que asiste. */
export function minutosOcupados(bloques: Bloque[]): number {
  let total = 0;
  for (let dia = 1; dia <= 6; dia++) {
    const tramos = bloques
      .filter((b) => b.asiste && b.diaSemana === dia)
      .map((b) => [aMinutos(b.horaInicio), aMinutos(b.horaFin)] as const)
      .sort((a, b) => a[0] - b[0]);
    let fin = -1;
    for (const [ini, f] of tramos) {
      if (ini >= fin) total += f - ini;
      else if (f > fin) total += f - fin;
      fin = Math.max(fin, f);
    }
  }
  return total;
}

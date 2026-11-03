import { pedirApi } from "./api";

export type LugarViable = {
  lugarId: number;
  nombre: string;
  categoria: string;
  descripcion: string | null;
  distanciaM: number;
  tiempoTrasladoMin: number;
  duracionEstimadaMin: number;
  validado: boolean;
  tipoLugar: string | null;
  modalidad: string[];
  nivelEnergia: string[];
  actividadesSugeridas: string[];
};

export type ResultadoLugares = {
  estado: "OK" | "SIN_RESULTADOS" | "SIN_ZONA";
  mensaje: string | null;
  zona: { id: number; nombre: string } | null;
  radioAplicadoM: number | null;
  minutosDisponibles: number;
  lugares: LugarViable[];
};

const MENSAJE_SIN_RESULTADOS = "No encontramos opciones cercanas para este tiempo disponible.";

export const lugarTieneMetricasCompletas = (l: LugarViable) =>
  Boolean(l.nombre?.trim()) &&
  Boolean(l.categoria?.trim()) &&
  Number.isFinite(l.distanciaM) && l.distanciaM >= 0 &&
  Number.isFinite(l.tiempoTrasladoMin) && l.tiempoTrasladoMin >= 0 &&
  Number.isFinite(l.duracionEstimadaMin) && l.duracionEstimadaMin > 0;

const soloConMetricas = (r: ResultadoLugares): ResultadoLugares => {
  if (r.estado !== "OK") return r;
  const lugares = r.lugares.filter(lugarTieneMetricasCompletas);
  return lugares.length > 0
    ? { ...r, lugares }
    : { ...r, estado: "SIN_RESULTADOS", mensaje: MENSAJE_SIN_RESULTADOS, lugares: [] };
};

export const buscarLugaresViables = (puenteId: number) =>
  pedirApi<ResultadoLugares>(`/api/lugares/viables?puenteId=${puenteId}`).then(soloConMetricas);
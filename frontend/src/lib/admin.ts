import { pedirApi } from "./api";

/** Panel de administración del campus (rol ADMIN). El backend vuelve a comprobar el rol en cada endpoint. */
export type EstadoLugar = "ACTIVO" | "EN_REVISION" | "INACTIVO";

export type ResumenAdmin = {
  lugaresActivos: number;
  lugaresEnRevision: number;
  estudiantes: number;
  comunidadesHoy: number;
  consultasRagSemana: number;
  reportesPendientes: number;
};

export type LugarAdmin = {
  id: number;
  nombre: string;
  categoria: string;
  estado: EstadoLugar;
  duracionEstimadaMin: number;
  horaApertura: string | null;
  horaCierre: string | null;
  vecesRecomendado: number;
};

export type ReporteAdmin = {
  id: number;
  motivo: string;
  descripcion: string | null;
  estado: "PENDIENTE" | "RESUELTO" | "DESCARTADO";
  objeto: string;
  autor: string;
  creadoEn: string;
};

export type ConsultaRag = {
  id: number;
  fecha: string;
  usuario: string;
  tipo: string;
  entrada: string | null;
  estado: "OK" | "SIN_RESULTADOS" | "RECHAZADO_SEGURIDAD" | "ERROR";
  modelo: string | null;
  latenciaMs: number | null;
  lugaresRecuperados: number;
  recomendados: number;
};

export type MetricasAdmin = {
  consultasTotales: number;
  ok: number;
  sinResultados: number;
  rechazadas: number;
  errores: number;
  latenciaPromedioMs: number | null;
  masRecomendados: { nombre: string; categoria: string; veces: number }[];
};

export const obtenerResumen = () => pedirApi<ResumenAdmin>("/api/admin/resumen");
export const listarLugaresAdmin = () => pedirApi<LugarAdmin[]>("/api/admin/lugares");
export const cambiarEstadoLugar = (id: number, estado: EstadoLugar) =>
  pedirApi<void>(`/api/admin/lugares/${id}/estado`, { method: "PATCH", body: JSON.stringify({ estado }) });
export const listarReportes = () => pedirApi<ReporteAdmin[]>("/api/admin/reportes");
export const listarConsultasRag = (limite = 50) => pedirApi<ConsultaRag[]>(`/api/admin/auditoria-rag?limite=${limite}`);
export const obtenerMetricas = () => pedirApi<MetricasAdmin>("/api/admin/metricas");

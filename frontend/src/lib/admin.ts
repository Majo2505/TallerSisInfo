import { pedirApi } from "./api";

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
  validado: boolean;
  recomendable: boolean;
  tipoLugar: string | null;
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

export type MotivoRechazo = "INYECCION" | "FUERA_DE_ALCANCE" | "BIENESTAR" | "FUERA_DE_REGLAMENTO" | "FUERA_DE_CONTEXTO";

export type ConsultaRag = {
  id: number;
  fecha: string;
  usuario: string;
  tipo: string;
  entrada: string | null;
  estado: "OK" | "SIN_RESULTADOS" | "RECHAZADO_SEGURIDAD" | "ERROR" | "INCOMPLETO";
  modelo: string | null;
  latenciaMs: number | null;
  lugaresRecuperados: number;
  recomendados: number;
  motivoRechazo?: MotivoRechazo | null;
};

export type MetricasAdmin = {
  consultasTotales: number;
  ok: number;
  sinResultados: number;
  rechazadas: number;
  errores: number;
  latenciaPromedioMs: number | null;
  masRecomendados: { nombre: string; categoria: string; veces: number; meGusta: number; noMeSirve: number }[];
  meGusta: number;
  noMeSirve: number;
  rechazosPorMotivo?: { motivo: MotivoRechazo | "SIN_MOTIVO"; cantidad: number }[];
};

export const obtenerResumen = () => pedirApi<ResumenAdmin>("/api/admin/resumen");
export const listarLugaresAdmin = () => pedirApi<LugarAdmin[]>("/api/admin/lugares");
export const cambiarEstadoLugar = (id: number, estado: EstadoLugar) =>
  pedirApi<void>(`/api/admin/lugares/${id}/estado`, { method: "PATCH", body: JSON.stringify({ estado }) });
export const cambiarValidacionLugar = (id: number, validado: boolean) =>
  pedirApi<void>(`/api/admin/lugares/${id}/validacion`, { method: "PATCH", body: JSON.stringify({ validado }) });
export const listarReportes = () => pedirApi<ReporteAdmin[]>("/api/admin/reportes");
export const resolverReporte = (id: number, estado: "RESUELTO" | "DESCARTADO") =>
  pedirApi<void>(`/api/admin/reportes/${id}`, { method: "PATCH", body: JSON.stringify({ estado }) });
export const listarConsultasRag = (limite = 50) => pedirApi<ConsultaRag[]>(`/api/admin/auditoria-rag?limite=${limite}`);
export const obtenerMetricas = () => pedirApi<MetricasAdmin>("/api/admin/metricas");

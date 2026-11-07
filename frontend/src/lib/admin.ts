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
// ---------- Catálogo: crear, editar y eliminar lugares ----------

export type CategoriaAdmin = { id: number; nombre: string };

/** Campus activo: un lugar tiene que quedar dentro de su radio. */
export type CampusAdmin = { id: number; nombre: string; latitud: number; longitud: number; radioMaxM: number };

/** Lo que se envía al crear o editar un lugar. Las horas en "HH:mm"; apertura y cierre van juntas o ninguna. */
export type GuardarLugar = {
  nombre: string;
  descripcion: string | null;
  categoriaId: number;
  latitud: number;
  longitud: number;
  duracionEstimadaMin: number;
  horaApertura: string | null;
  horaCierre: string | null;
  minAPieDesdeBloqueA: number | null;
};

export type LugarDetalleAdmin = GuardarLugar & { id: number; estado: EstadoLugar; validado: boolean; fotoUrl: string | null };

export const listarCategoriasAdmin = () => pedirApi<CategoriaAdmin[]>("/api/admin/categorias");
export const listarCampusAdmin = () => pedirApi<CampusAdmin[]>("/api/admin/campus");
export const obtenerLugarAdmin = (id: number) => pedirApi<LugarDetalleAdmin>(`/api/admin/lugares/${id}`);
export const crearLugarAdmin = (lugar: GuardarLugar) =>
  pedirApi<{ id: number }>("/api/admin/lugares", { method: "POST", body: JSON.stringify(lugar) });
export const editarLugarAdmin = (id: number, lugar: GuardarLugar) =>
  pedirApi<void>(`/api/admin/lugares/${id}`, { method: "PUT", body: JSON.stringify(lugar) });
/** Sube o reemplaza la foto del lugar (ya achicada con `achicarFoto`). */
export function subirFotoLugarAdmin(id: number, foto: Blob) {
  const datos = new FormData();
  datos.append("foto", foto, foto.type === "image/webp" ? "foto.webp" : "foto.jpg");
  return pedirApi<{ fotoUrl: string }>(`/api/admin/lugares/${id}/foto`, { method: "PUT", body: datos });
}
export const quitarFotoLugarAdmin = (id: number) => pedirApi<void>(`/api/admin/lugares/${id}/foto`, { method: "DELETE" });
/** Solo si nunca se usó; si no, responde EN_USO y hay que desactivarlo. */
export const eliminarLugarAdmin = (id: number) => pedirApi<void>(`/api/admin/lugares/${id}`, { method: "DELETE" });

/** Cómo se lee el motivo de cada reporte en el panel. */
export const MOTIVO_REPORTE: Record<string, string> = {
  ERROR_APP: "Algo no funciona",
  SUGERENCIA: "Sugerencia",
  LUGAR_INCORRECTO: "Información incorrecta",
  COMUNIDAD_INAPROPIADA: "Comunidad inapropiada",
};

export const listarReportes = () => pedirApi<ReporteAdmin[]>("/api/admin/reportes");
export const resolverReporte = (id: number, estado: "RESUELTO" | "DESCARTADO") =>
  pedirApi<void>(`/api/admin/reportes/${id}`, { method: "PATCH", body: JSON.stringify({ estado }) });
export const listarConsultasRag = (limite = 50) => pedirApi<ConsultaRag[]>(`/api/admin/auditoria-rag?limite=${limite}`);
export const obtenerMetricas = () => pedirApi<MetricasAdmin>("/api/admin/metricas");

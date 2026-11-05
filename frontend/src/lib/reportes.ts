import { pedirApi } from "./api";

/** Reportes que envía el estudiante desde su perfil; los revisa un administrador en el panel. */
export type MotivoReporte = "ERROR_APP" | "SUGERENCIA" | "LUGAR_INCORRECTO";

export type LugarReportable = { id: number; nombre: string };

export const LARGO_MINIMO_REPORTE = 10;
export const LARGO_MAXIMO_REPORTE = 500;

export const listarLugaresReportables = () => pedirApi<LugarReportable[]>("/api/reportes/lugares");

/** El lugar va solo con LUGAR_INCORRECTO. */
export const enviarReporte = (motivo: MotivoReporte, descripcion: string, lugarId: number | null) =>
  pedirApi<{ id: number; estado: string }>("/api/reportes", {
    method: "POST",
    body: JSON.stringify({ motivo, lugarId, descripcion }),
  });

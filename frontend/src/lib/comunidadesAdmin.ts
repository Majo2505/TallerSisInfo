import { pedirApi } from "./api";

/** HU35: una comunidad en el panel del admin. `inicio`, `fin` y `fechaBaja` en hora de Bolivia. */
export type ComunidadAdmin = {
  id: number;
  actividad: string;
  creadorId: number;
  creador: string;
  inicio: string;
  fin: string;
  confirmados: number;
  maxParticipantes: number;
  privada: boolean;
  estado: "ACTIVA" | "BAJADA";
  reportesPendientes: number;
  motivoBaja: string | null;
  fechaBaja: string | null;
};

export const listarComunidadesAdmin = (estado: "ACTIVA" | "BAJADA") =>
  pedirApi<ComunidadAdmin[]>(`/api/admin/comunidades?estado=${estado}`);

/** Quita una comunidad activa (queda oculta, no se borra y no sanciona). Con `reporteId`, ese reporte queda resuelto. */
export const quitarComunidad = (id: number, motivo: string, reporteId?: number | null) =>
  pedirApi<void>(`/api/admin/comunidades/${id}/baja`, {
    method: "PATCH",
    body: JSON.stringify({ motivo: motivo.trim(), reporteId: reporteId ?? null }),
  });

export const restaurarComunidad = (id: number) =>
  pedirApi<void>(`/api/admin/comunidades/${id}/restauracion`, { method: "PATCH" });

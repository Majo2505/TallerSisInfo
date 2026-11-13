import { pedirApi } from "./api";
import type { EstadoUsuario, TipoSancion } from "./reglasUsuarios";

/** HU28: gestión de usuarios y sanciones (solo ADMIN). Contratos de /api/admin/usuarios. */
export type UsuarioLista = {
  id: number;
  nombre: string;
  correo: string;
  carrera: string | null;
  estado: EstadoUsuario;
  /** Total aplicado (historial). */
  sanciones: number;
  /** Advertencias que cuentan para las 3: las levantadas no. */
  advertencias: number;
};

export type Sancion = {
  id: number;
  tipo: TipoSancion;
  motivo: string;
  reporteId: number | null;
  creadoPor: string;
  creadoEn: string;
  hasta: string | null;
  levantadaEn: string | null;
  levantadaPor: string | null;
  vigente: boolean;
};

export type UsuarioDetalle = {
  id: number;
  nombre: string;
  correo: string;
  carrera: string | null;
  semestre: number | null;
  estado: EstadoUsuario;
  registradoEn: string;
  advertencias: number;
  siguienteSancionSugerida: TipoSancion;
  advertenciasParaSuspension: number;
  diasSuspensionPorDefecto: number;
  bloqueo: { tipo: string; motivo: string; hasta: string | null } | null;
  sanciones: Sancion[];
};

export type SancionAplicada = {
  sancion: Sancion;
  estadoUsuario: EstadoUsuario;
  advertencias: number;
  siguienteSancionSugerida: TipoSancion;
};

export const listarUsuarios = (buscar: string) =>
  pedirApi<UsuarioLista[]>(`/api/admin/usuarios${buscar.trim() ? `?buscar=${encodeURIComponent(buscar.trim())}` : ""}`);

export const obtenerUsuario = (id: number) => pedirApi<UsuarioDetalle>(`/api/admin/usuarios/${id}`);

export const sancionarUsuario = (id: number, tipo: TipoSancion, motivo: string, hasta: string | null, reporteId: number | null = null) =>
  pedirApi<SancionAplicada>(`/api/admin/usuarios/${id}/sanciones`, {
    method: "POST",
    body: JSON.stringify({ tipo, motivo: motivo.trim(), hasta, reporteId }),
  });

export const levantarSancion = (usuarioId: number, sancionId: number) =>
  pedirApi<Sancion>(`/api/admin/usuarios/${usuarioId}/sanciones/${sancionId}/levantar`, { method: "POST" });

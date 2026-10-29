import { pedirApi } from "./api";

export type Zona = { id: number; nombre: string };
export type EstadoUbicacion = {
  consentimientoActivo: boolean;
  zonaActual: Zona | null;
};

export const obtenerEstadoUbicacion = () => pedirApi<EstadoUbicacion>("/api/ubicacion");
export const listarZonas = () => pedirApi<Zona[]>("/api/ubicacion/zonas");
export const compartirUbicacion = (latitud: number, longitud: number) =>
  pedirApi<EstadoUbicacion>("/api/ubicacion", {
    method: "PUT",
    body: JSON.stringify({ latitud, longitud }),
  });
export const seleccionarZona = (zonaId: number) =>
  pedirApi<EstadoUbicacion>("/api/ubicacion/zona", {
    method: "PUT",
    body: JSON.stringify({ zonaId }),
  });
export const revocarUbicacion = () => pedirApi<void>("/api/ubicacion", { method: "DELETE" });

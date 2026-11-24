export const CLAVE_AVISO_UBICACION = "breaku:aviso-ubicacion-visto";

type EstadoParaAviso = {
  consentimientoActivo: boolean;
  zonaActual?: { id: number } | null;
} | null;

/**
 * HU5: el aviso aparece en el primer acceso mientras no haya zona elegida ni consentimiento y no se haya descartado.
 * Nunca llama a la geolocalización: solo invita a decidir en la pantalla de configuración.
 */
export function debeMostrarAvisoUbicacion(estado: EstadoParaAviso, visto: boolean): boolean {
  return estado !== null && !estado.consentimientoActivo && !estado.zonaActual && !visto;
}

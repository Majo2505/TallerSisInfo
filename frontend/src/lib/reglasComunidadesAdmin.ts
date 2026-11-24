/** HU35: solo se restaura una comunidad quitada mientras no haya terminado (`fin` llega en hora de Bolivia, con desfase). */
export function puedeRestaurarComunidad(finIso: string, ahora: Date): boolean {
  const fin = new Date(finIso).getTime();
  return Number.isFinite(fin) && fin > ahora.getTime();
}

export const TEXTO_COMUNIDAD_TERMINADA = "Ya terminó: no hay nada que restaurar.";

export type AccionValidacionLugar = "validar" | "desvalidar";

export const ETIQUETA_ACCION_VALIDACION: Record<AccionValidacionLugar, string> = {
  validar: "Validar",
  desvalidar: "Desvalidar",
};

/** HU16: el admin puede quitar la validación de un lugar que antes aprobó; la API acepta ambos valores. */
export function accionValidacionLugar(validado: boolean): AccionValidacionLugar {
  return validado ? "desvalidar" : "validar";
}

/** HU16: solo quitar la validación pide confirmación; validar se hace de una vez. */
export function pideConfirmacionValidacion(accion: AccionValidacionLugar): boolean {
  return accion === "desvalidar";
}

export const TEXTO_CONFIRMAR_DESVALIDAR = "Al quitar la validación, los eventos nuevos en este lugar quedarán en revisión.";
export const ETIQUETA_CONFIRMAR_DESVALIDAR = "Quitar validación";

export const tituloConfirmarDesvalidar = (nombre: string): string => `¿Quitar la validación de «${nombre}»?`;

export function mensajeValidacionLugar(nombre: string, validado: boolean): string {
  return `${nombre}: ${validado ? "validado" : "sin validar"}.`;
}

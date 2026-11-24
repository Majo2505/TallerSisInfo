export type AccionValidacionLugar = "validar" | "desvalidar";

export const ETIQUETA_ACCION_VALIDACION: Record<AccionValidacionLugar, string> = {
  validar: "Validar",
  desvalidar: "Desvalidar",
};

/** HU16: el admin puede quitar la validación de un lugar que antes aprobó; la API acepta ambos valores. */
export function accionValidacionLugar(validado: boolean): AccionValidacionLugar {
  return validado ? "desvalidar" : "validar";
}

export function mensajeValidacionLugar(nombre: string, validado: boolean): string {
  return `${nombre}: ${validado ? "validado" : "sin validar"}.`;
}

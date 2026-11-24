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

/**
 * HU35 y HU38: desde un reporte pendiente el admin puede quitar la comunidad o bajar el evento reportado (con motivo
 * obligatorio, el reporte queda resuelto y no se sanciona a nadie). «Bajar evento» solo si el evento sigue PUBLICADO.
 */
export type AccionModeracionReporte = "quitar" | "bajar";

export function accionModeracionDeReporte(r: {
  estado: string;
  objetoTipo?: string | null;
  objetoId?: number | null;
  objetoEstado?: string | null;
}): AccionModeracionReporte | null {
  if (r.estado !== "PENDIENTE" || !r.objetoId) return null;
  if (r.objetoTipo === "COMUNIDAD") return "quitar";
  if (r.objetoTipo === "EVENTO" && r.objetoEstado === "PUBLICADO") return "bajar";
  return null;
}

export const TEXTOS_MODERACION_REPORTE: Record<
  AccionModeracionReporte,
  { boton: string; etiquetaMotivo: string; pista: string; ayuda: string; confirmar: string }
> = {
  quitar: {
    boton: "Quitar comunidad",
    etiquetaMotivo: "Motivo para quitar la comunidad",
    pista: "(obligatorio; le llega a quien la creó y a quienes iban)",
    ayuda: "Queda oculta sin borrarse, el reporte se marca como resuelto y no sanciona a quien la creó.",
    confirmar: "Confirmar y quitar",
  },
  bajar: {
    boton: "Bajar evento",
    etiquetaMotivo: "Motivo para bajar el evento",
    pista: "(obligatorio; se lo mandamos a quien lo creó)",
    ayuda: "Queda oculto sin borrarse, el reporte se marca como resuelto y no sanciona a quien lo creó.",
    confirmar: "Confirmar y bajar",
  },
};

export function mensajeValidacionLugar(nombre: string, validado: boolean): string {
  return `${nombre}: ${validado ? "validado" : "sin validar"}.`;
}

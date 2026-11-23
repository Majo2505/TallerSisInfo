import { enlaceMisReportes } from "./reglasReportes.ts";

// Cómo se ve cada tipo de notificación en la campana. Función pura, sin React, para poder probarla.
// Un tipo que la campana no conoce (por ejemplo uno que agregue otra HU) usa una presentación genérica
// en vez de romper el panel.

export type IconoNotificacion = "rayo" | "personas" | "bandera" | "calendario";
export type ClaseNotificacion = "puente" | "invitacion" | "cerrada" | "advertencia" | "reporte";

export type PresentacionNotificacion = {
  titulo: string;
  clase: ClaseNotificacion;
  icono: IconoNotificacion;
};

const PRESENTACIONES: Record<string, PresentacionNotificacion> = {
  PUENTE_DISPONIBLE: { titulo: "Puente disponible", clase: "puente", icono: "rayo" },
  DESPUES_DE_CLASES: { titulo: "Terminaste tus clases", clase: "puente", icono: "rayo" },
  INVITACION_COMUNIDAD: { titulo: "Invitación a una comunidad", clase: "invitacion", icono: "personas" },
  COMUNIDAD_CERRADA: { titulo: "El grupo se llenó", clase: "cerrada", icono: "personas" },
  // Una conexión de «Libres contigo» te invita a un evento de hoy. Lleva al detalle del evento.
  INVITACION_EVENTO: { titulo: "Te invitan a un evento", clase: "invitacion", icono: "calendario" },
  // Para quien creó la comunidad: alguien se sumó o se bajó. Lleva a la comunidad, donde está «Quiénes van».
  ASISTENCIA_COMUNIDAD: { titulo: "Cambió quién va a tu grupo", clase: "invitacion", icono: "personas" },
  // HU28: la advertencia del administrador. No lleva a otra pantalla: el aviso ya dice el motivo.
  SANCION_ADVERTENCIA: { titulo: "Advertencia del administrador", clase: "advertencia", icono: "bandera" },
  // HU12: saludos de «Libres contigo». Llevan a esa pestaña, donde se responden o se ve el correo.
  CONEXION_SOLICITADA: { titulo: "Alguien quiere conocerte", clase: "invitacion", icono: "personas" },
  CONEXION_ACEPTADA: { titulo: "Aceptaron tu saludo", clase: "invitacion", icono: "personas" },
  // HU27: un administrador revisó tu reporte. Lleva a «Mis reportes», donde está la respuesta completa.
  REPORTE_RESUELTO: { titulo: "Revisamos tu reporte", clase: "reporte", icono: "bandera" },
  // HU23: el administrador revisó un evento pendiente. Lleva a «Mis eventos».
  EVENTO_APROBADO: { titulo: "Tu evento fue aprobado", clase: "reporte", icono: "calendario" },
  EVENTO_RECHAZADO: { titulo: "Tu evento fue rechazado", clase: "advertencia", icono: "calendario" },
  // HU26: el administrador bajó un evento que ya estaba publicado. El mensaje trae el motivo.
  EVENTO_BAJADO: { titulo: "Tu evento fue retirado", clase: "advertencia", icono: "calendario" },
  // Campanita del admin: algo espera su decisión. Llevan a la pestaña o pantalla donde se resuelve.
  REPORTE_NUEVO: { titulo: "Nuevo reporte", clase: "reporte", icono: "bandera" },
  EVENTO_POR_REVISAR: { titulo: "Evento por revisar", clase: "invitacion", icono: "calendario" },
};

export const PRESENTACION_GENERICA: PresentacionNotificacion = {
  titulo: "Nuevo aviso",
  clase: "cerrada",
  icono: "rayo",
};

export function presentacionNotificacion(tipo: string): PresentacionNotificacion {
  return Object.hasOwn(PRESENTACIONES, tipo) ? PRESENTACIONES[tipo] : PRESENTACION_GENERICA;
}

type AvisoConDestino = {
  tipo: string;
  duracionMinutos?: number | null;
  comunidadId?: number | null;
  reporteId?: number | null;
  eventoId?: number | null;
};

/** A dónde lleva «Ver» (o «Revisar», en el admin). `null` = el aviso no lleva a ninguna pantalla. */
export function rutaNotificacion(n: AvisoConDestino): string | null {
  switch (n.tipo) {
    case "SANCION_ADVERTENCIA":
      return null;
    case "REPORTE_NUEVO":
      return "/admin?pestana=reportes";
    case "EVENTO_POR_REVISAR":
      return "/admin/eventos";
    case "REPORTE_RESUELTO":
      return enlaceMisReportes(n.reporteId);
    case "INVITACION_EVENTO":
      return n.eventoId ? `/eventos/${n.eventoId}` : "/eventos";
    case "EVENTO_APROBADO":
    case "EVENTO_RECHAZADO":
    case "EVENTO_BAJADO":
      return "/eventos?pestana=mios";
    case "CONEXION_SOLICITADA":
    case "CONEXION_ACEPTADA":
      return "/comunidades?pestana=libres";
    case "PUENTE_DISPONIBLE":
    case "DESPUES_DE_CLASES":
      return n.duracionMinutos ? `/recomendaciones?min=${n.duracionMinutos}` : "/recomendaciones";
    default:
      return n.comunidadId ? `/comunidades?pestana=descubrir&comunidad=${n.comunidadId}` : "/comunidades?pestana=descubrir";
  }
}

/** Texto de la bandeja vacía: distinto si es la pestaña «Sin leer» y si quien mira es el administrador. */
export function bandejaVacia(filtro: "todas" | "sin-leer", admin: boolean): { titulo: string; detalle: string } {
  if (filtro === "sin-leer")
    return {
      titulo: "Ya leíste todo",
      detalle: "Cada aviso nuevo aparece aquí hasta que lo abras o lo marques como leído.",
    };
  return admin
    ? { titulo: "Sin avisos por ahora", detalle: "Te avisaremos cuando un estudiante envíe un reporte o un evento espere tu revisión." }
    : { titulo: "Sin avisos por ahora", detalle: "Te avisaremos en cuanto empiece uno de tus puentes." };
}

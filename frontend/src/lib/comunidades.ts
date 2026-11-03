import { pedirApi } from "./api";

/**
 * HU13: armado automático de la comunidad. Cuando se crea la actividad (HU10), el backend busca a los compañeros de tu
 * campus libres a esa hora (las reglas del matching de HU12), invita a los más compatibles (uno por lugar libre) y les avisa.
 * El resultado llega en `invitacion` de la actividad creada.
 */
export type Invitado = {
  nombre: string;
  carrera: string | null;
  libreDesde: string; // "HH:mm", hora de Bolivia
  libreHasta: string;
  minutosEnComun: number;
};

export type EstadoInvitacion = "INVITADOS" | "SIN_COINCIDENCIAS" | "SIN_ZONA" | "NO_VISIBLE" | "COMPLETA" | "ERROR";

export type Invitacion = {
  estado: EstadoInvitacion;
  mensaje: string;
  invitados: Invitado[]; // los de este intento
};

/** Vuelve a buscar compañeros para tu actividad (después de aceptar el matching o si antes no había nadie). */
export const buscarDeNuevo = (comunidadId: number) =>
  pedirApi<Invitacion>(`/api/comunidades/${comunidadId}/invitaciones`, { method: "POST" });

/** Una comunidad activa de hoy (Comunidades → Descubrir). Las horas son de hoy, en hora de Bolivia. */
export type ComunidadActiva = {
  id: number;
  actividad: string;
  horaInicio: string; // "HH:mm"
  horaFin: string;
  maxParticipantes: number;
  confirmados: number; // contando a quien la creó
  creador: string; // «Ana P.» o «Tú»
  esMia: boolean;
  miEstado: "PENDIENTE" | "CONFIRMADO" | "RECHAZADO" | null; // si me invitaron
};

export type ResultadoDescubrir = {
  estado: "OK" | "SIN_RESULTADOS" | "SIN_ZONA" | "NO_VISIBLE";
  mensaje: string | null; // solo si el estado no es OK
  comunidades: ComunidadActiva[];
};

/**
 * Comunidades activas de hoy: las mías, aquellas a las que me invitaron y las de mi campus (si acepté el matching,
 * igual que las invitaciones de HU13). Solo lectura: unirse o confirmar asistencia llega con HU14.
 */
export const listarComunidades = () => pedirApi<ResultadoDescubrir>("/api/comunidades");

/** Solo para el dibujo de la tarjeta: la categoría que sugiere el texto de la actividad. */
export function categoriaDe(actividad: string): "Comida" | "Estudio" | "Descanso" | "Deporte" {
  const texto = actividad.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
  if (/cafe|almorz|comer|merend|desayun|cena|snack|\bte\b/.test(texto)) return "Comida";
  if (/estudi|repas|tarea|leer|biblioteca|practic|proyecto/.test(texto)) return "Estudio";
  if (/futbol|fulbito|basquet|voley|cancha|deporte|correr|ping ?pong|ajedrez|cartas|jugar/.test(texto)) return "Deporte";
  return "Descanso";
}

export const EJEMPLOS_SOLICITUD = [
  "¿Alguien libre para un café en 20 minutos?",
  "Busco compañeros para estudiar Redes en la biblioteca",
  "¿Quién quiere ir a la cancha ahorita?",
  "Grupo para almorzar en la Cafetería Central",
];

export const MAXIMO_SOLICITUD = 200;

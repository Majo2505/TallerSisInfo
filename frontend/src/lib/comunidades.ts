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

/** Descubrir todavía usa datos de ejemplo (llega con su historia de usuario). */
export type Comunidad = {
  id: number;
  actividad: string;
  lugar: string;
  categoria: "Comida" | "Estudio" | "Descanso" | "Deporte";
  inicio: string; // "10:30"
  fin: string;
  participantes: string[]; // nombres de pila
  maximo: number;
  creador: string;
};

export const COMUNIDADES_EJEMPLO: Comunidad[] = [
  { id: 1, actividad: "Estudiar para el parcial de Redes", lugar: "Biblioteca Norte", categoria: "Estudio", inicio: "10:30", fin: "12:00", participantes: ["Carlos", "Ana", "Luis"], maximo: 5, creador: "Carlos R." },
  { id: 2, actividad: "Café y plática entre clases", lugar: "Cafetería Central", categoria: "Comida", inicio: "11:00", fin: "11:45", participantes: ["Sofía", "Diego"], maximo: 4, creador: "Sofía M." },
  { id: 3, actividad: "Partido rápido de fútbol", lugar: "Cancha Polideportiva", categoria: "Deporte", inicio: "13:00", fin: "14:00", participantes: ["Marco", "Iván", "Pablo", "Rocío", "Jorge"], maximo: 10, creador: "Marco T." },
  { id: 4, actividad: "Descanso al aire libre", lugar: "Jardín de las Ciencias", categoria: "Descanso", inicio: "15:30", fin: "16:15", participantes: ["Valeria"], maximo: 6, creador: "Valeria P." },
];

export const EJEMPLOS_SOLICITUD = [
  "¿Alguien libre para un café en 20 minutos?",
  "Busco compañeros para estudiar Redes en la biblioteca",
  "¿Quién quiere ir a la cancha ahorita?",
  "Grupo para almorzar en la Cafetería Central",
];

export const MAXIMO_SOLICITUD = 200;

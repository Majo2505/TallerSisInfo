import { pedirApi } from "./api";

export type Invitado = {
  nombre: string;
  carrera: string | null;
  libreDesde: string;
  libreHasta: string;
  minutosEnComun: number;
};

export type EstadoInvitacion = "INVITADOS" | "SIN_COINCIDENCIAS" | "SIN_ZONA" | "NO_VISIBLE" | "COMPLETA" | "ERROR";

export type Invitacion = {
  estado: EstadoInvitacion;
  mensaje: string;
  invitados: Invitado[];
};

export const buscarDeNuevo = (comunidadId: number) =>
  pedirApi<Invitacion>(`/api/comunidades/${comunidadId}/invitaciones`, { method: "POST" });

export type ComunidadActiva = {
  id: number;
  actividad: string;
  horaInicio: string;
  horaFin: string;
  maxParticipantes: number;
  confirmados: number;
  creador: string;
  esMia: boolean;
  miEstado: "PENDIENTE" | "CONFIRMADO" | "RECHAZADO" | null;
};

export type ResultadoDescubrir = {
  estado: "OK" | "SIN_RESULTADOS" | "SIN_ZONA" | "NO_VISIBLE";
  mensaje: string | null;
  comunidades: ComunidadActiva[];
};

export const listarComunidades = () => pedirApi<ResultadoDescubrir>("/api/comunidades");

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

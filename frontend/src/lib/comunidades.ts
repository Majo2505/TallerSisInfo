import { pedirApi } from "./api";

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

type ComunidadApi = {
  id: number;
  actividad: string;
  texto: string;
  inicio: string;
  fin: string;
  maxParticipantes: number;
  estado: string;
  participantes: number;
  lugar: string | null;
};

const adaptar = (c: ComunidadApi): Comunidad => ({
  id: c.id,
  actividad: c.actividad,
  lugar: c.lugar ?? "Campus UCB",
  categoria: "Estudio",
  inicio: new Date(c.inicio).toLocaleTimeString("es-BO", { hour: "2-digit", minute: "2-digit", hour12: false }),
  fin: new Date(c.fin).toLocaleTimeString("es-BO", { hour: "2-digit", minute: "2-digit", hour12: false }),
  participantes: Array.from({ length: c.participantes }, () => "Participante"),
  maximo: c.maxParticipantes,
  creador: "Tú",
});

export async function listarComunidades(): Promise<Comunidad[]> {
  const datos = await pedirApi<ComunidadApi[]>("/api/comunidades");
  return datos.map(adaptar);
}

export function crearComunidad(texto: string): Promise<ComunidadApi> {
  const inicio = new Date();
  inicio.setMinutes(Math.ceil((inicio.getMinutes() + 1) / 15) * 15, 0, 0);
  const fin = new Date(inicio.getTime() + 45 * 60_000);
  return pedirApi<ComunidadApi>("/api/comunidades", {
    method: "POST",
    body: JSON.stringify({ texto, actividad: texto.slice(0, 100), inicio: inicio.toISOString(), fin: fin.toISOString() }),
  });
}

export function responderComunidad(id: number, confirmar: boolean) {
  return pedirApi(`/api/comunidades/${id}/rsvp`, { method: "PATCH", body: JSON.stringify({ confirmar }) });
}

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

/**
 * Datos de ejemplo para las pantallas que todavía no tienen backend (Inicio, IA, Campus y Perfil).
 * Son estáticos y solo sirven para ver el diseño: cuando existan los endpoints, se reemplazan por la API.
 */

export type EstadoAnimo = "cansado" | "enfocado" | "energia" | "social";

export const ESTADOS_ANIMO: { id: EstadoAnimo; nombre: string; descripcion: string }[] = [
  { id: "cansado", nombre: "Cansado", descripcion: "Necesito descansar o algo tranquilo" },
  { id: "enfocado", nombre: "Enfocado", descripcion: "Quiero aprovechar para estudiar" },
  { id: "energia", nombre: "Energía", descripcion: "Me apetece algo activo o social" },
  { id: "social", nombre: "Social", descripcion: "Busco conectar con alguien" },
];

export type Categoria = "Comida" | "Estudio" | "Descanso" | "Deporte";

export const CATEGORIAS: Categoria[] = ["Comida", "Estudio", "Descanso", "Deporte"];

export type Lugar = {
  id: number;
  nombre: string;
  categoria: Categoria;
  distanciaM: number;
  minutos: number;
  rating: number;
  abreHasta: string;
  estados: EstadoAnimo[];
};

export const LUGARES: Lugar[] = [
  { id: 1, nombre: "Cafetería Central", categoria: "Comida", distanciaM: 150, minutos: 3, rating: 4.5, abreHasta: "20:00", estados: ["cansado", "social", "energia"] },
  { id: 2, nombre: "Biblioteca Norte — Quiet Zone", categoria: "Estudio", distanciaM: 200, minutos: 4, rating: 4.8, abreHasta: "22:00", estados: ["enfocado", "cansado"] },
  { id: 3, nombre: "Jardín de las Ciencias", categoria: "Descanso", distanciaM: 120, minutos: 2, rating: 4.6, abreHasta: "19:00", estados: ["cansado", "energia"] },
  { id: 4, nombre: "Cancha Polideportiva", categoria: "Deporte", distanciaM: 300, minutos: 5, rating: 4.4, abreHasta: "21:00", estados: ["energia", "social"] },
  { id: 5, nombre: "Sala de Estudio Grupal", categoria: "Estudio", distanciaM: 180, minutos: 3, rating: 4.7, abreHasta: "21:00", estados: ["enfocado", "social"] },
  { id: 6, nombre: "Kiosco del Patio", categoria: "Comida", distanciaM: 90, minutos: 2, rating: 4.2, abreHasta: "18:00", estados: ["energia", "social"] },
];

export const CARRERAS = [
  "Ingeniería de Sistemas",
  "Ingeniería Comercial",
  "Derecho",
  "Psicología",
  "Arquitectura",
  "Administración de Empresas",
];

export const SEMESTRES = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

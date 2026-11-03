/**
 * Estados de ánimo que el estudiante puede elegir para pedir una recomendación (HU7).
 * Es la lista de opciones del selector, no datos del servidor: los lugares y las recomendaciones salen siempre de la API.
 */

export type EstadoAnimo = "cansado" | "enfocado" | "energia" | "social";

export const ESTADOS_ANIMO: { id: EstadoAnimo; nombre: string; descripcion: string }[] = [
  { id: "cansado", nombre: "Cansado", descripcion: "Necesito descansar o algo tranquilo" },
  { id: "enfocado", nombre: "Enfocado", descripcion: "Quiero aprovechar para estudiar" },
  { id: "energia", nombre: "Energía", descripcion: "Tengo ganas de algo activo o social" },
  { id: "social", nombre: "Social", descripcion: "Busco conectar con alguien" },
];

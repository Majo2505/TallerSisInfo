export type NombreIcono = "rayo" | "calendario" | "escudo" | "ubicacion";

export type Paso = {
  /** Nombre corto del paso en la barra de arriba. */
  etiqueta: string;
  /** Título en tres partes: la del medio va resaltada. */
  titulo: [string, string, string];
  texto: string;
  nota: string;
  boton: string;
  icono: NombreIcono;
};

export const PASOS: Paso[] = [
  {
    etiqueta: "Puentes",
    titulo: ["Ese hueco entre clases ahora es ", "tuyo", "."],
    texto:
      "Un «puente» es el tiempo libre entre dos clases. BREAKU lo detecta solo y te ayuda a aprovecharlo: comer, repasar, descansar o juntarte con tu grupo.",
    nota: "Desde 45 minutos libres ya te damos ideas.",
    boton: "Siguiente",
    icono: "rayo",
  },
  {
    etiqueta: "Tu horario",
    titulo: ["Registra tus clases ", "una vez", " y listo."],
    texto:
      "Con tu horario, BREAKU calcula los puentes de toda tu semana y te avisa cuando empieza cada uno. Puedes cargarlo a mano o con una foto.",
    nota: "Siempre tú decides qué se guarda.",
    boton: "Siguiente",
    icono: "calendario",
  },
  {
    etiqueta: "Recomendaciones",
    titulo: ["Solo lugares que ", "existen", " en tu campus."],
    texto:
      "Cada sugerencia sale de un catálogo revisado por la universidad. La IA no puede inventar: si un lugar no está en el catálogo, no te lo recomienda.",
    nota: "Con la distancia y el tiempo de ida y vuelta.",
    boton: "Siguiente",
    icono: "escudo",
  },
  {
    etiqueta: "Privacidad",
    titulo: ["Tu ubicación, ", "solo", " cuando importa."],
    texto:
      "Te pedimos permiso antes de usarla, y solo para sugerirte lugares cercanos. Si prefieres, eliges tu campus a mano. Nunca la compartimos.",
    nota: "Privacidad por diseño.",
    boton: "Empezar con BREAKU",
    icono: "ubicacion",
  },
];

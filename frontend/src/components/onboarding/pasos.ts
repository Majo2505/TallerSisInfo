export type NombreIcono = "rayo" | "calendario" | "escudo" | "ubicacion";

export type Paso = {
  titulo: string;
  texto: string;
  nota: string;
  boton: string;
  icono: NombreIcono;
  brillo: string; // color de la luz del ícono, sobre el azul marino
};

export const PASOS: Paso[] = [
  {
    titulo: "¿Qué es un puente?",
    texto:
      'Un "puente" es el tiempo libre que tienes entre dos clases. BREAKU lo detecta automáticamente y te ayuda a aprovecharlo al máximo.',
    nota: "Mínimo 45 minutos para generar recomendaciones.",
    boton: "Siguiente",
    icono: "rayo",
    brillo: "#fdd537",
  },
  {
    titulo: "Tu horario, tu base",
    texto:
      "Registra tus clases una sola vez. Con eso BREAKU calcula tus puentes de toda la semana y te avisa cuando empieza cada uno.",
    nota: "Siempre tú decides qué se guarda.",
    boton: "Siguiente",
    icono: "calendario",
    brillo: "#2f7fd1",
  },
  {
    titulo: "Recomendaciones verificadas",
    texto:
      "Cada lugar sugerido proviene de un catálogo curado y verificado. La IA solo puede recomendar lo que realmente existe en el campus.",
    nota: "Si no está en el catálogo, no te lo recomendamos.",
    boton: "Siguiente",
    icono: "escudo",
    brillo: "#0e9fbe",
  },
  {
    titulo: "Tu ubicación, solo cuando importa",
    texto:
      "Te pedimos permiso antes de usar tu ubicación, y solo para sugerirte lugares cercanos. Si prefieres, eliges tu campus a mano. Nunca la compartimos.",
    nota: "Privacidad por diseño.",
    boton: "Empezar con BREAKU",
    icono: "ubicacion",
    brillo: "#fdd537",
  },
];

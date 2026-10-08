export type NombreIcono = "rayo" | "calendario" | "escudo" | "ubicacion";

export type Paso = {
  titulo: string;
  texto: string;
  nota: string;
  boton: string;
  icono: NombreIcono;
  fondo: string; // color de fondo de la pantalla
  acento: string; // ícono, puntos, sombra y botón
  acentoTexto: string; // variante más oscura para texto (contraste legible)
};

export const PASOS: Paso[] = [
  {
    titulo: "¿Qué es un puente?",
    texto:
      'Un "puente" es el tiempo libre que tienes entre dos clases. BREAKU lo detecta automáticamente y te ayuda a aprovecharlo al máximo.',
    nota: "Mínimo 30 minutos para generar recomendaciones.",
    boton: "Siguiente",
    icono: "rayo",
    fondo: "#EDE9FE",
    acento: "#7C3AED",
    acentoTexto: "#6D28D9",
  },
  {
    titulo: "Tu horario, tu base",
    texto:
      "Registra tu horario manualmente o sube una foto. Nuestra IA extrae las clases automáticamente para que las confirmes antes de guardar.",
    nota: "Siempre tú decides qué se guarda.",
    boton: "Siguiente",
    icono: "calendario",
    fondo: "#DBEAFE",
    acento: "#2563EB",
    acentoTexto: "#1D4ED8",
  },
  {
    titulo: "Recomendaciones verificadas",
    texto:
      "Cada lugar sugerido proviene de un catálogo curado y verificado. La IA solo puede recomendar lo que realmente existe en el campus.",
    nota: "Sin alucinaciones. 100% respaldado por datos reales.",
    boton: "Siguiente",
    icono: "escudo",
    fondo: "#D1FAE5",
    acento: "#059669",
    acentoTexto: "#047857",
  },
  {
    titulo: "Tu ubicación, solo cuando importa",
    texto:
      "Solo pedimos acceso a tu ubicación cuando estés generando una recomendación activa. Nunca la almacenamos ni la compartimos.",
    nota: "Privacidad por diseño.",
    boton: "Empezar con BREAKU",
    icono: "ubicacion",
    fondo: "#FEF3C7",
    acento: "#D97706",
    acentoTexto: "#B45309",
  },
];

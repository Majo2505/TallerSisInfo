import estilos from "./IconosDuo.module.css";

/**
 * Íconos propios de BREAKU a dos tonos: piezas azul marino, acentos amarillos con contorno marino (así se leen
 * sobre cualquier fondo, también el amarillo suave). Las piezas `acento` dan un brinco corto al pasar el mouse
 * por la tarjeta que los contiene (clase global `icono-duo-anfitrion`).
 */
export type NombreIconoDuo =
  | "descanso"
  | "estudio"
  | "energia"
  | "charla"
  | "grupo"
  | "mapa"
  | "lugar"
  | "birrete"
  | "asistente"
  | "catalogo"
  | "bandera"
  | "barras"
  | "lupa"
  | "usuarios";

/** `vivo`: el acento se mece lento todo el tiempo (para tarjetas grandes de Inicio, no para listas). */
type Props = { nombre: NombreIconoDuo; tamano?: number; vivo?: boolean };

export default function IconoDuo({ nombre, tamano = 24, vivo = false }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" className={`${estilos.icono} ${vivo ? estilos.vivo : ""}`} aria-hidden="true" focusable="false">
      {FORMAS[nombre]}
    </svg>
  );
}

/** Para listas de datos que esperan un componente `Icono` con `tamano` (cifras, accesos, pestañas). */
export function duo(nombre: NombreIconoDuo) {
  function Icono({ tamano }: { tamano?: number }) {
    return <IconoDuo nombre={nombre} tamano={tamano} />;
  }
  return Icono;
}

const m = estilos.marino;
const a = `${estilos.amarillo} ${estilos.acento}`;
const l = estilos.linea;
const b = estilos.blanco;

const FORMAS: Record<NombreIconoDuo, React.ReactNode> = {
  // Taza humeante con una luna: un descanso.
  descanso: (
    <>
      <path d="M17.5 2.6a3.4 3.4 0 1 0 3.9 4.4 2.8 2.8 0 0 1-3.9-4.4Z" className={a} />
      <path d="M4 10h11v5.5a5 5 0 0 1-5 5h-1a5 5 0 0 1-5-5Z" className={m} />
      <path d="M15 11.5h1.6a2.4 2.4 0 0 1 0 4.8H15" className={l} />
      <path d="M8 4.5c-.9 1 .9 2 0 3M11 4.5c-.9 1 .9 2 0 3" className={l} />
    </>
  ),
  // Libro abierto con marcapáginas: estudiar.
  estudio: (
    <>
      <path d="M12 6.5C9.8 5 6.8 4.6 3 5v13c3.8-.4 6.8 0 9 1.5Z" className={m} />
      <path d="M12 6.5C14.2 5 17.2 4.6 21 5v13c-3.8-.4-6.8 0-9 1.5Z" className={m} />
      <path d="M12 6.8v12.5" className={estilos.lineaClara} />
      <path d="M15.5 4.6v6l1.6-1.2 1.6 1.2v-6.2" className={a} />
    </>
  ),
  // Pelota con líneas de rapidez: moverse.
  energia: (
    <>
      <path d="M2.5 9h4M1.5 13h4M2.5 17h3" className={l} />
      <circle cx="14.5" cy="12.5" r="7" className={a} />
      <path d="M8 10.5c4 1.6 9 1.6 13 0M8.3 15.6c3.8-1.4 8.6-1.4 12.4 0" className={l} />
    </>
  ),
  // Dos globos de diálogo: conversar.
  charla: (
    <>
      <path d="M3 5.5A2.5 2.5 0 0 1 5.5 3h8A2.5 2.5 0 0 1 16 5.5v5a2.5 2.5 0 0 1-2.5 2.5H8l-3.5 3v-3A2.5 2.5 0 0 1 3 10.5Z" className={m} />
      <path d="M11 11.5A2.5 2.5 0 0 1 13.5 9h5a2.5 2.5 0 0 1 2.5 2.5v4a2.5 2.5 0 0 1-2.5 2.5V21l-3.5-3h-1.5a2.5 2.5 0 0 1-2.5-2.5Z" className={a} />
      <circle cx="7" cy="8" r="1" className={b} />
      <circle cx="10.5" cy="8" r="1" className={b} />
    </>
  ),
  // Tres personas, la del medio resaltada: un grupo.
  grupo: (
    <>
      <circle cx="5.5" cy="9" r="2.4" className={m} />
      <path d="M1.5 19a4 4 0 0 1 8 0Z" className={m} />
      <circle cx="18.5" cy="9" r="2.4" className={m} />
      <path d="M14.5 19a4 4 0 0 1 8 0Z" className={m} />
      <circle cx="12" cy="7.5" r="3" className={a} />
      <path d="M6.8 20a5.2 5.2 0 0 1 10.4 0Z" className={a} />
    </>
  ),
  // Mapa plegado con un pin: descubrir el campus.
  mapa: (
    <>
      <path d="M2.5 6.5 8 4.5l8 2.5 5.5-2v13l-5.5 2-8-2.5-5.5 2Z" className={m} />
      <path d="M8 4.5v13M16 7v13" className={estilos.lineaClara} />
      <path d="M16 3a3.5 3.5 0 0 1 3.5 3.5C19.5 9 16 12.5 16 12.5S12.5 9 12.5 6.5A3.5 3.5 0 0 1 16 3Z" className={a} />
      <circle cx="16" cy="6.5" r="1.2" className={m} />
    </>
  ),
  // Pin sobre el suelo: un lugar del campus.
  lugar: (
    <>
      <ellipse cx="12" cy="20.5" rx="6" ry="1.6" className={estilos.sombra} />
      <path d="M12 2.5a6.5 6.5 0 0 1 6.5 6.5c0 4.6-6.5 10.5-6.5 10.5S5.5 13.6 5.5 9A6.5 6.5 0 0 1 12 2.5Z" className={m} />
      <circle cx="12" cy="9" r="2.6" className={a} />
    </>
  ),
  // Birrete con borla: estudiantes.
  birrete: (
    <>
      <path d="M12 4 1.5 9 12 14l10.5-5Z" className={m} />
      <path d="M6 11.5V16c0 1.7 2.7 3 6 3s6-1.3 6-3v-4.5L12 14.5Z" className={m} />
      <path d="M20 10v5.5" className={l} />
      <circle cx="20" cy="17" r="1.6" className={a} />
    </>
  ),
  // La cara de Breaki: el asistente de la app.
  asistente: (
    <>
      <path d="M7.5 6.5Q12 1 16.5 6.5" className={estilos.puente} />
      <rect x="3.5" y="6" width="17" height="15" rx="7" className={m} />
      <ellipse cx="9.3" cy="13" rx="2" ry="2.4" className={b} />
      <ellipse cx="14.7" cy="13" rx="2" ry="2.4" className={b} />
      <circle cx="9.6" cy="13.3" r="1.1" className={m} />
      <circle cx="15" cy="13.3" r="1.1" className={m} />
    </>
  ),
  // Tarjetas apiladas con un pin: el catálogo de lugares.
  catalogo: (
    <>
      <rect x="6" y="2.5" width="14" height="16" rx="3" className={estilos.marinoSuave} />
      <rect x="3" y="5.5" width="14" height="16" rx="3" className={m} />
      <path d="M10 8.5a3 3 0 0 1 3 3c0 2.2-3 5-3 5s-3-2.8-3-5a3 3 0 0 1 3-3Z" className={a} />
    </>
  ),
  // Banderín: reportes.
  bandera: (
    <>
      <path d="M5 21V3.5" className={estilos.mastil} />
      <path d="M5.5 4h12l-2.6 4.2 2.6 4.3h-12Z" className={a} />
    </>
  ),
  // Barras con la más alta resaltada: métricas.
  barras: (
    <>
      <rect x="3" y="12" width="4.5" height="9" rx="1.5" className={m} />
      <rect x="9.75" y="4" width="4.5" height="17" rx="1.5" className={a} />
      <rect x="16.5" y="8.5" width="4.5" height="12.5" rx="1.5" className={m} />
    </>
  ),
  // Documento con lupa: auditoría.
  lupa: (
    <>
      <rect x="3" y="2.5" width="14" height="17" rx="2.5" className={m} />
      <path d="M7.5 8h6M7.5 11h4" className={estilos.lineaClara} />
      <circle cx="15.5" cy="15.5" r="4" className={a} />
      <path d="m18.5 18.5 3 3" className={l} />
    </>
  ),
  // Dos personas: usuarios.
  usuarios: (
    <>
      <circle cx="9" cy="8" r="3.4" className={m} />
      <path d="M2.5 20a6.5 6.5 0 0 1 13 0Z" className={m} />
      <circle cx="17" cy="9.5" r="2.6" className={a} />
      <path d="M13.5 20a5 5 0 0 1 9 -2.8V20Z" className={a} />
    </>
  ),
};

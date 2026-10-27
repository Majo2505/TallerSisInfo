import estilos from "./ArteCategoria.module.css";

type Props = { categoria: string };

/**
 * Escena animada por categoría sobre un fondo de color propio. No hay fotos reales de los lugares,
 * así que cada categoría cuenta una pequeña historia: la taza humea, el libro pasa la página,
 * la planta se mece con la nube y la pelota rebota. Se anima al pasar el mouse por la tarjeta
 * (el padre usa la clase global "lugar-arte"); en pantallas táctiles se mueve suave todo el tiempo.
 */
export default function ArteCategoria({ categoria }: Props) {
  const tipo = categoria.toLowerCase();
  return (
    <svg className={`${estilos.arte} ${estilos[tipo] ?? ""}`} viewBox="0 0 120 64" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
      {tipo === "comida" && (
        <>
          <ellipse cx="60" cy="57" rx="24" ry="3.5" fill="#c99400" opacity="0.45" />
          <path className={`${estilos.vapor} ${estilos.v1}`} d="M52 22q-4-5 0-10t0-10" />
          <path className={`${estilos.vapor} ${estilos.v2}`} d="M59 21q-4-5 0-10t0-10" />
          <path className={`${estilos.vapor} ${estilos.v3}`} d="M66 22q-4-5 0-10t0-10" />
          <rect x="45" y="27" width="28" height="27" rx="6" fill="#052845" />
          <path d="M73 33h3a6.5 6.5 0 0 1 0 13h-3" stroke="#052845" strokeWidth="4" fill="none" />
          <rect x="45" y="33" width="28" height="4" fill="#fdd537" opacity="0.9" />
          <g className={estilos.dona}>
            <circle cx="96" cy="47" r="9.5" fill="#e0457b" />
            <circle cx="96" cy="47" r="3.2" fill="#fdd537" />
            <path d="M92 41.5l1.5 1M100 42l-1 1.5M90.5 48.5l1.6-.4M101.5 50l-1.5-.8" stroke="#fff" strokeWidth="1.4" strokeLinecap="round" />
          </g>
        </>
      )}
      {tipo === "estudio" && (
        <>
          <path className={`${estilos.brillo} ${estilos.b1}`} d="M20 14l1.5 3.5L25 19l-3.5 1.5L20 24l-1.5-3.5L15 19l3.5-1.5Z" />
          <path className={`${estilos.brillo} ${estilos.b2}`} d="M100 10l1 2.5 2.5 1-2.5 1-1 2.5-1-2.5-2.5-1 2.5-1Z" />
          <path className={`${estilos.brillo} ${estilos.b3}`} d="M98 40l1 2.5 2.5 1-2.5 1-1 2.5-1-2.5-2.5-1 2.5-1Z" />
          <path d="M60 52 30 48V20l30 4Z" fill="#ffffff" />
          <path d="M60 52l30-4V20l-30 4Z" fill="#dbe6f3" />
          <path className={estilos.pagina} d="M60 52l30-4V20l-30 4Z" fill="#ffffff" />
          <path d="M36 28l18 2.4M36 34l18 2.4M36 40l12 1.6" stroke="#9fb6cf" strokeWidth="1.6" strokeLinecap="round" />
          <path d="M78 22v14l3-2.4 3 2.4V21" fill="#fdd537" />
        </>
      )}
      {tipo === "descanso" && (
        <>
          <circle className={estilos.sol} cx="84" cy="15" r="7" fill="#fdd537" />
          <path className={estilos.nube} d="M22 22a6 6 0 0 1 11-3 5 5 0 0 1 8 4 4 4 0 0 1-1 8H24a5 5 0 0 1-2-9Z" fill="#ffffff" opacity="0.9" />
          <g className={estilos.planta}>
            <path d="M60 46C48 42 40 33 42 24c10 3 17 12 18 22Z" fill="#7fe0c9" />
            <path d="M60 46c12-4 20-13 18-22-10 3-17 12-18 22Z" fill="#b6f0df" />
            <path d="M60 46c-7-9-8-19-2-28 6 9 7 19 2 28Z" fill="#e6fbf5" />
          </g>
          <path d="M48 46h24l-3 12H51Z" fill="#052845" />
        </>
      )}
      {tipo === "deporte" && (
        <>
          <path className={estilos.velocidad} d="M16 28h16M22 36h12M16 44h16" stroke="#ffffff" strokeWidth="3" strokeLinecap="round" />
          <ellipse className={estilos.sombra} cx="66" cy="57" rx="12" ry="2.8" fill="#052845" opacity="0.35" />
          <g className={estilos.pelota}>
            <circle cx="66" cy="42" r="12" fill="#fdd537" />
            <path d="M55 37q11 6 22 0M55 47q11-6 22 0" stroke="#052845" strokeWidth="2" fill="none" />
          </g>
        </>
      )}
    </svg>
  );
}

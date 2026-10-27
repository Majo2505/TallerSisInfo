import estilos from "./ArteCategoria.module.css";

type Props = { categoria: string };

/**
 * Ilustración generativa por categoría, en la paleta UCB. No hay fotos reales de los lugares,
 * así que cada categoría tiene una composición propia que se mueve al pasar el mouse por la tarjeta.
 */
export default function ArteCategoria({ categoria }: Props) {
  const tipo = categoria.toLowerCase();
  return (
    <svg className={`${estilos.arte} ${estilos[tipo] ?? ""}`} viewBox="0 0 120 64" aria-hidden="true">
      {tipo === "comida" && (
        <>
          <circle className={estilos.a1} cx="44" cy="40" r="26" fill="#fdd537" />
          <circle className={estilos.a2} cx="44" cy="40" r="15" fill="#fff7d6" />
          <path className={estilos.a3} d="M84 22 q6 -8 0 -16 M96 26 q6 -8 0 -16" stroke="#052845" strokeWidth="4" strokeLinecap="round" fill="none" />
        </>
      )}
      {tipo === "estudio" && (
        <>
          <rect className={estilos.a1} x="24" y="14" width="14" height="46" rx="4" fill="#052845" />
          <rect className={estilos.a2} x="42" y="22" width="14" height="38" rx="4" fill="#1d5c91" />
          <rect className={estilos.a3} x="60" y="8" width="14" height="52" rx="4" fill="#fdd537" transform="rotate(10 67 34)" />
          <rect x="84" y="30" width="14" height="30" rx="4" fill="#d6e2ef" />
        </>
      )}
      {tipo === "descanso" && (
        <>
          <circle className={estilos.a1} cx="54" cy="32" r="22" fill="#0e9fbe" />
          <circle className={estilos.a2} cx="64" cy="26" r="20" fill="#e2f4f7" />
          <circle className={estilos.a3} cx="92" cy="16" r="4" fill="#fdd537" />
          <circle cx="100" cy="34" r="2.5" fill="#052845" />
        </>
      )}
      {tipo === "deporte" && (
        <>
          <circle className={estilos.a1} cx="70" cy="34" r="24" fill="#2f7fd1" />
          <path className={estilos.a2} d="M50 22 q20 14 40 0 M50 46 q20 -14 40 0" stroke="#fff" strokeWidth="3" fill="none" />
          <path className={estilos.a3} d="M14 26 h22 M20 36 h18 M14 46 h22" stroke="#fdd537" strokeWidth="4" strokeLinecap="round" />
        </>
      )}
    </svg>
  );
}

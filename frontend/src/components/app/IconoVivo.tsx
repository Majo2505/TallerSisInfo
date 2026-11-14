import estilos from "./IconoVivo.module.css";

export type NombreIconoVivo = "luna" | "objetivo" | "rayo" | "personas" | "grupo" | "brujula";

type Props = { nombre: NombreIconoVivo; tamano?: number };

/**
 * Íconos con un movimiento chico que cuenta qué son: la luna se mece, el objetivo late como un radar, el rayo
 * parpadea, las personas se mueven, el grupo se junta y la brújula busca el norte. Ciclos lentos con pausa (Inicio se
 * ve muchas veces al día), más rápidos al pasar el mouse por su tarjeta (.icono-vivo-anfitrion) y quietos si la
 * persona pidió reducir el movimiento. Solo transform y opacity.
 */
export default function IconoVivo({ nombre, tamano = 22 }: Props) {
  return (
    <svg
      className={`${estilos.icono} ${estilos[nombre]}`}
      width={tamano}
      height={tamano}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      {nombre === "luna" && (
        <>
          <path className={estilos.lunaCuerpo} d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z" />
          <path className={estilos.chispa} d="M17 3.5v2M16 4.5h2" />
        </>
      )}
      {nombre === "objetivo" && (
        <>
          <circle className={estilos.onda} cx="12" cy="12" r="9" />
          <circle cx="12" cy="12" r="5.5" />
          <circle cx="12" cy="12" r="1.6" fill="currentColor" />
        </>
      )}
      {nombre === "rayo" && <path className={estilos.rayoCuerpo} d="M13 2 4 14h7l-1 8 9-12h-7l1-8z" />}
      {nombre === "personas" && (
        <>
          <g className={estilos.persona1}>
            <circle cx="9" cy="8" r="3.2" />
            <path d="M3 20c.6-3.4 3-5.4 6-5.4s5.4 2 6 5.4" />
          </g>
          <g className={estilos.persona2}>
            <path d="M16 5.2a3 3 0 0 1 0 5.6M18 14.9c1.6.7 2.7 2.4 3 5.1" />
          </g>
        </>
      )}
      {nombre === "grupo" && (
        <>
          <circle className={estilos.punto1} cx="6" cy="12" r="2.6" fill="currentColor" stroke="none" />
          <circle className={estilos.punto2} cx="12" cy="12" r="2.6" fill="currentColor" stroke="none" />
          <circle className={estilos.punto3} cx="18" cy="12" r="2.6" fill="currentColor" stroke="none" />
        </>
      )}
      {nombre === "brujula" && (
        <>
          <circle cx="12" cy="12" r="9" />
          <path className={estilos.aguja} d="m15.5 8.5-2 5-5 2 2-5z" fill="currentColor" fillOpacity="0.25" />
        </>
      )}
    </svg>
  );
}

import estilos from "./EscenasComunidades.module.css";

/**
 * «Crear»: cómo funciona en un vistazo. Se escribe un mensaje, se arma la tarjeta del grupo y llega la gente.
 * Decorativa (aria-hidden): el formulario de al lado ya lo explica con palabras.
 */
export function EscenaCrear() {
  return (
    <div className={estilos.crear} aria-hidden="true">
      <div className={estilos.mensaje}>
        <span className={estilos.avatarYo}>Tú</span>
        <p>¿Alguien libre para un café en 20 minutos?</p>
      </div>

      <div className={estilos.flecha}>
        <span />
        <span />
        <span />
      </div>

      <div className={estilos.grupo}>
        <span className={estilos.grupoChip}>Comunidad creada</span>
        <strong>Café en la Cafetería Central</strong>
        <small>Hoy · 10:30 · 20 min</small>
        <div className={estilos.van}>
          <span className={`${estilos.cara} ${estilos.c1}`}>AQ</span>
          <span className={`${estilos.cara} ${estilos.c2}`}>DM</span>
          <span className={`${estilos.cara} ${estilos.c3}`}>LR</span>
          <span className={estilos.vanTexto}>3 van</span>
        </div>
      </div>

      <p className={estilos.pie}>Invitamos a quienes están libres a esa hora.</p>
    </div>
  );
}

/** «Descubrir» sin comunidades: un radar que sigue buscando en tu campus. */
export function EscenaBuscando() {
  return (
    <div className={estilos.radar} aria-hidden="true">
      <span className={`${estilos.onda} ${estilos.o1}`} />
      <span className={`${estilos.onda} ${estilos.o2}`} />
      <span className={`${estilos.onda} ${estilos.o3}`} />
      <span className={estilos.centro}>
        <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M12 21s-7-6.2-7-11.5a7 7 0 1 1 14 0C19 14.8 12 21 12 21z" />
          <circle cx="12" cy="9.5" r="2.5" />
        </svg>
      </span>
      <span className={`${estilos.punto} ${estilos.p1}`} />
      <span className={`${estilos.punto} ${estilos.p2}`} />
      <span className={`${estilos.punto} ${estilos.p3}`} />
    </div>
  );
}

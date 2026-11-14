import estilos from "./IlustracionBloque.module.css";

/**
 * Una escena chica por bloque del test, con un movimiento que cuenta de qué trata. Formas simples en los colores de la
 * UCB; solo transform y opacity, y quieta si la persona pidió reducir el movimiento.
 */
export default function IlustracionBloque({ bloque }: { bloque: string }) {
  const tipo = bloque.toLowerCase();
  return (
    <svg className={estilos.escena} viewBox="0 0 160 120" aria-hidden="true" focusable="false">
      <ellipse cx="80" cy="108" rx="52" ry="6" className={estilos.sombra} />
      {tipo.includes("gustos") && <Gustos />}
      {tipo.includes("forma de ser") && <FormaDeSer />}
      {tipo.includes("conectas") && <Conectas />}
      {tipo.includes("día a día") && <DiaADia />}
      {tipo.includes("valores") && <Valores />}
    </svg>
  );
}

/** Un control de juego del que suben notas: lo que te gusta hacer. */
function Gustos() {
  return (
    <>
      <g className={estilos.flotar}>
        <rect x="42" y="56" width="76" height="40" rx="20" fill="#052845" />
        <circle cx="62" cy="76" r="7" fill="#fdd537" />
        <rect x="58.5" y="66" width="7" height="20" rx="2" fill="#052845" opacity="0" />
        <path d="M59 76h6M62 73v6" stroke="#052845" strokeWidth="2.4" strokeLinecap="round" />
        <circle cx="96" cy="72" r="3.6" fill="#2f7fd1" />
        <circle cx="104" cy="79" r="3.6" fill="#fdd537" />
      </g>
      <g className={`${estilos.nota} ${estilos.n1}`}>
        <path d="M58 44V28l12-3v16" stroke="#1d5c91" strokeWidth="3" fill="none" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="55" cy="44" r="4" fill="#1d5c91" />
        <circle cx="67" cy="41" r="4" fill="#1d5c91" />
      </g>
      <g className={`${estilos.nota} ${estilos.n2}`}>
        <path d="M100 40V26" stroke="#c99400" strokeWidth="3" strokeLinecap="round" />
        <path d="M100 26c4 1 6 4 6 7" stroke="#c99400" strokeWidth="3" fill="none" strokeLinecap="round" />
        <circle cx="97" cy="40" r="4" fill="#c99400" />
      </g>
    </>
  );
}

/** Dos burbujas de charla que aparecen por turnos: cómo eres con gente nueva. */
function FormaDeSer() {
  return (
    <>
      <g className={`${estilos.burbuja} ${estilos.b1}`}>
        <path d="M30 28h58a12 12 0 0 1 12 12v14a12 12 0 0 1-12 12H52l-12 10v-10h-10a12 12 0 0 1-12-12V40a12 12 0 0 1 12-12z" fill="#052845" />
        <circle cx="46" cy="47" r="3.4" fill="#fdd537" />
        <circle cx="59" cy="47" r="3.4" fill="#fdd537" />
        <circle cx="72" cy="47" r="3.4" fill="#fdd537" />
      </g>
      <g className={`${estilos.burbuja} ${estilos.b2}`}>
        <path d="M84 58h42a10 10 0 0 1 10 10v10a10 10 0 0 1-10 10h-6v9l-11-9H84a10 10 0 0 1-10-10V68a10 10 0 0 1 10-10z" fill="#fdd537" />
        <path d="M90 73h28" stroke="#052845" strokeWidth="3.4" strokeLinecap="round" />
      </g>
    </>
  );
}

/** Dos círculos que se acercan hasta tocarse: lo que los conecta. */
function Conectas() {
  return (
    <>
      <circle className={estilos.izq} cx="62" cy="62" r="26" fill="#052845" />
      <circle className={estilos.der} cx="98" cy="62" r="26" fill="#fdd537" opacity="0.92" />
      <path
        className={estilos.corazon}
        d="M80 70c-6-4-10-7-10-11.5a5 5 0 0 1 10-1.5 5 5 0 0 1 10 1.5C90 63 86 66 80 70z"
        fill="#ffffff"
      />
    </>
  );
}

/** El sol y la luna se turnan: tu ritmo del día. */
function DiaADia() {
  return (
    <>
      <path d="M24 96a56 56 0 0 1 112 0" stroke="#d6dde6" strokeWidth="3" fill="none" strokeDasharray="3 7" strokeLinecap="round" />
      <g className={estilos.giro}>
        <g transform="translate(80 96)">
          <g transform="translate(-50 0)">
            <circle r="13" fill="#fdd537" />
            <path d="M0-20v-5M0 20v5M-20 0h-5M20 0h5M-14-14l-3-3M14 14l3 3M-14 14l-3 3M14-14l3-3" stroke="#c99400" strokeWidth="2.6" strokeLinecap="round" />
          </g>
          <g transform="translate(50 0)">
            <path d="M8 6A13 13 0 0 1-6-9 13 13 0 1 0 8 6z" fill="#052845" transform="rotate(180)" />
          </g>
        </g>
      </g>
    </>
  );
}

/** Un corazón que late, con chispas: lo que valoras. */
function Valores() {
  return (
    <>
      <path
        className={estilos.latir}
        d="M80 96C56 80 42 68 42 52a18 18 0 0 1 38-8 18 18 0 0 1 38 8c0 16-14 28-38 44z"
        fill="#052845"
      />
      <path d="M66 52a8 8 0 0 1 10-6" stroke="#fdd537" strokeWidth="3.4" fill="none" strokeLinecap="round" className={estilos.latir} />
      <path className={`${estilos.chispa} ${estilos.c1}`} d="M124 30l2 6 6 2-6 2-2 6-2-6-6-2 6-2z" fill="#fdd537" />
      <path className={`${estilos.chispa} ${estilos.c2}`} d="M34 34l1.5 4.5 4.5 1.5-4.5 1.5L34 46l-1.5-4.5L28 40l4.5-1.5z" fill="#2f7fd1" />
    </>
  );
}

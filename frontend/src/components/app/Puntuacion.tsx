import estilos from "./Puntuacion.module.css";

const formato = new Intl.NumberFormat("es-BO", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

type Props = {
  /** De 1 a 5, a partir de los votos de los estudiantes; null si nadie votó. */
  valor: number | null | undefined;
  votos?: number;
  /** «sobreFoto»: pastilla oscura para ir encima de una imagen; «linea»: texto para tablas y listas. */
  variante?: "sobreFoto" | "linea";
};

/** ★ 4,5 · 22 votos. La puntuación sale de los «me gusta» y «no me sirve» de las recomendaciones. */
export default function Puntuacion({ valor, votos = 0, variante = "linea" }: Props) {
  const clase = variante === "sobreFoto" ? estilos.sobreFoto : estilos.linea;
  if (valor == null) {
    return <span className={`${clase} ${estilos.sinVotos}`}>Sin votos aún</span>;
  }
  const texto = formato.format(valor);
  return (
    <span className={clase} aria-label={`Puntuación ${texto} de 5, con ${votos} ${votos === 1 ? "voto" : "votos"}`}>
      <svg className={estilos.estrella} width="15" height="15" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path d="M12 2.8l2.8 5.8 6.3.9-4.6 4.4 1.1 6.3L12 17.2l-5.6 3 1.1-6.3-4.6-4.4 6.3-.9z" />
      </svg>
      <b aria-hidden="true">{texto}</b>
      <small aria-hidden="true">
        {votos} {votos === 1 ? "voto" : "votos"}
      </small>
    </span>
  );
}

/** El lugar fue verificado por el equipo de BreakU (`lugar.validado`). */
export function SelloVerificado() {
  return (
    <span className={estilos.verificado}>
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="m5 12.5 4.5 4.5L19 7.5" />
      </svg>
      Verificado
    </span>
  );
}

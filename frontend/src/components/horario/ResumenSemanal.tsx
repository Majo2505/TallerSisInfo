import estilos from "./ResumenSemanal.module.css";

type Props = {
  clases: number;
  puentes: number;
  tiempoLibreMin: number;
  cargando: boolean;
};

function formatearDuracion(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h}h` : `${h}h ${m}min`;
}

/** Tarjeta con los totales de la semana: clases, puentes y tiempo libre (suma de los puentes). */
export default function ResumenSemanal({ clases, puentes, tiempoLibreMin, cargando }: Props) {
  const valor = (texto: string) => (cargando ? "–" : texto);
  return (
    <section className={estilos.panel} aria-labelledby="resumen-titulo">
      <h2 id="resumen-titulo" className={estilos.titulo}>Resumen semanal</h2>
      <dl className={estilos.lista}>
        <div className={estilos.fila}>
          <dt>Clases registradas</dt>
          <dd className={estilos.clases}>{valor(String(clases))}</dd>
        </div>
        <div className={estilos.fila}>
          <dt>Puentes detectados</dt>
          <dd className={estilos.puentes}>{valor(String(puentes))}</dd>
        </div>
        <div className={estilos.fila}>
          <dt>Tiempo libre total</dt>
          <dd className={estilos.libre}>{valor(formatearDuracion(tiempoLibreMin))}</dd>
        </div>
      </dl>
    </section>
  );
}

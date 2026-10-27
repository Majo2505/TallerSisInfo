import estilos from "./FondoVivo.module.css";

/** Fondo de las superficies azul marino: luces UCB que flotan, grano y un foco que sigue al mouse. */
export default function FondoVivo() {
  return (
    <div className={estilos.fondo} aria-hidden="true">
      <span className={estilos.luzAzul} />
      <span className={estilos.luzCeleste} />
      <span className={estilos.luzAmarilla} />
      <span className={estilos.foco} />
      <span className={estilos.grano} />
    </div>
  );
}

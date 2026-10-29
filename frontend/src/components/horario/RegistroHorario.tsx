import { IconoCamara, IconoLapiz } from "@/components/app/Iconos";
import estilos from "./RegistroHorario.module.css";

type Props = {
  onManual: () => void;
  onFoto: () => void;
};

export default function RegistroHorario({ onManual, onFoto }: Props) {
  return (
    <section className={estilos.tarjeta} aria-labelledby="registro-titulo">
      <h2 id="registro-titulo" className={estilos.titulo}>
        ¿Cómo quieres registrar tu horario?
      </h2>
      <div className={estilos.opciones}>
        <button type="button" className={estilos.foto} onClick={onFoto}>
          <span className={estilos.iconoFoto}>
            <IconoCamara tamano={24} />
          </span>
          <strong>Foto del horario</strong>
          <small>La IA lee tus clases</small>
          <span className={estilos.recomendado}>Recomendado</span>
        </button>
        <button type="button" className={estilos.manual} onClick={onManual}>
          <span className={estilos.iconoManual}>
            <IconoLapiz tamano={24} />
          </span>
          <strong>Manual</strong>
          <small>Clase por clase</small>
        </button>
      </div>
    </section>
  );
}

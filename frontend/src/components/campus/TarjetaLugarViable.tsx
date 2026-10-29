import ArteCategoria from "@/components/inicio/ArteCategoria";
import { IconoPin, IconoReloj } from "@/components/app/Iconos";
import estilos from "@/components/app/TarjetaLugar.module.css";
import { LugarViable } from "@/lib/lugares";

const CON_ILUSTRACION = ["comida", "estudio", "descanso", "deporte"];

type Props = {
  lugar: LugarViable;
  minutosDisponibles: number;
};

/** Un lugar viable para el puente: cuánto cuesta llegar, cuánto dura la actividad y cuánto del puente usa. */
export default function TarjetaLugarViable({ lugar, minutosDisponibles }: Props) {
  const total = lugar.tiempoTrasladoMin + lugar.duracionEstimadaMin;
  const arte = CON_ILUSTRACION.includes(lugar.categoria.toLowerCase()) ? lugar.categoria : "Descanso";

  return (
    <article className={`${estilos.tarjeta} lugar-arte`}>
      <div className={estilos.portada}>
        <ArteCategoria categoria={arte} />
      </div>
      <div className={estilos.cuerpo}>
        <span className={estilos.categoria}>{lugar.categoria}</span>
        {!lugar.validado && <span className={estilos.porValidar}>Por validar</span>}
        <h3 className={estilos.nombre}>{lugar.nombre}</h3>
        {lugar.descripcion && <p className={estilos.descripcion}>{lugar.descripcion}</p>}
        <p className={estilos.meta}>
          <span>
            <IconoPin tamano={14} /> {lugar.distanciaM} m
          </span>
          <span>
            <IconoReloj tamano={14} /> Ida y vuelta {lugar.tiempoTrasladoMin} min
          </span>
          <span>Actividad {lugar.duracionEstimadaMin} min</span>
        </p>
        <p className={estilos.total}>
          Usas {total} de tus {minutosDisponibles} min
        </p>
      </div>
    </article>
  );
}

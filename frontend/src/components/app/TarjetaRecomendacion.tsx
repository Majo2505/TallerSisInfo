import { Recomendacion } from "@/lib/recomendaciones";
import { IconoCheck, IconoDestello, IconoLibro, IconoLuna, IconoObjetivo, IconoPin, IconoRayo, IconoReloj } from "./Iconos";
import estilos from "./TarjetaLugar.module.css";

// Mismo aspecto que TarjetaLugar. La categoría llega como texto del catálogo: si es nueva, se usa el estilo de Estudio.
const ESTILO_CATEGORIA: Record<string, { Icono: typeof IconoLibro; clase: string }> = {
  comida: { Icono: IconoLuna, clase: estilos.comida },
  estudio: { Icono: IconoLibro, clase: estilos.estudio },
  descanso: { Icono: IconoObjetivo, clase: estilos.descanso },
  deporte: { Icono: IconoRayo, clase: estilos.deporte },
};

type Props = {
  recomendacion: Recomendacion;
};

/** Tarjeta de una recomendación del asistente (HU7), con sus métricas a la vista. */
export default function TarjetaRecomendacion({ recomendacion: r }: Props) {
  const { Icono, clase } = ESTILO_CATEGORIA[r.categoria.toLowerCase()] ?? { Icono: IconoPin, clase: estilos.estudio };

  return (
    <article className={estilos.completa}>
      <div className={`${estilos.portada} ${clase}`}>
        <span className={estilos.categoria}>{r.categoria}</span>
        <Icono tamano={44} />
      </div>
      <div className={estilos.cuerpo}>
        <h3 className={estilos.nombreGrande}>{r.nombre}</h3>
        <p className={estilos.justificacion}>
          <IconoDestello tamano={16} />
          <span>{r.explicacion}</span>
        </p>
        <div className={estilos.fila}>
          <p className={estilos.meta}>
            <IconoPin tamano={14} /> {r.distanciaM} m
            <IconoReloj tamano={14} /> {r.tiempoTrasladoMin} min ida y vuelta · {r.duracionEstimadaMin} min de actividad
          </p>
          <span className={estilos.verificado}>
            <IconoCheck tamano={14} /> Verificado
          </span>
        </div>
      </div>
    </article>
  );
}

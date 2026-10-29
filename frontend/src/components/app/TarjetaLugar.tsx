"use client";

import ArteCategoria from "@/components/inicio/ArteCategoria";
import { Lugar } from "@/lib/maqueta";
import { IconoEstrella, IconoPin, IconoReloj } from "./Iconos";
import estilos from "./TarjetaLugar.module.css";

/** Un lugar del campus: ilustración de su categoría, nombre, distancia, tiempo y valoración. */
export default function TarjetaLugar({ lugar }: { lugar: Lugar }) {
  return (
    <article className={`${estilos.tarjeta} ${estilos[lugar.categoria.toLowerCase()] ?? ""} lugar-arte`}>
      <div className={estilos.portada}>
        <ArteCategoria categoria={lugar.categoria} />
        <span className={estilos.rating}>
          <IconoEstrella tamano={13} /> {lugar.rating.toFixed(1)}
        </span>
      </div>
      <div className={estilos.cuerpo}>
        <span className={estilos.categoria}>{lugar.categoria}</span>
        <h3 className={estilos.nombre}>{lugar.nombre}</h3>
        <p className={estilos.meta}>
          <span>
            <IconoPin tamano={14} /> {lugar.distanciaM} m
          </span>
          <span>
            <IconoReloj tamano={14} /> {lugar.minutos} min
          </span>
          <span>Abre hasta las {lugar.abreHasta}</span>
        </p>
      </div>
    </article>
  );
}

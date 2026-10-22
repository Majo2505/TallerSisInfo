"use client";

import { useState } from "react";
import { Lugar } from "@/lib/maqueta";
import {
  IconoCheck,
  IconoDestello,
  IconoEstrella,
  IconoLibro,
  IconoLuna,
  IconoObjetivo,
  IconoPin,
  IconoRayo,
  IconoReloj,
} from "./Iconos";
import estilos from "./TarjetaLugar.module.css";

const ICONO_CATEGORIA = {
  Comida: IconoLuna,
  Estudio: IconoLibro,
  Descanso: IconoObjetivo,
  Deporte: IconoRayo,
};

const CLASE_CATEGORIA = {
  Comida: "comida",
  Estudio: "estudio",
  Descanso: "descanso",
  Deporte: "deporte",
} as const;

type Props = {
  lugar: Lugar;
  variante: "compacta" | "completa";
  minutosLibres?: number;
};

export default function TarjetaLugar({ lugar, variante, minutosLibres }: Props) {
  const [voto, setVoto] = useState<"si" | "no" | null>(null);
  const Icono = ICONO_CATEGORIA[lugar.categoria];
  const clase = estilos[CLASE_CATEGORIA[lugar.categoria]];

  if (variante === "compacta") {
    return (
      <article className={estilos.compacta}>
        <div className={`${estilos.miniatura} ${clase}`}>
          <Icono tamano={24} />
        </div>
        <div className={estilos.datosCompactos}>
          <h3 className={estilos.nombre}>{lugar.nombre}</h3>
          <p className={estilos.meta}>
            <IconoPin tamano={14} /> {lugar.distanciaM} m
            <IconoReloj tamano={14} /> {lugar.minutos} min
          </p>
        </div>
        <span className={estilos.rating}>
          <IconoEstrella tamano={14} /> {lugar.rating.toFixed(1)}
        </span>
      </article>
    );
  }

  return (
    <article className={estilos.completa}>
      <div className={`${estilos.portada} ${clase}`}>
        <span className={estilos.categoria}>{lugar.categoria}</span>
        <span className={estilos.puntuacion}>
          <IconoEstrella tamano={14} /> {lugar.rating.toFixed(1)}
        </span>
        <Icono tamano={44} />
      </div>
      <div className={estilos.cuerpo}>
        <h3 className={estilos.nombreGrande}>{lugar.nombre}</h3>
        <p className={estilos.justificacion}>
          <IconoDestello tamano={16} />
          <span>
            A {lugar.minutos} min caminando · Abre hasta las {lugar.abreHasta}
            {minutosLibres ? ` · Encaja con tus ${minutosLibres} min libres` : ""}
          </span>
        </p>
        <div className={estilos.fila}>
          <p className={estilos.meta}>
            <IconoPin tamano={14} /> {lugar.distanciaM} m
            <IconoReloj tamano={14} /> {lugar.minutos} min
          </p>
          <span className={estilos.verificado}>
            <IconoCheck tamano={14} /> Verificado
          </span>
        </div>
        <div className={estilos.votos}>
          <button
            type="button"
            className={voto === "si" ? estilos.votoSiActivo : estilos.votoSi}
            aria-pressed={voto === "si"}
            onClick={() => setVoto(voto === "si" ? null : "si")}
          >
            Me sirve
          </button>
          <button
            type="button"
            className={voto === "no" ? estilos.votoNoActivo : estilos.votoNo}
            aria-pressed={voto === "no"}
            onClick={() => setVoto(voto === "no" ? null : "no")}
          >
            No aplica
          </button>
        </div>
      </div>
    </article>
  );
}

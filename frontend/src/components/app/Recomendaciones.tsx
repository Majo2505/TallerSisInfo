"use client";

import Link from "next/link";
import { useState } from "react";
import { ESTADOS_ANIMO, EstadoAnimo, LUGARES } from "@/lib/maqueta";
import comun from "./Comun.module.css";
import { IconoDestello, IconoFlechaIzquierda, IconoLuna, IconoObjetivo, IconoPersonas, IconoRayo } from "./Iconos";
import estilos from "./Recomendaciones.module.css";
import TarjetaLugar from "./TarjetaLugar";

const ICONO_ESTADO = {
  cansado: IconoLuna,
  enfocado: IconoObjetivo,
  energia: IconoRayo,
  social: IconoPersonas,
};

type Props = {
  estadoInicial?: EstadoAnimo;
  minutosLibres?: number;
};

export default function Recomendaciones({ estadoInicial, minutosLibres }: Props) {
  const [estado, setEstado] = useState<EstadoAnimo | null>(estadoInicial ?? null);

  if (!estado) {
    return (
      <main className={comun.pagina}>
        <Link href="/inicio" className={comun.volver}>
          <IconoFlechaIzquierda tamano={18} /> Volver
        </Link>

        {minutosLibres && (
          <span className={`${comun.chip} ${estilos.libres}`}>
            <IconoRayo tamano={14} /> {minutosLibres} min libres
          </span>
        )}

        <header>
          <h1 className={comun.titulo}>¿Cómo te sientes ahorita?</h1>
          <p className={comun.subtitulo}>
            Te vamos a sugerir actividades que encajen con tu estado de ánimo y el tiempo que tienes.
          </p>
        </header>

        <div className={estilos.estados}>
          {ESTADOS_ANIMO.map((e) => {
            const Icono = ICONO_ESTADO[e.id];
            return (
              <button
                key={e.id}
                type="button"
                className={`${estilos.estado} ${estilos[`estado_${e.id}`]}`}
                onClick={() => setEstado(e.id)}
              >
                <span className={estilos.estadoIcono}>
                  <Icono tamano={22} />
                </span>
                <strong>{e.nombre}</strong>
                <span>{e.descripcion}</span>
              </button>
            );
          })}
        </div>
      </main>
    );
  }

  const actual = ESTADOS_ANIMO.find((e) => e.id === estado)!;
  const lugares = LUGARES.filter((l) => l.estados.includes(estado));
  const Icono = ICONO_ESTADO[estado];

  return (
    <main className={comun.pagina}>
      <button type="button" className={comun.volver} onClick={() => setEstado(null)}>
        <IconoFlechaIzquierda tamano={18} /> Volver
      </button>

      <header className={estilos.cabecera}>
        <div>
          <h1 className={comun.titulo}>Recomendaciones</h1>
          <p className={comun.subtitulo}>
            {minutosLibres ? `Basadas en tu puente de ${minutosLibres} min` : "Basadas en cómo te sientes"}
          </p>
        </div>
        <span className={comun.chip}>
          <Icono tamano={14} /> {actual.nombre}
        </span>
      </header>

      <p className={estilos.motor}>
        <IconoDestello tamano={16} />
        <span>
          <strong>Motor RAG</strong> · {lugares.length} lugares del catálogo curado · Cada sugerencia tiene
          justificación verificable
        </span>
      </p>

      <div className={estilos.lista}>
        {lugares.map((l) => (
          <TarjetaLugar key={l.id} lugar={l} variante="completa" minutosLibres={minutosLibres} />
        ))}
      </div>

      <p className={comun.nota}>
        Vista previa con lugares de ejemplo; las recomendaciones de verdad van a llegar cuando estén el catálogo y el backend de IA.
      </p>
    </main>
  );
}

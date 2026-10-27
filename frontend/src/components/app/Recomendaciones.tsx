"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import { ESTADOS_ANIMO, EstadoAnimo } from "@/lib/maqueta";
import { Puente, duracionMin, hhmm, listarPuentes } from "@/lib/puentes";
import { RespuestaRecomendacion, pedirRecomendaciones } from "@/lib/recomendaciones";
import { useSesion } from "@/lib/useSesion";
import comun from "./Comun.module.css";
import { IconoDestello, IconoFlechaIzquierda, IconoLuna, IconoObjetivo, IconoPersonas, IconoRayo } from "./Iconos";
import estilos from "./Recomendaciones.module.css";
import TarjetaRecomendacion from "./TarjetaRecomendacion";

const ICONO_ESTADO = {
  cansado: IconoLuna,
  enfocado: IconoObjetivo,
  energia: IconoRayo,
  social: IconoPersonas,
};

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

/** Sin puente en la URL se sugiere el de hoy que está en curso o el próximo; si no, el primero de la semana. */
function puentePorDefecto(puentes: Puente[]): Puente | undefined {
  const ahora = new Date();
  const minutos = ahora.getHours() * 60 + ahora.getMinutes();
  const ordenados = [...puentes].sort((a, b) => a.diaSemana - b.diaSemana || aMin(a.horaInicio) - aMin(b.horaInicio));
  return ordenados.find((p) => p.diaSemana === ahora.getDay() && minutos < aMin(p.horaFin)) ?? ordenados[0];
}

type Props = {
  estadoInicial?: EstadoAnimo;
  minutosLibres?: number;
  puenteInicial?: number;
};

export default function Recomendaciones({ estadoInicial, minutosLibres, puenteInicial }: Props) {
  const { sesion } = useSesion();
  const [estado, setEstado] = useState<EstadoAnimo | null>(estadoInicial ?? null);
  const [puentes, setPuentes] = useState<Puente[] | null>(null);
  const [puenteId, setPuenteId] = useState<number | undefined>(puenteInicial);
  const [respuesta, setRespuesta] = useState<RespuestaRecomendacion | null>(null);
  const [error, setError] = useState("");
  const [intento, setIntento] = useState(0);
  // Clave del pedido en curso: evita repetir la consulta al LLM (StrictMode monta dos veces en desarrollo)
  // y descarta respuestas de un pedido que ya no corresponde.
  const pedido = useRef<string | null>(null);

  useEffect(() => {
    if (!sesion) return;
    listarPuentes()
      .then((lista) => {
        setPuentes(lista);
        setPuenteId((actual) => (lista.some((p) => p.id === actual) ? actual : puentePorDefecto(lista)?.id));
      })
      .catch((e: Error) => setError(e.message));
  }, [sesion]);

  const puente = useMemo(() => puentes?.find((p) => p.id === puenteId), [puentes, puenteId]);
  const minutos = minutosLibres ?? (puente ? duracionMin(puente) : undefined);

  useEffect(() => {
    if (!estado || !puente) return;
    const clave = `${puente.id}-${estado}-${intento}`;
    if (pedido.current === clave) return;
    pedido.current = clave;

    const animo = ESTADOS_ANIMO.find((e) => e.id === estado)!;
    setRespuesta(null);
    setError("");
    pedirRecomendaciones(puente.id, `${animo.nombre}: ${animo.descripcion}`)
      .then((r) => pedido.current === clave && setRespuesta(r))
      .catch((e: Error) => pedido.current === clave && setError(e.message));
  }, [estado, puente, intento]);

  if (!estado) {
    return (
      <main className={comun.pagina}>
        <Link href="/inicio" className={comun.volver}>
          <IconoFlechaIzquierda tamano={18} /> Volver
        </Link>

        {minutos && (
          <span className={`${comun.chip} ${estilos.libres}`}>
            <IconoRayo tamano={14} /> {minutos} min libres
          </span>
        )}

        <header>
          <h1 className={comun.titulo}>¿Cómo te sientes ahora mismo?</h1>
          <p className={comun.subtitulo}>
            Te sugeriremos lugares del campus que encajen con tu estado de ánimo y el tiempo que tienes.
          </p>
        </header>

        {puentes && puentes.length > 1 && (
          <label className={estilos.selector}>
            <span>Puente</span>
            <select value={puenteId ?? ""} onChange={(e) => setPuenteId(Number(e.target.value))}>
              {puentes.map((p) => (
                <option key={p.id} value={p.id}>
                  {DIAS[p.diaSemana]} · {hhmm(p.horaInicio)} – {hhmm(p.horaFin)} ({duracionMin(p)} min)
                </option>
              ))}
            </select>
          </label>
        )}

        {puentes && puentes.length === 0 ? (
          <p className={estilos.vacio}>
            Todavía no tienes puentes. <Link href="/horario">Registra tu horario</Link> para recibir recomendaciones.
          </p>
        ) : (
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
        )}
      </main>
    );
  }

  const actual = ESTADOS_ANIMO.find((e) => e.id === estado)!;
  const Icono = ICONO_ESTADO[estado];
  const sinPuente = puentes !== null && !puente;
  const cargando = !respuesta && !error && !sinPuente;

  return (
    <main className={comun.pagina}>
      <button type="button" className={comun.volver} onClick={() => setEstado(null)}>
        <IconoFlechaIzquierda tamano={18} /> Volver
      </button>

      <header className={estilos.cabecera}>
        <div>
          <h1 className={comun.titulo}>Recomendaciones</h1>
          <p className={comun.subtitulo}>
            {puente
              ? `Para tu puente del ${DIAS[puente.diaSemana].toLowerCase()} de ${hhmm(puente.horaInicio)} a ${hhmm(puente.horaFin)}`
              : "Basadas en cómo te sientes"}
          </p>
        </div>
        <span className={comun.chip}>
          <Icono tamano={14} /> {actual.nombre}
        </span>
      </header>

      {error && (
        <div className="hz-banner" role="alert">
          <span>{error}</span>
          <button type="button" className="hz-btn" onClick={() => setIntento((n) => n + 1)}>
            Reintentar
          </button>
        </div>
      )}

      {cargando && (
        <p className={estilos.motor} role="status">
          <IconoDestello tamano={16} />
          <span>
            <strong>Buscando en el catálogo…</strong> El asistente solo elige entre lugares verificados. Puede tardar
            hasta un minuto.
          </span>
        </p>
      )}

      {sinPuente && (
        <p className={estilos.vacio}>
          Todavía no tienes puentes. <Link href="/horario">Registra tu horario</Link> para recibir recomendaciones.
        </p>
      )}

      {respuesta?.estado === "SIN_RESULTADOS" && <p className={estilos.vacio}>{respuesta.mensaje}</p>}

      {respuesta?.estado === "OK" && (
        <>
          <p className={estilos.motor}>
            <IconoDestello tamano={16} />
            <span>
              <strong>Motor RAG</strong> · {respuesta.recomendaciones.length}{" "}
              {respuesta.recomendaciones.length === 1 ? "lugar" : "lugares"} del catálogo verificado · Cada sugerencia
              está respaldada por un registro del catálogo
            </span>
          </p>

          <div className={estilos.lista}>
            {respuesta.recomendaciones.map((r) => (
              <TarjetaRecomendacion key={r.id} recomendacion={r} />
            ))}
          </div>
        </>
      )}
    </main>
  );
}

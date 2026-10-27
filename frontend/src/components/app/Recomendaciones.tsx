"use client";

import Link from "next/link";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useEffect, useMemo, useRef, useState } from "react";
import { ESTADOS_ANIMO, EstadoAnimo } from "@/lib/maqueta";
import { Puente, duracionMin, hhmm, listarPuentes } from "@/lib/puentes";
import { RespuestaRecomendacion, pedirRecomendaciones } from "@/lib/recomendaciones";
import { useSesion } from "@/lib/useSesion";
import FondoVivo from "./FondoVivo";
import { IconoDestello, IconoFlechaIzquierda, IconoLuna, IconoObjetivo, IconoPersonas, IconoRayo } from "./Iconos";
import { Entrada, Odometro, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "./Movimiento";
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
  const reducir = useReducedMotion();
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
      <Entrada as="main" className={estilos.pagina}>
        <Pieza>
          <Link href="/inicio" className={estilos.volver}>
            <IconoFlechaIzquierda tamano={18} /> Volver
          </Link>
        </Pieza>

        <Pieza as="header" className={estilos.cabecera}>
          {minutos && (
            <span className={estilos.libres}>
              <IconoRayo tamano={14} /> <Odometro valor={minutos} /> min libres
            </span>
          )}
          <h1 className={estilos.titulo}>¿Cómo te sientes ahorita?</h1>
          <p className={estilos.bajada}>Elige tu ánimo y te sugerimos lugares del campus que caben en tu puente.</p>
        </Pieza>

        {puentes && puentes.length > 1 && (
          <Pieza>
            <label className={estilos.selector}>
              <span>Para qué puente</span>
              <select value={puenteId ?? ""} onChange={(e) => setPuenteId(Number(e.target.value))}>
                {puentes.map((p) => (
                  <option key={p.id} value={p.id}>
                    {`${DIAS[p.diaSemana]}, de ${hhmm(p.horaInicio)} a ${hhmm(p.horaFin)} (${duracionMin(p)} min)`}
                  </option>
                ))}
              </select>
            </label>
          </Pieza>
        )}

        {puentes && puentes.length === 0 ? (
          <Pieza>
            <p className={estilos.vacio}>
              Todavía no tienes puentes. <Link href="/horario">Registra tu horario</Link> para recibir recomendaciones.
            </p>
          </Pieza>
        ) : (
          <Pieza className={estilos.estados}>
            {ESTADOS_ANIMO.map((e) => {
              const Icono = ICONO_ESTADO[e.id];
              return (
                <motion.button
                  key={e.id}
                  type="button"
                  className={`${estilos.estado} ${estilos[`estado_${e.id}`]}`}
                  onClick={() => setEstado(e.id)}
                  onPointerMove={seguirPuntero}
                  whileHover={reducir ? undefined : { y: -4 }}
                  whileTap={{ scale: 0.96 }}
                  transition={RESORTE_TOQUE}
                >
                  <span className={estilos.estadoIcono}>
                    <Icono tamano={24} />
                  </span>
                  <strong>{e.nombre}</strong>
                  <span className={estilos.estadoTexto}>{e.descripcion}</span>
                </motion.button>
              );
            })}
          </Pieza>
        )}
      </Entrada>
    );
  }

  const actual = ESTADOS_ANIMO.find((e) => e.id === estado)!;
  const Icono = ICONO_ESTADO[estado];
  const sinPuente = puentes !== null && !puente;
  const cargando = !respuesta && !error && !sinPuente;

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza>
        <button type="button" className={estilos.volver} onClick={() => setEstado(null)}>
          <IconoFlechaIzquierda tamano={18} /> Cambiar ánimo
        </button>
      </Pieza>

      <Pieza as="header" className={estilos.resumen}>
        <FondoVivo />
        <span className={estilos.resumenAnimo}>
          <Icono tamano={16} /> {actual.nombre}
        </span>
        <h1 className={estilos.resumenTitulo}>Para tu puente</h1>
        <p className={estilos.resumenDetalle}>
          {puente
            ? `${DIAS[puente.diaSemana]}, de ${hhmm(puente.horaInicio)} a ${hhmm(puente.horaFin)}`
            : "Basadas en cómo te sientes"}
        </p>
      </Pieza>

      {error && (
        <motion.div
          key={`${error}-${intento}`}
          className={estilos.error}
          role="alert"
          initial={reducir ? false : { x: 0 }}
          animate={{ x: [0, -8, 8, -5, 5, 0] }}
          transition={{ duration: 0.42 }}
        >
          <p>{error}</p>
          <motion.button
            type="button"
            className={estilos.reintentar}
            onClick={() => setIntento((n) => n + 1)}
            whileTap={{ scale: 0.95 }}
            transition={RESORTE_TOQUE}
          >
            Reintentar
          </motion.button>
        </motion.div>
      )}

      {cargando && <Pensando />}

      {sinPuente && (
        <p className={estilos.vacio}>
          Todavía no tienes puentes. <Link href="/horario">Registra tu horario</Link> para recibir recomendaciones.
        </p>
      )}

      {respuesta?.estado === "SIN_RESULTADOS" && (
        <motion.div className={estilos.sinResultados} initial={reducir ? false : { opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <span className={estilos.sinResultadosIcono} aria-hidden="true">
            <IconoDestello tamano={26} />
          </span>
          <p>{respuesta.mensaje}</p>
          <button type="button" className={estilos.volverAnimo} onClick={() => setEstado(null)}>
            Probar con otro ánimo
          </button>
        </motion.div>
      )}

      {respuesta?.estado === "OK" && (
        <>
          <motion.ol
            className={estilos.lista}
            initial={reducir ? false : "oculto"}
            animate="visible"
            variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.09 } } }}
          >
            {respuesta.recomendaciones.map((r, i) => (
              <motion.li
                key={r.id}
                variants={{
                  oculto: { opacity: 0, y: 24, scale: 0.98, filter: "blur(6px)" },
                  visible: { opacity: 1, y: 0, scale: 1, filter: "blur(0px)", transition: { duration: 0.6, ease: SALIDA } },
                }}
              >
                <TarjetaRecomendacion recomendacion={r} destacada={i === 0} />
              </motion.li>
            ))}
          </motion.ol>
          <p className={estilos.nota}>
            <IconoDestello tamano={14} /> Solo lugares verificados del catálogo del campus. Las distancias y los tiempos salen del catálogo, no del asistente.
          </p>
        </>
      )}
    </Entrada>
  );
}

const MENSAJES_PENSANDO = [
  "Revisando el catálogo del campus…",
  "Midiendo distancias desde tu zona…",
  "Descartando lo que no cabe en tu puente…",
  "Eligiendo lo que va con tu ánimo…",
];

/** Mientras el asistente piensa (puede tardar hasta un minuto): una tarjeta viva con mensajes que rotan. */
function Pensando() {
  const reducir = useReducedMotion();
  const [i, setI] = useState(0);

  useEffect(() => {
    const t = setInterval(() => setI((n) => (n + 1) % MENSAJES_PENSANDO.length), 1800);
    return () => clearInterval(t);
  }, []);

  return (
    <div className={estilos.pensando} role="status">
      <FondoVivo />
      <span className={estilos.orbes} aria-hidden="true">
        <span />
        <span />
        <span />
      </span>
      <AnimatePresence mode="wait" initial={false}>
        <motion.p
          key={i}
          className={estilos.pensandoTexto}
          initial={reducir ? false : { opacity: 0, y: 10, filter: "blur(4px)" }}
          animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
          exit={{ opacity: 0, y: -10, filter: "blur(4px)" }}
          transition={{ duration: 0.35, ease: SALIDA }}
        >
          {MENSAJES_PENSANDO[i]}
        </motion.p>
      </AnimatePresence>
      <p className={estilos.pensandoNota}>Puede tardar hasta un minuto.</p>
    </div>
  );
}

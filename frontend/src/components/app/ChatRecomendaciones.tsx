"use client";

import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useEffect, useRef, useState } from "react";
import { Puente, duracionMin } from "@/lib/puentes";
import { RespuestaRecomendacion, pedirRecomendaciones } from "@/lib/recomendaciones";
import { IconoDestello, IconoFlechaDerecha } from "./Iconos";
import { RESORTE_TOQUE, SALIDA } from "./Movimiento";
import estilos from "./ChatRecomendaciones.module.css";
import TarjetaRecomendacion from "./TarjetaRecomendacion";

/** Mismo límite que el backend (ReglasRecomendacion.MaximoLargoEntrada). */
const MAXIMO = 300;

const EJEMPLOS = [
  "Una plática con café",
  "Un lugar tranquilo para leer",
  "Algo para moverme un rato",
  "Almorzar algo rápido",
];

type Mensaje =
  | { id: number; autor: "tu"; texto: string }
  | { id: number; autor: "ia"; consulta: string; estado: "pensando" }
  | { id: number; autor: "ia"; consulta: string; estado: "listo"; respuesta: RespuestaRecomendacion }
  | { id: number; autor: "ia"; consulta: string; estado: "error"; error: string };

/**
 * Chat con el asistente (HU7): el estudiante escribe lo que quiere hacer y recibe lugares del catálogo
 * para su puente. Cada mensaje es una consulta nueva al mismo endpoint de recomendaciones;
 * el texto va como preferencia y el backend ignora cualquier instrucción que contenga.
 */
export default function ChatRecomendaciones({ puente }: { puente?: Puente }) {
  const reducir = useReducedMotion();
  const [mensajes, setMensajes] = useState<Mensaje[]>([]);
  const [texto, setTexto] = useState("");
  const siguienteId = useRef(1);
  const final = useRef<HTMLDivElement>(null);
  const pensando = mensajes.some((m) => m.autor === "ia" && m.estado === "pensando");

  // Cada mensaje nuevo queda a la vista.
  useEffect(() => {
    if (mensajes.length) final.current?.scrollIntoView({ behavior: reducir ? "auto" : "smooth", block: "end" });
  }, [mensajes, reducir]);

  async function consultar(consulta: string, idIa: number) {
    if (!puente) return;
    try {
      const respuesta = await pedirRecomendaciones(puente.id, consulta);
      setMensajes((ms) => ms.map((m) => (m.id === idIa ? { id: idIa, autor: "ia", consulta, estado: "listo", respuesta } : m)));
    } catch (e) {
      const error = (e as Error).message;
      setMensajes((ms) => ms.map((m) => (m.id === idIa ? { id: idIa, autor: "ia", consulta, estado: "error", error } : m)));
    }
  }

  function enviar(valor = texto) {
    const consulta = valor.trim().slice(0, MAXIMO);
    if (!consulta || pensando || !puente) return;
    const idTu = siguienteId.current++;
    const idIa = siguienteId.current++;
    setMensajes((ms) => [...ms, { id: idTu, autor: "tu", texto: consulta }, { id: idIa, autor: "ia", consulta, estado: "pensando" }]);
    setTexto("");
    void consultar(consulta, idIa);
  }

  function reintentar(m: Extract<Mensaje, { autor: "ia" }>) {
    setMensajes((ms) => ms.map((x) => (x.id === m.id ? { id: m.id, autor: "ia", consulta: m.consulta, estado: "pensando" } : x)));
    void consultar(m.consulta, m.id);
  }

  const entrada = {
    initial: reducir ? false : { opacity: 0, transform: "translateY(12px) scale(0.96)" },
    animate: { opacity: 1, transform: "translateY(0px) scale(1)" },
    transition: { type: "spring" as const, duration: 0.45, bounce: 0.2 },
  };

  return (
    <section className={estilos.chat} aria-label="Chat con el asistente">
      <div className={estilos.hilo} role="log" aria-live="polite">
        <motion.div className={`${estilos.fila} ${estilos.filaIa}`} {...entrada}>
          <span className={estilos.avatar} aria-hidden="true">
            <IconoDestello tamano={16} />
          </span>
          <div className={estilos.burbujaIa}>
            ¡Hola! Cuéntame qué te gustaría hacer
            {puente ? ` en tu puente de ${duracionMin(puente)} min` : ""} y te recomiendo lugares del campus que te alcanzan.
          </div>
        </motion.div>

        {mensajes.length === 0 && (
          <motion.div
            className={estilos.ejemplos}
            initial={reducir ? false : "oculto"}
            animate="visible"
            variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.06, delayChildren: 0.2 } } }}
          >
            {EJEMPLOS.map((e) => (
              <motion.button
                key={e}
                type="button"
                className={estilos.ejemplo}
                onClick={() => enviar(e)}
                disabled={!puente}
                variants={{ oculto: { opacity: 0, y: 8 }, visible: { opacity: 1, y: 0, transition: { duration: 0.35, ease: SALIDA } } }}
                whileTap={{ scale: 0.96 }}
              >
                {e}
              </motion.button>
            ))}
          </motion.div>
        )}

        <AnimatePresence initial={false}>
          {mensajes.map((m) =>
            m.autor === "tu" ? (
              <motion.div key={m.id} className={`${estilos.fila} ${estilos.filaTu}`} {...entrada}>
                <div className={estilos.burbujaTu}>{m.texto}</div>
              </motion.div>
            ) : (
              <motion.div key={m.id} className={`${estilos.fila} ${estilos.filaIa}`} {...entrada} layout={!reducir}>
                <span className={estilos.avatar} aria-hidden="true">
                  <IconoDestello tamano={16} />
                </span>
                <div className={estilos.respuesta}>
                  {m.estado === "pensando" && (
                    <div className={estilos.burbujaIa} aria-label="El asistente está escribiendo">
                      <span className={estilos.escribiendo} aria-hidden="true">
                        <span />
                        <span />
                        <span />
                      </span>
                    </div>
                  )}

                  {m.estado === "error" && (
                    <div className={`${estilos.burbujaIa} ${estilos.burbujaError}`} role="alert">
                      <p>{m.error}</p>
                      <button type="button" className={estilos.reintentar} onClick={() => reintentar(m)}>
                        Reintentar
                      </button>
                    </div>
                  )}

                  {m.estado === "listo" && (
                    <>
                      <div className={estilos.burbujaIa}>
                        {m.respuesta.estado === "OK"
                          ? `Para "${m.consulta}" te recomiendo ${m.respuesta.recomendaciones.length === 1 ? "este lugar" : `estos ${m.respuesta.recomendaciones.length} lugares`}:`
                          : m.respuesta.mensaje}
                      </div>
                      {m.respuesta.estado === "OK" && (
                        <motion.ol
                          className={estilos.tarjetas}
                          initial={reducir ? false : "oculto"}
                          animate="visible"
                          variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.08 } } }}
                        >
                          {m.respuesta.recomendaciones.map((r, i) => (
                            <motion.li
                              key={r.id}
                              variants={{
                                oculto: { opacity: 0, y: 16, filter: "blur(4px)" },
                                visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.5, ease: SALIDA } },
                              }}
                            >
                              <TarjetaRecomendacion recomendacion={r} destacada={i === 0} />
                            </motion.li>
                          ))}
                        </motion.ol>
                      )}
                    </>
                  )}
                </div>
              </motion.div>
            ),
          )}
        </AnimatePresence>
        <div ref={final} />
      </div>

      <form
        className={estilos.compositor}
        onSubmit={(e) => {
          e.preventDefault();
          enviar();
        }}
      >
        <textarea
          className={estilos.campo}
          value={texto}
          maxLength={MAXIMO}
          rows={1}
          placeholder={puente ? "Ej: algo tranquilo con café cerca de la biblioteca" : "Primero necesitas un puente en tu horario"}
          aria-label="Escribe qué te gustaría hacer"
          disabled={!puente}
          enterKeyHint="send"
          onChange={(e) => setTexto(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              enviar();
            }
          }}
        />
        <div className={estilos.pie}>
          <span className={estilos.contador}>
            {texto.length}/{MAXIMO}
          </span>
          <motion.button
            type="submit"
            className={estilos.enviar}
            disabled={!texto.trim() || pensando || !puente}
            whileTap={{ scale: 0.94 }}
            transition={RESORTE_TOQUE}
            aria-label="Enviar"
          >
            {pensando ? "Pensando…" : "Enviar"} <IconoFlechaDerecha tamano={18} />
          </motion.button>
        </div>
      </form>
      <p className={estilos.nota}>Cada mensaje es una consulta nueva. Solo recomiendo lugares verificados del catálogo del campus.</p>
    </section>
  );
}

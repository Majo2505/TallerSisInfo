"use client";

import Link from "next/link";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState } from "react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoCheck, IconoFlechaDerecha, IconoFlechaIzquierda, IconoPersonas, IconoPin, IconoReloj } from "@/components/app/Iconos";
import { Entrada, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import CompanerosLibres from "@/components/comunidades/CompanerosLibres";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import { ActividadExtraida, RespuestaExtraccion, extraerActividad, formatearDuracion } from "@/lib/actividades";
import { COMUNIDADES_EJEMPLO, EJEMPLOS_SOLICITUD, MAXIMO_SOLICITUD } from "@/lib/comunidades";
import { iniciales } from "@/lib/iniciales";
import estilos from "./comunidades.module.css";

type Pestana = "libres" | "crear" | "descubrir";

/** Un dato extraído (HU11). Si falta, se muestra «Falta» para que se vea qué se va a preguntar. */
function Dato({ etiqueta, valor }: { etiqueta: string; valor: string | null }) {
  return (
    <li className={valor ? estilos.dato : estilos.datoFalta}>
      <span>{etiqueta}</span>
      <strong>{valor ?? "Falta"}</strong>
    </li>
  );
}

export default function Comunidades() {
  const reducir = useReducedMotion();
  const [pestana, setPestana] = useState<Pestana>("libres");
  const [texto, setTexto] = useState("");
  const [unido, setUnido] = useState<Set<number>>(new Set());
  // HU11: lo que el servidor entendió del texto libre y la respuesta a la pregunta por un dato faltante.
  const [extraccion, setExtraccion] = useState<RespuestaExtraccion | null>(null);
  const [respuesta, setRespuesta] = useState("");
  const [buscando, setBuscando] = useState(false);
  const [error, setError] = useState("");

  async function consultar(textoAEnviar: string, anterior: ActividadExtraida | null) {
    setBuscando(true);
    setError("");
    try {
      setExtraccion(await extraerActividad(textoAEnviar, anterior));
      setRespuesta("");
    } catch (e) {
      // Incluye el rechazo de los guardrails (SOLICITUD_NO_PERMITIDA): el servidor ya manda el texto para mostrar.
      setError(e instanceof Error ? e.message : "Hubo un error inesperado. Inténtalo de nuevo.");
    } finally {
      setBuscando(false);
    }
  }

  function enviar(e: React.FormEvent) {
    e.preventDefault();
    if (!texto.trim() || buscando) return;
    setExtraccion(null); // una descripción nueva: el aviso de la anterior («no entendimos») ya no aplica
    void consultar(texto.trim(), null);
  }

  // Solo se manda la respuesta y lo ya entendido: no hace falta reescribir toda la descripción.
  function responder(e: React.FormEvent) {
    e.preventDefault();
    if (!extraccion || !respuesta.trim() || buscando) return;
    void consultar(respuesta.trim(), extraccion.actividad);
  }

  function reiniciar() {
    setExtraccion(null);
    setTexto("");
    setRespuesta("");
    setError("");
  }

  const entendida = extraccion && extraccion.estado !== "NO_ENTENDIDO" ? extraccion : null;

  function alternarUnion(id: number) {
    setUnido((actual) => {
      const nuevo = new Set(actual);
      if (nuevo.has(id)) nuevo.delete(id);
      else nuevo.add(id);
      return nuevo;
    });
  }

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza>
        <Link href="/inicio" className={estilos.volver}>
          <IconoFlechaIzquierda tamano={18} /> Volver
        </Link>
      </Pieza>

      <Pieza as="header" className={estilos.portada}>
        <div onPointerMove={seguirPuntero} className={estilos.portadaInterior}>
          <FondoVivo />
          <span className={estilos.etiqueta}>
            <IconoPersonas tamano={14} /> Comunidades efímeras
          </span>
          <h1 className={estilos.titulo}>Crea y descubre comunidades</h1>
          <p className={estilos.bajada}>Junta gente con el mismo tiempo libre para estudiar, comer o moverte, solo por ese puente.</p>
          <div className={estilos.caras} aria-hidden="true">
            {["Ana", "Luis", "Sofía", "Marco"].map((n, i) => (
              <motion.span
                key={n}
                className={estilos.cara}
                style={{ zIndex: 4 - i }}
                initial={reducir ? false : { opacity: 0, transform: "translateX(-12px) scale(0.8)" }}
                animate={{ opacity: 1, transform: "translateX(0px) scale(1)" }}
                transition={{ type: "spring", duration: 0.5, bounce: 0.3, delay: 0.3 + i * 0.08 }}
              >
                {iniciales(n)}
              </motion.span>
            ))}
            <span className={estilos.carasTexto}>+38 hoy en el campus</span>
          </div>
        </div>
      </Pieza>

      <Pieza>
        <div className={estilos.pestanas} role="tablist" aria-label="Comunidades">
          {(
            [
              ["libres", "Libres contigo"],
              ["crear", "Crear"],
              ["descubrir", "Descubrir"],
            ] as const
          ).map(([id, etiqueta]) => (
            <motion.button
              key={id}
              type="button"
              role="tab"
              aria-selected={pestana === id}
              className={pestana === id ? estilos.pestanaActiva : estilos.pestana}
              onClick={() => setPestana(id)}
              whileTap={{ scale: 0.96 }}
              transition={RESORTE_TOQUE}
            >
              {pestana === id && (
                <motion.span
                  layoutId="pestana-comunidades"
                  className={estilos.pastilla}
                  transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
                />
              )}
              <span className={estilos.textoPestana}>{etiqueta}</span>
            </motion.button>
          ))}
        </div>
      </Pieza>

      <Pieza>
        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={pestana}
            initial={reducir ? false : { opacity: 0, y: 12, filter: "blur(4px)" }}
            animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
            exit={reducir ? undefined : { opacity: 0, y: -8, filter: "blur(4px)" }}
            transition={{ duration: 0.25, ease: SALIDA }}
          >
            {pestana === "libres" ? (
              <CompanerosLibres />
            ) : pestana === "crear" ? (
              <section className={estilos.crear} aria-labelledby="crear-titulo">
                <span className={estilos.chip}>Solicitud libre</span>
                <h2 id="crear-titulo" className={estilos.subtitulo}>
                  Escríbelo en tus palabras
                </h2>
                <p className={estilos.texto}>Cuéntanos qué, a qué hora, cuánto dura y con cuántas personas. Si falta algo, te preguntamos.</p>
                {/* Arriba del formulario: en el celular, debajo del botón quedaría tapado por la barra inferior. */}
                {error && (
                  <p className={estilos.error} role="alert">
                    {error}
                  </p>
                )}

                <AnimatePresence mode="wait" initial={false}>
                  {entendida ? (
                    <motion.div
                      key="entendida"
                      className={estilos.resultado}
                      initial={reducir ? false : { opacity: 0, transform: "translateY(12px) scale(0.97)" }}
                      animate={{ opacity: 1, transform: "translateY(0px) scale(1)" }}
                      transition={{ type: "spring", duration: 0.5, bounce: 0.2 }}
                    >
                      <span className={estilos.resultadoIcono}>
                        <IconoCheck tamano={22} />
                      </span>
                      <div className={estilos.entendida}>
                        <strong>{entendida.estado === "COMPLETO" ? "Esto entendimos" : "Ya casi"}</strong>
                        <ul className={estilos.datos}>
                          <Dato etiqueta="Actividad" valor={entendida.actividad.actividad} />
                          <Dato
                            etiqueta="Hora"
                            valor={
                              entendida.actividad.horaInicio &&
                              `${entendida.actividad.horaInicio}${entendida.supuestos.includes("horaInicio") ? " (ahora)" : ""}`
                            }
                          />
                          <Dato
                            etiqueta="Duración"
                            valor={entendida.actividad.duracionMin ? formatearDuracion(entendida.actividad.duracionMin) : null}
                          />
                          <Dato
                            etiqueta="Participantes"
                            valor={entendida.actividad.maxParticipantes ? `hasta ${entendida.actividad.maxParticipantes}` : null}
                          />
                        </ul>

                        {entendida.estado === "INCOMPLETO" ? (
                          <form className={estilos.respuesta} onSubmit={responder}>
                            <label htmlFor="respuesta-dato" className={estilos.pregunta}>
                              {entendida.mensaje}
                            </label>
                            <div className={estilos.respuestaFila}>
                              <input
                                id="respuesta-dato"
                                className={estilos.respuestaCampo}
                                value={respuesta}
                                maxLength={MAXIMO_SOLICITUD}
                                onChange={(e) => setRespuesta(e.target.value)}
                                placeholder="Tu respuesta"
                                autoComplete="off"
                                autoFocus
                                disabled={buscando}
                              />
                              <button type="submit" className={estilos.responder} disabled={!respuesta.trim() || buscando}>
                                {buscando ? "Revisando…" : "Responder"}
                              </button>
                            </div>
                          </form>
                        ) : (
                          <p>Pronto podrás confirmar y crear el grupo desde aquí.</p>
                        )}
                      </div>
                      <button type="button" className={estilos.otra} onClick={reiniciar} disabled={buscando}>
                        Escribir otra
                      </button>
                    </motion.div>
                  ) : (
                    <motion.form key="form" className={estilos.formulario} onSubmit={enviar} exit={{ opacity: 0 }}>
                      {extraccion?.estado === "NO_ENTENDIDO" && (
                        <p className={estilos.noEntendido} role="status">
                          {extraccion.mensaje}
                        </p>
                      )}
                      <textarea
                        className={estilos.campo}
                        value={texto}
                        maxLength={MAXIMO_SOLICITUD}
                        onChange={(e) => setTexto(e.target.value)}
                        placeholder="¿Qué quieres hacer? Ej: ¿alguien libre para un café en 20 minutos?"
                        aria-label="Describe tu solicitud"
                        disabled={buscando}
                      />
                      <span className={estilos.contador}>
                        {texto.length}/{MAXIMO_SOLICITUD} caracteres
                      </span>

                      <p className={estilos.ejemplosTitulo}>Ejemplos rápidos</p>
                      <div className={estilos.ejemplos}>
                        {EJEMPLOS_SOLICITUD.map((e) => (
                          <motion.button
                            key={e}
                            type="button"
                            className={estilos.ejemplo}
                            onClick={() => setTexto(e)}
                            whileTap={{ scale: 0.96 }}
                            disabled={buscando}
                          >
                            {e}
                          </motion.button>
                        ))}
                      </div>

                      <motion.button
                        type="submit"
                        className={estilos.enviar}
                        disabled={!texto.trim() || buscando}
                        whileTap={{ scale: 0.97 }}
                        transition={RESORTE_TOQUE}
                      >
                        {buscando ? (
                          <>
                            <span className={estilos.radar} aria-hidden="true" /> Leyendo tu actividad…
                          </>
                        ) : (
                          <>
                            Enviar solicitud <IconoFlechaDerecha tamano={18} />
                          </>
                        )}
                      </motion.button>
                    </motion.form>
                  )}
                </AnimatePresence>
                <p className={estilos.nota}>Sin historial de conversación · Solicitud puntual · Sin chat grupal</p>
              </section>
            ) : (
              <motion.ul
                className={estilos.lista}
                initial={reducir ? false : "oculto"}
                animate="visible"
                variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.07 } } }}
              >
                {COMUNIDADES_EJEMPLO.map((c) => {
                  const dentro = unido.has(c.id);
                  const total = c.participantes.length + (dentro ? 1 : 0);
                  return (
                    <motion.li
                      key={c.id}
                      className={`${estilos.comunidad} lugar-arte`}
                      variants={{
                        oculto: { opacity: 0, y: 16, filter: "blur(4px)" },
                        visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.45, ease: SALIDA } },
                      }}
                      whileHover={reducir ? undefined : { y: -4 }}
                    >
                      <ArteCategoria categoria={c.categoria} />
                      <div className={estilos.comunidadCuerpo}>
                        <h3>{c.actividad}</h3>
                        <p className={estilos.meta}>
                          <span>
                            <IconoPin tamano={14} /> {c.lugar}
                          </span>
                          <span>
                            <IconoReloj tamano={14} /> {c.inicio}–{c.fin}
                          </span>
                        </p>
                        <div className={estilos.pieComunidad}>
                          <span className={estilos.miembros}>
                            {c.participantes.slice(0, 3).map((n) => (
                              <span key={n} className={estilos.miembro}>
                                {iniciales(n)}
                              </span>
                            ))}
                            <small>
                              {total}/{c.maximo}
                            </small>
                          </span>
                          <motion.button
                            type="button"
                            className={dentro ? estilos.unido : estilos.unirme}
                            onClick={() => alternarUnion(c.id)}
                            whileTap={{ scale: 0.95 }}
                            transition={RESORTE_TOQUE}
                            aria-pressed={dentro}
                          >
                            {dentro ? (
                              <>
                                <IconoCheck tamano={16} /> Te uniste
                              </>
                            ) : (
                              "Unirme"
                            )}
                          </motion.button>
                        </div>
                        <small className={estilos.creador}>Creada por {c.creador}</small>
                      </div>
                    </motion.li>
                  );
                })}
              </motion.ul>
            )}
          </motion.div>
        </AnimatePresence>
      </Pieza>

      {pestana === "descubrir" && (
        <Pieza>
          <p className={estilos.aviso}>Vista previa con comunidades de ejemplo; la creación real de grupos llega con su historia de usuario.</p>
        </Pieza>
      )}
    </Entrada>
  );
}

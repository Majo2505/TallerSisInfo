"use client";

import Link from "next/link";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState } from "react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoCheck, IconoFlechaDerecha, IconoFlechaIzquierda, IconoPersonas, IconoPin, IconoReloj } from "@/components/app/Iconos";
import { Entrada, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import { COMUNIDADES_EJEMPLO, EJEMPLOS_SOLICITUD, MAXIMO_SOLICITUD } from "@/lib/comunidades";
import { iniciales } from "@/lib/iniciales";
import estilos from "./comunidades.module.css";

type Pestana = "crear" | "descubrir";
type Envio = "nada" | "buscando" | "listo";

export default function Comunidades() {
  const reducir = useReducedMotion();
  const [pestana, setPestana] = useState<Pestana>("crear");
  const [texto, setTexto] = useState("");
  const [envio, setEnvio] = useState<Envio>("nada");
  const [unido, setUnido] = useState<Set<number>>(new Set());

  function enviar(e: React.FormEvent) {
    e.preventDefault();
    if (!texto.trim() || envio === "buscando") return;
    setEnvio("buscando");
    // Vista previa: simula la búsqueda de compañeros con el mismo tiempo libre.
    setTimeout(() => setEnvio("listo"), reducir ? 0 : 2200);
  }

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
            {pestana === "crear" ? (
              <section className={estilos.crear} aria-labelledby="crear-titulo">
                <span className={estilos.chip}>Solicitud libre</span>
                <h2 id="crear-titulo" className={estilos.subtitulo}>
                  Escríbelo en tus palabras
                </h2>
                <p className={estilos.texto}>Buscamos compañeros con el mismo tiempo libre y armamos el grupo por ti.</p>

                <AnimatePresence mode="wait" initial={false}>
                  {envio === "listo" ? (
                    <motion.div
                      key="listo"
                      className={estilos.resultado}
                      initial={reducir ? false : { opacity: 0, transform: "translateY(12px) scale(0.97)" }}
                      animate={{ opacity: 1, transform: "translateY(0px) scale(1)" }}
                      transition={{ type: "spring", duration: 0.5, bounce: 0.2 }}
                    >
                      <span className={estilos.resultadoIcono}>
                        <IconoCheck tamano={22} />
                      </span>
                      <div>
                        <strong>¡Grupo armado!</strong>
                        <p>
                          3 compañeros con tu mismo puente se sumaron a “{texto.trim()}”. Te esperan en la Cafetería Central de 10:30 a
                          11:15.
                        </p>
                      </div>
                      <button
                        type="button"
                        className={estilos.otra}
                        onClick={() => {
                          setEnvio("nada");
                          setTexto("");
                        }}
                      >
                        Crear otra
                      </button>
                    </motion.div>
                  ) : (
                    <motion.form key="form" className={estilos.formulario} onSubmit={enviar} exit={{ opacity: 0 }}>
                      <textarea
                        className={estilos.campo}
                        value={texto}
                        maxLength={MAXIMO_SOLICITUD}
                        onChange={(e) => setTexto(e.target.value)}
                        placeholder="¿Qué quieres hacer? Ej: ¿alguien libre para un café en 20 minutos?"
                        aria-label="Describe tu solicitud"
                        disabled={envio === "buscando"}
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
                            disabled={envio === "buscando"}
                          >
                            {e}
                          </motion.button>
                        ))}
                      </div>

                      <motion.button
                        type="submit"
                        className={estilos.enviar}
                        disabled={!texto.trim() || envio === "buscando"}
                        whileTap={{ scale: 0.97 }}
                        transition={RESORTE_TOQUE}
                      >
                        {envio === "buscando" ? (
                          <>
                            <span className={estilos.radar} aria-hidden="true" /> Buscando compañeros…
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

      <Pieza>
        <p className={estilos.aviso}>Vista previa con comunidades de ejemplo; la creación real de grupos llega con su historia de usuario.</p>
      </Pieza>
    </Entrada>
  );
}

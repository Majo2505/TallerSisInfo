"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { IconoCandado, IconoCheck, IconoFlechaDerecha, IconoFlechaIzquierda } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { type PreguntaTest, type RespuestasTest, type TestAfinidad as Test, alternar, guardarTest, preguntaLista } from "@/lib/afinidad";
import estilos from "./TestAfinidad.module.css";

const CLAVE_BORRADOR = "breaku.testAfinidad.borrador";
const sinSuscripcion = () => () => {};

function leerBorrador(): RespuestasTest | null {
  try {
    const crudo = window.sessionStorage.getItem(CLAVE_BORRADOR);
    return crudo ? (JSON.parse(crudo) as RespuestasTest) : null;
  } catch {
    return null;
  }
}

function guardarBorrador(r: RespuestasTest | null) {
  try {
    if (r) window.sessionStorage.setItem(CLAVE_BORRADOR, JSON.stringify(r));
    else window.sessionStorage.removeItem(CLAVE_BORRADOR);
  } catch {
    // Sin almacenamiento (modo privado): el test funciona igual, solo no recuerda el borrador.
  }
}

type Props = {
  test: Test;
  onCerrar: () => void;
  onListo: (test: Test) => void;
};

/**
 * HU12: el test de afinidad, una pregunta a la vez. Barra de progreso, recorrido por bloques (en pantallas anchas),
 * «Saltar» en las opcionales y borrador en la pestaña por si se cierra a la mitad.
 */
export default function TestAfinidad({ test, onCerrar, onListo }: Props) {
  const reducir = useReducedMotion();
  const enCliente = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  const idTitulo = useId();
  const preguntas = test.preguntas;
  const [respuestas, setRespuestas] = useState<RespuestasTest>(() => leerBorrador() ?? test.respuestas ?? {});
  const [[indice, direccion], setPaso] = useState<[number, number]>([0, 1]);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState("");
  const titulo = useRef<HTMLHeadingElement>(null);

  const pregunta = preguntas[indice];
  const elegidas = respuestas[pregunta.id] ?? [];
  const lista = preguntaLista(pregunta, elegidas);
  const esUltima = indice === preguntas.length - 1;
  const respondidas = preguntas.filter((p) => (respuestas[p.id]?.length ?? 0) > 0).length;
  const segundosRestantes = Math.max(0, Math.round(((preguntas.length - indice) * test.minutosEstimados * 60) / preguntas.length));

  const bloques = useMemo(() => {
    const orden: { nombre: string; preguntas: PreguntaTest[]; opcional: boolean }[] = [];
    for (const p of preguntas) {
      const ultimo = orden[orden.length - 1];
      if (ultimo?.nombre === p.bloque) ultimo.preguntas.push(p);
      else orden.push({ nombre: p.bloque, preguntas: [p], opcional: p.opcional });
    }
    return orden;
  }, [preguntas]);

  useEffect(() => guardarBorrador(respuestas), [respuestas]);

  // El foco va al título de cada pregunta: el lector de pantalla la anuncia y el teclado arranca desde ahí.
  useEffect(() => titulo.current?.focus({ preventScroll: true }), [indice]);

  useEffect(() => {
    const previo = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previo;
    };
  }, []);

  const ir = useCallback(
    (nuevo: number) => setPaso(([actual]) => [Math.min(Math.max(nuevo, 0), preguntas.length - 1), nuevo >= actual ? 1 : -1]),
    [preguntas.length],
  );

  const terminar = useCallback(async () => {
    setEnviando(true);
    setError("");
    try {
      // Las opcionales sin responder no se mandan.
      const limpias = Object.fromEntries(Object.entries(respuestas).filter(([, v]) => v.length > 0));
      const guardado = await guardarTest(limpias);
      guardarBorrador(null);
      onListo(guardado);
    } catch (e) {
      setError((e as Error).message);
      setEnviando(false);
    }
  }, [respuestas, onListo]);

  const siguiente = useCallback(() => {
    if (!lista || enviando) return;
    if (esUltima) void terminar();
    else ir(indice + 1);
  }, [lista, enviando, esUltima, terminar, ir, indice]);

  /** En una opcional: «Prefiero no responder» borra solo esa pregunta y sigue (o termina si es la última). */
  function saltarPregunta() {
    setRespuestas((r) => Object.fromEntries(Object.entries(r).filter(([k]) => k !== pregunta.id)));
    if (esUltima) void terminarSin([pregunta.id]);
    else ir(indice + 1);
  }

  async function terminarSin(quitar: string[]) {
    setEnviando(true);
    setError("");
    try {
      const limpias = Object.fromEntries(Object.entries(respuestas).filter(([k, v]) => v.length > 0 && !quitar.includes(k)));
      const guardado = await guardarTest(limpias);
      guardarBorrador(null);
      onListo(guardado);
    } catch (e) {
      setError((e as Error).message);
      setEnviando(false);
    }
  }

  useEffect(() => {
    function alTeclear(e: KeyboardEvent) {
      if (e.key === "Escape" && !enviando) onCerrar();
      if (e.key === "Enter" && !(e.target instanceof HTMLButtonElement)) siguiente();
    }
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [enviando, onCerrar, siguiente]);

  if (!enCliente) return null;

  const ayuda =
    (pregunta.tipo === "UNICA"
      ? "Elige una."
      : `Elige hasta ${pregunta.maximo}${elegidas.length > 0 ? ` · llevas ${elegidas.length}` : ""}.`) +
    (pregunta.opcional ? " Es opcional." : "");

  const variantes = {
    entra: (dir: number) => ({ opacity: 0, x: reducir ? 0 : dir * 32, filter: "blur(4px)" }),
    centro: { opacity: 1, x: 0, filter: "blur(0px)" },
    sale: (dir: number) => ({ opacity: 0, x: reducir ? 0 : dir * -32, filter: "blur(4px)" }),
  };

  return createPortal(
    <motion.div
      className={estilos.fondo}
      role="dialog"
      aria-modal="true"
      aria-labelledby={idTitulo}
      initial={reducir ? false : { opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.22 }}
    >
      <div className={estilos.marco}>
        <header className={estilos.cabecera}>
          <button type="button" className={estilos.cerrar} onClick={onCerrar} disabled={enviando}>
            Cerrar
          </button>
          <div className={estilos.progresoTexto} aria-live="polite">
            <strong>
              Pregunta {indice + 1} de {preguntas.length}
            </strong>
            <span>
              {pregunta.bloque} · {segundosRestantes < 60 ? `quedan unos ${segundosRestantes} s` : `quedan unos ${Math.ceil(segundosRestantes / 60)} min`}
            </span>
          </div>
          <div
            className={estilos.barra}
            role="progressbar"
            aria-label="Progreso del test"
            aria-valuemin={0}
            aria-valuemax={preguntas.length}
            aria-valuenow={indice + 1}
          >
            <motion.span
              className={estilos.barraLlena}
              animate={{ scaleX: (indice + 1) / preguntas.length }}
              transition={reducir ? { duration: 0 } : { duration: 0.45, ease: SALIDA }}
            />
          </div>
        </header>

        <div className={estilos.cuerpo}>
          <aside className={estilos.recorrido} aria-label="Partes del test">
            <h2 className={estilos.recorridoTitulo}>Tu test</h2>
            <p className={estilos.recorridoCuenta}>
              {respondidas} de {preguntas.length} respondidas
            </p>
            <ol className={estilos.bloques}>
              {bloques.map((b) => {
                const primero = preguntas.indexOf(b.preguntas[0]);
                const actual = b.nombre === pregunta.bloque;
                const hecho = !actual && b.preguntas.every((p) => preguntaLista(p, respuestas[p.id]) && (respuestas[p.id]?.length ?? 0) > 0);
                return (
                  <li key={b.nombre}>
                    <button
                      type="button"
                      className={estilos.bloque}
                      data-estado={actual ? "actual" : hecho ? "hecho" : "pendiente"}
                      aria-current={actual ? "step" : undefined}
                      onClick={() => ir(primero)}
                      disabled={enviando}
                    >
                      <span className={estilos.bloqueMarca} aria-hidden="true">
                        {hecho ? <IconoCheck tamano={13} /> : null}
                      </span>
                      <span className={estilos.bloqueNombre}>
                        {b.nombre}
                        {b.opcional && <small>Opcional</small>}
                      </span>
                      <span className={estilos.bloqueCuenta}>{b.preguntas.length}</span>
                    </button>
                  </li>
                );
              })}
            </ol>
            <p className={estilos.privacidad}>
              <IconoCandado tamano={16} />
              <span>
                Nadie ve tus respuestas. A otra persona solo le mostramos lo que tienen <strong>en común</strong>, y solo si
                también lo eligió.
              </span>
            </p>
          </aside>

          <section className={estilos.principal}>
            <AnimatePresence mode="wait" custom={direccion} initial={false}>
              <motion.div
                key={pregunta.id}
                className={estilos.pregunta}
                custom={direccion}
                variants={variantes}
                initial="entra"
                animate="centro"
                exit="sale"
                transition={{ duration: 0.26, ease: SALIDA }}
              >
                <h2 id={idTitulo} ref={titulo} tabIndex={-1} className={estilos.texto}>
                  {pregunta.texto}
                </h2>
                <p className={estilos.ayuda}>{ayuda}</p>

                <div className={estilos.opciones} role="group" aria-label={pregunta.texto}>
                  {pregunta.opciones.map((o, i) => {
                    const activa = elegidas.includes(o.id);
                    const lleno = !activa && pregunta.tipo === "MULTIPLE" && elegidas.length >= pregunta.maximo;
                    return (
                      <motion.button
                        key={o.id}
                        type="button"
                        className={estilos.opcion}
                        aria-pressed={activa}
                        disabled={enviando}
                        data-lleno={lleno || undefined}
                        onClick={() => setRespuestas((r) => ({ ...r, [pregunta.id]: alternar(pregunta, r[pregunta.id] ?? [], o.id) }))}
                        initial={reducir ? false : { opacity: 0, y: 8 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.3, ease: SALIDA, delay: reducir ? 0 : 0.03 * i }}
                        whileTap={{ scale: 0.96 }}
                      >
                        <span className={estilos.opcionMarca} aria-hidden="true">
                          {activa && <IconoCheck tamano={14} />}
                        </span>
                        {o.texto}
                      </motion.button>
                    );
                  })}
                </div>
                {pregunta.id === "creencias" && (
                  <p className={estilos.notaOpcional}>
                    Si coinciden, puede aparecer como algo que los conecta. Si prefieres no decirlo, sáltala.
                  </p>
                )}
              </motion.div>
            </AnimatePresence>

            {error && (
              <motion.p
                key={error}
                className={estilos.error}
                role="alert"
                initial={reducir ? false : { x: 0 }}
                animate={{ x: [0, -8, 8, -5, 5, 0] }}
                transition={{ duration: 0.42 }}
              >
                {error}
              </motion.p>
            )}

            <footer className={estilos.pie}>
              <button type="button" className={estilos.atras} onClick={() => ir(indice - 1)} disabled={indice === 0 || enviando} aria-label="Atrás">
                <IconoFlechaIzquierda tamano={18} />
                <span>Atrás</span>
              </button>
              {pregunta.opcional && (
                <button type="button" className={estilos.saltar} onClick={saltarPregunta} disabled={enviando}>
                  Prefiero no responder
                </button>
              )}
              <motion.button
                type="button"
                className={estilos.siguiente}
                onClick={siguiente}
                disabled={!lista || enviando}
                whileTap={{ scale: 0.97 }}
                transition={RESORTE_TOQUE}
              >
                <span>{enviando ? "Buscando coincidencias…" : esUltima ? "Ver con quién coincido" : "Siguiente"}</span>
                {!enviando && <IconoFlechaDerecha tamano={18} />}
              </motion.button>
            </footer>
          </section>
        </div>
      </div>
    </motion.div>,
    document.body,
  );
}

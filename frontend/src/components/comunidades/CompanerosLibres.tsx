"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import {
  IconoAjustes,
  IconoCalendario,
  IconoCandado,
  IconoCheck,
  IconoDestello,
  IconoPersonas,
  IconoReloj,
} from "@/components/app/Iconos";
import { Odometro, RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { TestAfinidad as Test, obtenerTest } from "@/lib/afinidad";
import { Conexion, Conexiones, listarConexiones, responderSaludo, saludar } from "@/lib/conexiones";
import { iniciales } from "@/lib/iniciales";
import { CompaneroCompatible, ResultadoMatching, buscarCompaneros, guardarConfiguracionMatching, obtenerConfiguracionMatching } from "@/lib/matching";
import { Puente, duracionMin, esDespuesDeClases, hhmm, listarPuentes } from "@/lib/puentes";
import { useEventoEnVivo } from "@/lib/tiempoReal";
import estilos from "./CompanerosLibres.module.css";
import { EscenaCoincidir } from "./EscenasComunidades";
import TestAfinidad from "./TestAfinidad";

const DIAS = ["", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];
const CORTOS = ["", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

function duracion(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

function puenteSiguiente(puentes: Puente[]): Puente | undefined {
  const ahora = new Date();
  const hoy = ahora.getDay();
  const minutos = ahora.getHours() * 60 + ahora.getMinutes();
  const orden = [...puentes].sort((a, b) => a.diaSemana - b.diaSemana || aMin(a.horaInicio) - aMin(b.horaInicio));
  return orden.find((p) => p.diaSemana > hoy || (p.diaSemana === hoy && aMin(p.horaFin) > minutos)) ?? orden[0];
}

type Carga =
  | { tipo: "cargando" }
  | { tipo: "error"; mensaje: string }
  | { tipo: "listo"; puentes: Puente[]; visible: boolean; test: Test; conexiones: Conexiones };

type Busqueda = { tipo: "buscando" } | { tipo: "error"; mensaje: string } | { tipo: "listo"; resultado: ResultadoMatching };

export default function CompanerosLibres() {
  const reducir = useReducedMotion();
  const [carga, setCarga] = useState<Carga>({ tipo: "cargando" });
  const [elegido, setElegido] = useState<number | null>(null);
  const [busqueda, setBusqueda] = useState<Busqueda>({ tipo: "buscando" });
  const [intento, setIntento] = useState(0);
  const [aceptando, setAceptando] = useState(false);
  const [errorAceptar, setErrorAceptar] = useState("");
  const [testAbierto, setTestAbierto] = useState(false);
  const [intentoCarga, setIntentoCarga] = useState(0);

  useEffect(() => {
    let activo = true;
    Promise.all([listarPuentes(), obtenerConfiguracionMatching(), obtenerTest(), listarConexiones()])
      .then(([puentes, config, test, conexiones]) => {
        if (!activo) return;
        setCarga({ tipo: "listo", puentes, visible: config.visible, test, conexiones });
        setElegido((actual) => (actual && puentes.some((p) => p.id === actual) ? actual : (puenteSiguiente(puentes)?.id ?? null)));
      })
      .catch((e: Error) => activo && setCarga({ tipo: "error", mensaje: e.message }));
    return () => {
      activo = false;
    };
  }, [intentoCarga]);

  const visible = carga.tipo === "listo" && carga.visible;
  const conTest = carga.tipo === "listo" && carga.test.completado;

  useEffect(() => {
    if (!visible || !conTest || elegido === null) return;
    let activo = true;
    buscarCompaneros(elegido)
      .then((resultado) => activo && setBusqueda({ tipo: "listo", resultado }))
      .catch((e: Error) => activo && setBusqueda({ tipo: "error", mensaje: e.message }));
    return () => {
      activo = false;
    };
  }, [visible, conTest, elegido, intento]);

  const refrescarConexiones = useCallback(async () => {
    try {
      const conexiones = await listarConexiones();
      setCarga((c) => (c.tipo === "listo" ? { ...c, conexiones } : c));
    } catch {
      // Si falla, la próxima acción o el próximo aviso lo pone al día.
    }
  }, []);

  // Un saludo nuevo o aceptado llega como aviso: se actualizan las listas sin recargar.
  useEventoEnVivo("avisosCambiaron", () => {
    void refrescarConexiones();
    setIntento((n) => n + 1);
  });

  function elegir(id: number) {
    if (id === elegido) return;
    setBusqueda({ tipo: "buscando" });
    setElegido(id);
  }

  function reintentar() {
    setBusqueda({ tipo: "buscando" });
    setIntento((n) => n + 1);
  }

  async function aceptarVisibilidad() {
    setAceptando(true);
    setErrorAceptar("");
    try {
      const c = await guardarConfiguracionMatching(true);
      setCarga((actual) => (actual.tipo === "listo" ? { ...actual, visible: c.visible } : actual));
    } catch (e) {
      setErrorAceptar((e as Error).message);
    } finally {
      setAceptando(false);
    }
  }

  function alTerminarTest(test: Test) {
    setTestAbierto(false);
    setCarga((c) => (c.tipo === "listo" ? { ...c, test } : c));
    setBusqueda({ tipo: "buscando" });
    setIntento((n) => n + 1);
  }

  /** Tras saludar o responder, la tarjeta cambia al instante y las listas se ponen al día. */
  function actualizarCompanero(usuarioId: number, cambio: Partial<CompaneroCompatible>) {
    setBusqueda((b) =>
      b.tipo === "listo"
        ? {
            tipo: "listo",
            resultado: {
              ...b.resultado,
              companeros: b.resultado.companeros.map((c) => (c.usuarioId === usuarioId ? { ...c, ...cambio } : c)),
            },
          }
        : b,
    );
    void refrescarConexiones();
  }

  const aparecer = {
    initial: reducir ? false : ({ opacity: 0, y: 12, filter: "blur(4px)" } as const),
    animate: { opacity: 1, y: 0, filter: "blur(0px)" },
    exit: reducir ? undefined : { opacity: 0, y: -8, filter: "blur(4px)" },
    transition: { duration: 0.28, ease: SALIDA },
  };

  return (
    <section className={estilos.seccion} aria-labelledby="libres-titulo">
      <div className={estilos.cabecera}>
        <div className={estilos.cabeceraTexto}>
          <span className={estilos.chip}>Tiempo libre y gustos en común</span>
          <h2 id="libres-titulo" className={estilos.subtitulo}>
            Compañeros libres contigo
          </h2>
          <p className={estilos.texto}>
            Gente de tu campus que está libre a la misma hora que tú y con quien tienes cosas en común. Salúdala y, si acepta, coordinen para verse.
          </p>
        </div>
        <EscenaCoincidir />
      </div>

      <AnimatePresence mode="wait" initial={false}>
        {carga.tipo === "cargando" && (
          <motion.div key="cargando" {...aparecer}>
            <Esqueleto />
          </motion.div>
        )}

        {carga.tipo === "error" && (
          <motion.div key="error" className={estilos.estado} role="alert" {...aparecer}>
            <strong>No pudimos cargar tus puentes</strong>
            <p>{carga.mensaje}</p>
            <button
              type="button"
              className={estilos.secundario}
              onClick={() => {
                setCarga({ tipo: "cargando" });
                setIntentoCarga((n) => n + 1);
              }}
            >
              Reintentar
            </button>
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length === 0 && (
          <motion.div key="sin-puentes" className={estilos.estado} {...aparecer}>
            <span className={estilos.estadoIcono}>
              <IconoCalendario tamano={22} />
            </span>
            <strong>Todavía no tienes puentes</strong>
            <p>Registra tu horario: cuando tengas un hueco de 45 minutos o más entre clases, aquí verás quién está libre contigo.</p>
            <Link href="/horario" className={estilos.primario}>
              Registrar mi horario
            </Link>
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length > 0 && !carga.visible && (
          <motion.div key="consentimiento" className={estilos.consentimiento} {...aparecer}>
            <span className={estilos.consentimientoIcono} aria-hidden="true">
              <IconoPersonas tamano={26} />
            </span>
            <div className={estilos.consentimientoTexto}>
              <strong>Encuentra a quién está libre contigo</strong>
              <p>
                Para ver a tus compañeros, ellos también tienen que poder encontrarte. Solo verán tu nombre corto, tu carrera, el tramo libre que
                comparten y lo que tienen en común. Tu correo, solo si aceptas conectar.
              </p>
              {errorAceptar && (
                <motion.p
                  key={errorAceptar}
                  className={estilos.error}
                  role="alert"
                  initial={reducir ? false : { x: 0 }}
                  animate={{ x: [0, -8, 8, -5, 5, 0] }}
                  transition={{ duration: 0.42 }}
                >
                  {errorAceptar}
                </motion.p>
              )}
              <motion.button
                type="button"
                className={estilos.primario}
                onClick={() => void aceptarVisibilidad()}
                disabled={aceptando}
                whileTap={{ scale: 0.97 }}
                transition={RESORTE_TOQUE}
              >
                {aceptando ? "Guardando…" : "Aceptar y continuar"}
              </motion.button>
              <small>Puedes cambiarlo cuando quieras en Perfil → Ajustes.</small>
            </div>
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length > 0 && carga.visible && !carga.test.completado && (
          <motion.div key="test" {...aparecer}>
            <InvitacionTest
              preguntas={carga.test.preguntas.length}
              minutos={carga.test.minutosEstimados}
              conBorrador={tieneBorrador()}
              onEmpezar={() => setTestAbierto(true)}
            />
          </motion.div>
        )}

        {carga.tipo === "listo" && carga.puentes.length > 0 && carga.visible && carga.test.completado && (
          <motion.div key="matching" className={estilos.matching} {...aparecer}>
            <TusConexiones conexiones={carga.conexiones} onRespondida={(c) => actualizarPorConexion(c)} />

            <div className={estilos.barraTest}>
              <span className={estilos.testHecho}>
                <IconoCheck tamano={15} />
                Test hecho
              </span>
              <button type="button" className={estilos.enlace} onClick={() => setTestAbierto(true)}>
                Editar mis respuestas
              </button>
            </div>

            <SelectorPuentes puentes={carga.puentes} elegido={elegido} onElegir={elegir} />
            <AnimatePresence mode="wait" initial={false}>
              <motion.div key={`${elegido}-${busqueda.tipo}`} {...aparecer}>
                <Resultado busqueda={busqueda} onReintentar={reintentar} onCambio={actualizarCompanero} />
              </motion.div>
            </AnimatePresence>
          </motion.div>
        )}
      </AnimatePresence>

      {testAbierto && carga.tipo === "listo" && (
        <TestAfinidad test={carga.test} onCerrar={() => setTestAbierto(false)} onListo={alTerminarTest} />
      )}
    </section>
  );

  /** Responder desde «Te quieren conocer» también actualiza la tarjeta de esa persona en la lista. */
  function actualizarPorConexion(c: Conexion) {
    setBusqueda((b) =>
      b.tipo === "listo"
        ? {
            tipo: "listo",
            resultado: {
              ...b.resultado,
              companeros: b.resultado.companeros.map((x) => (x.conexion?.id === c.id ? { ...x, conexion: { ...x.conexion, estado: c.estado } } : x)),
            },
          }
        : b,
    );
    void refrescarConexiones();
  }
}

function tieneBorrador(): boolean {
  try {
    return window.sessionStorage.getItem("breaku.testAfinidad.borrador") !== null;
  } catch {
    return false;
  }
}

/** Antes del test no se muestra a nadie: se invita a hacerlo y se explica qué se comparte. */
function InvitacionTest({ preguntas, minutos, conBorrador, onEmpezar }: { preguntas: number; minutos: number; conBorrador: boolean; onEmpezar: () => void }) {
  return (
    <div className={estilos.invitacion}>
      <div className={estilos.invitacionTexto}>
        <strong>Antes de presentarte a alguien, cuéntanos un poco de ti</strong>
        <p>
          Coincidir en tiempo libre es el primer paso. Para que valga la pena, te mostramos solo a quienes además comparten tus gustos, tu forma de
          ser o lo que buscas.
        </p>
        <ul className={estilos.datosTest}>
          <li>
            <IconoReloj tamano={16} />
            Unos {minutos} minutos
          </li>
          <li>
            <IconoDestello tamano={16} />
            {preguntas} preguntas de opción múltiple
          </li>
          <li>
            <IconoCandado tamano={16} />
            Valores y creencias, opcionales
          </li>
        </ul>
        <motion.button type="button" className={estilos.botonAmarillo} onClick={onEmpezar} whileTap={{ scale: 0.97 }} transition={RESORTE_TOQUE}>
          {conBorrador ? "Seguir con el test" : "Hacer el test"}
        </motion.button>
        <small>Nadie ve tus respuestas: a otra persona solo le mostramos lo que tienen en común.</small>
      </div>
      <div className={estilos.invitacionVista} aria-hidden="true">
        <span className={estilos.vistaTarjeta}>
          <span className={estilos.vistaAvatar}>AQ</span>
          <span className={estilos.vistaLineas}>
            <span />
            <span />
          </span>
          <b>87 %</b>
        </span>
        <span className={estilos.vistaChips}>
          <span>Videojuegos</span>
          <span>El mismo humor</span>
        </span>
      </div>
    </div>
  );
}

/** Saludos recibidos y conexiones aceptadas: lo que más importa, arriba. */
function TusConexiones({ conexiones, onRespondida }: { conexiones: Conexiones; onRespondida: (c: Conexion) => void }) {
  const { recibidas, aceptadas } = conexiones;
  if (recibidas.length === 0 && aceptadas.length === 0) return null;

  return (
    <div className={estilos.conexiones}>
      {recibidas.length > 0 && (
        <div className={estilos.grupoConexiones}>
          <h3 className={estilos.grupoTitulo}>Te quieren conocer</h3>
          <ul className={estilos.listaConexiones}>
            {recibidas.map((c) => (
              <li key={c.id}>
                <SaludoRecibido conexion={c} onRespondida={onRespondida} />
              </li>
            ))}
          </ul>
        </div>
      )}
      {aceptadas.length > 0 && (
        <div className={estilos.grupoConexiones}>
          <h3 className={estilos.grupoTitulo}>Tus conexiones</h3>
          <ul className={estilos.listaConexiones}>
            {aceptadas.map((c) => (
              <li key={c.id} className={estilos.conexion}>
                <span className={estilos.avatarConectado} aria-hidden="true">
                  {iniciales(c.nombre)}
                </span>
                <div className={estilos.conexionTexto}>
                  <strong>{c.nombre}</strong>
                  <small>{[c.carrera, `${c.afinidad} % de afinidad`].filter(Boolean).join(" · ")}</small>
                  <span className={estilos.cuando}>
                    <IconoReloj tamano={14} />
                    Libres el {DIAS[c.diaSemana]} de {hhmm(c.horaInicio)} a {hhmm(c.horaFin)}
                  </span>
                </div>
                {c.correo && (
                  <a className={estilos.escribir} href={`mailto:${c.correo}?subject=${encodeURIComponent("¡Hola! Nos conectó BreakU")}`}>
                    <span>Escribirle</span>
                    <small>{c.correo}</small>
                  </a>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

function SaludoRecibido({ conexion, onRespondida }: { conexion: Conexion; onRespondida: (c: Conexion) => void }) {
  const [enviando, setEnviando] = useState<"si" | "no" | null>(null);
  const [error, setError] = useState("");

  async function responder(aceptar: boolean) {
    setEnviando(aceptar ? "si" : "no");
    setError("");
    try {
      onRespondida(await responderSaludo(conexion.id, aceptar));
    } catch (e) {
      setError((e as Error).message);
      setEnviando(null);
    }
  }

  return (
    <div className={estilos.saludo}>
      <span className={estilos.avatar} aria-hidden="true">
        {iniciales(conexion.nombre)}
      </span>
      <div className={estilos.conexionTexto}>
        <strong>
          {conexion.nombre} te saludó · {conexion.afinidad} % de afinidad
        </strong>
        <small>
          {conexion.carrera ? `${conexion.carrera} · ` : ""}libres el {DIAS[conexion.diaSemana]} de {hhmm(conexion.horaInicio)} a {hhmm(conexion.horaFin)}
        </small>
        {conexion.enComun.length > 0 && <EnComun cosas={conexion.enComun} />}
        {error && (
          <p className={estilos.error} role="alert">
            {error}
          </p>
        )}
      </div>
      <div className={estilos.respuestas}>
        <button type="button" className={estilos.primario} onClick={() => void responder(true)} disabled={enviando !== null}>
          {enviando === "si" ? "Aceptando…" : "Aceptar"}
        </button>
        <button type="button" className={estilos.secundario} onClick={() => void responder(false)} disabled={enviando !== null}>
          Ahora no
        </button>
      </div>
    </div>
  );
}

function EnComun({ cosas }: { cosas: string[] }) {
  return (
    <ul className={estilos.enComun} aria-label="Lo que los conecta">
      {cosas.map((c) => (
        <li key={c}>{c}</li>
      ))}
    </ul>
  );
}

function SelectorPuentes({ puentes, elegido, onElegir }: { puentes: Puente[]; elegido: number | null; onElegir: (id: number) => void }) {
  const reducir = useReducedMotion();
  const orden = [...puentes].sort((a, b) => a.diaSemana - b.diaSemana || aMin(a.horaInicio) - aMin(b.horaInicio));

  return (
    <div className={estilos.selector} role="group" aria-label="Elige uno de tus puentes">
      {orden.map((p) => {
        const activo = p.id === elegido;
        return (
          <motion.button
            key={p.id}
            type="button"
            aria-pressed={activo}
            className={[activo ? estilos.puenteActivo : estilos.puente, esDespuesDeClases(p) ? estilos.tras : ""].join(" ")}
            onClick={() => onElegir(p.id)}
            whileTap={{ scale: 0.95 }}
            transition={RESORTE_TOQUE}
          >
            {activo && (
              <motion.span
                layoutId="puente-matching"
                className={estilos.pastilla}
                transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
              />
            )}
            <span className={estilos.puenteTexto}>
              <strong>
                {CORTOS[p.diaSemana]}
                {esDespuesDeClases(p) ? " · tras clases" : ""}
              </strong>
              <span>
                {hhmm(p.horaInicio)}–{hhmm(p.horaFin)}
              </span>
            </span>
          </motion.button>
        );
      })}
    </div>
  );
}

function Resultado({
  busqueda,
  onReintentar,
  onCambio,
}: {
  busqueda: Busqueda;
  onReintentar: () => void;
  onCambio: (usuarioId: number, cambio: Partial<CompaneroCompatible>) => void;
}) {
  const reducir = useReducedMotion();

  if (busqueda.tipo === "buscando") return <Esqueleto />;

  if (busqueda.tipo === "error")
    return (
      <div className={estilos.estado} role="alert">
        <strong>No pudimos buscar compañeros</strong>
        <p>{busqueda.mensaje}</p>
        <button type="button" className={estilos.secundario} onClick={onReintentar}>
          Reintentar
        </button>
      </div>
    );

  const { resultado } = busqueda;
  const { puente } = resultado;

  if (resultado.estado === "SIN_ZONA")
    return (
      <div className={estilos.estado}>
        <span className={estilos.estadoIcono}>
          <IconoAjustes tamano={22} />
        </span>
        <strong>Falta elegir tu campus</strong>
        <p>{resultado.mensaje}</p>
        <Link href="/configuracion/ubicacion" className={estilos.primario}>
          Elegir mi campus
        </Link>
      </div>
    );

  if (resultado.estado !== "OK")
    return (
      <div className={estilos.estado}>
        <span className={estilos.estadoIcono}>
          <IconoReloj tamano={22} />
        </span>
        <strong>
          Nadie con quien coincidas el {DIAS[puente.diaSemana]} de {hhmm(puente.horaInicio)} a {hhmm(puente.horaFin)}
        </strong>
        <p>{resultado.mensaje}</p>
      </div>
    );

  const inicio = aMin(puente.horaInicio);
  const total = duracionMin(puente);
  const cantidad = resultado.companeros.length;

  return (
    <div className={estilos.resultado}>
      <p className={estilos.resumen} aria-live="polite">
        <span className={estilos.cifra}>
          <Odometro valor={cantidad} />
        </span>
        {cantidad === 1 ? "persona coincide" : "personas coinciden"} contigo el {DIAS[puente.diaSemana]}, de {hhmm(puente.horaInicio)} a{" "}
        {hhmm(puente.horaFin)}
      </p>

      <motion.ol
        className={estilos.lista}
        initial={reducir ? false : "oculto"}
        animate="visible"
        variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.06 } } }}
      >
        {resultado.companeros.map((c, i) => {
          const desde = ((aMin(c.horaInicio) - inicio) / total) * 100;
          const ancho = (c.minutosEnComun / total) * 100;
          const meta = [c.carrera, c.semestre ? `Semestre ${c.semestre}` : null].filter(Boolean).join(" · ");
          return (
            <motion.li
              key={c.usuarioId}
              className={i === 0 ? estilos.tarjetaTop : estilos.tarjeta}
              variants={{
                oculto: { opacity: 0, y: 16, filter: "blur(4px)" },
                visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.42, ease: SALIDA } },
              }}
            >
              <div className={estilos.cabeza}>
                <span className={estilos.avatar} aria-hidden="true">
                  {iniciales(c.nombre)}
                </span>
                <div className={estilos.quien}>
                  {i === 0 && <span className={estilos.sello}>Más compatible</span>}
                  <strong>{c.nombre}</strong>
                  {meta && <small>{meta}</small>}
                </div>
                <span className={estilos.porcentaje} aria-label={`${c.compatibilidad} % compatibles`}>
                  {c.compatibilidad}
                  <small>%</small>
                </span>
              </div>

              <div className={estilos.conecta}>
                <span className={estilos.conectaTitulo}>Lo que los conecta</span>
                <EnComun cosas={c.enComun} />
              </div>

              <div className={estilos.linea}>
                <div className={estilos.riel} aria-hidden="true">
                  <motion.span
                    className={estilos.tramo}
                    style={{ left: `${desde}%`, width: `${ancho}%` }}
                    initial={reducir ? false : { scaleX: 0 }}
                    animate={{ scaleX: 1 }}
                    transition={{ duration: 0.7, ease: SALIDA, delay: 0.15 + i * 0.06 }}
                  />
                </div>
                <div className={estilos.horas} aria-hidden="true">
                  <span>{hhmm(puente.horaInicio)}</span>
                  <span>{hhmm(puente.horaFin)}</span>
                </div>
              </div>

              <p className={estilos.juntos}>
                <IconoReloj tamano={15} />
                <span>
                  Libres juntos de <strong>{hhmm(c.horaInicio)}</strong> a <strong>{hhmm(c.horaFin)}</strong> ·{" "}
                  <span className={estilos.duracion}>{duracion(c.minutosEnComun)}</span>
                </span>
              </p>

              <AccionConexion companero={c} puenteId={puente.id} onCambio={onCambio} />
            </motion.li>
          );
        })}
      </motion.ol>
      <p className={estilos.notaPrivacidad}>
        <IconoCandado tamano={15} />
        Al saludar, le llega un aviso con tu nombre corto. Si acepta, los dos ven el correo del otro para coordinar.
      </p>
    </div>
  );
}

/** El botón de cada tarjeta según el estado del saludo con esa persona. */
function AccionConexion({
  companero,
  puenteId,
  onCambio,
}: {
  companero: CompaneroCompatible;
  puenteId: number;
  onCambio: (usuarioId: number, cambio: Partial<CompaneroCompatible>) => void;
}) {
  const reducir = useReducedMotion();
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState("");
  const conexion = companero.conexion;

  async function enviarSaludo() {
    setEnviando(true);
    setError("");
    try {
      const c = await saludar(companero.usuarioId, puenteId);
      onCambio(companero.usuarioId, { conexion: { id: c.id, estado: c.estado, enviadaPorMi: true } });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setEnviando(false);
    }
  }

  async function responder(aceptar: boolean) {
    if (!conexion) return;
    setEnviando(true);
    setError("");
    try {
      const c = await responderSaludo(conexion.id, aceptar);
      onCambio(companero.usuarioId, { conexion: { ...conexion, estado: c.estado } });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setEnviando(false);
    }
  }

  let contenido: React.ReactNode;
  if (!conexion) {
    contenido = (
      <motion.button type="button" className={estilos.saludar} onClick={() => void enviarSaludo()} disabled={enviando} whileTap={{ scale: 0.97 }}>
        {enviando ? "Saludando…" : `Saludar a ${companero.nombre.split(" ")[0]}`}
      </motion.button>
    );
  } else if (conexion.estado === "ACEPTADA") {
    contenido = (
      <span className={estilos.estadoConexion} data-tono="exito">
        <IconoCheck tamano={15} />
        Conectados · su correo está arriba
      </span>
    );
  } else if (conexion.enviadaPorMi) {
    contenido = (
      <span className={estilos.estadoConexion}>
        <IconoCheck tamano={15} />
        Saludo enviado · te avisamos si acepta
      </span>
    );
  } else if (conexion.estado === "PENDIENTE") {
    contenido = (
      <div className={estilos.respuestas}>
        <span className={estilos.teSaludo}>Te saludó</span>
        <button type="button" className={estilos.primario} onClick={() => void responder(true)} disabled={enviando}>
          Aceptar
        </button>
        <button type="button" className={estilos.secundario} onClick={() => void responder(false)} disabled={enviando}>
          Ahora no
        </button>
      </div>
    );
  } else {
    contenido = <span className={estilos.estadoConexion}>No aceptaste su saludo</span>;
  }

  return (
    <div className={estilos.accion}>
      {contenido}
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
    </div>
  );
}

function Esqueleto() {
  return (
    <div className={estilos.esqueleto} aria-busy="true" aria-label="Buscando compañeros">
      {[0, 1, 2].map((i) => (
        <span key={i} className={estilos.hueso} style={{ animationDelay: `${i * 120}ms` }} />
      ))}
    </div>
  );
}

"use client";
import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from "react";
import { useRouter } from "next/navigation";
import { createPortal } from "react-dom";
import { AnimatePresence, motion, useAnimationControls, useReducedMotion } from "motion/react";
import { IconoPersonas, IconoRayo as IconoRayoApp } from "@/components/app/Iconos";
import { SALIDA } from "@/components/app/Movimiento";
import {
  INTERVALO_CONSULTA_MS,
  Notificacion,
  guardarConfiguracion,
  listarNotificaciones,
  marcarLeida,
  obtenerConfiguracion,
} from "@/lib/notificaciones";
import { hhmm } from "@/lib/puentes";
import estilos from "./Notificaciones.module.css";

function IconoCampana() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </svg>
  );
}

function IconoRayo() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8Z" />
    </svg>
  );
}

function formatearDuracion(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

// "Hoy" o "22/10", seguido del rango del puente.
function cuando(n: Notificacion): string {
  if (!n.fecha || !n.horaInicio || !n.horaFin) return "";
  const hoy = new Date().toLocaleDateString("en-CA"); // yyyy-MM-dd en hora local
  const [, mes, dia] = n.fecha.split("-");
  const etiquetaDia = n.fecha === hoy ? "Hoy" : `${dia}/${mes}`;
  return `${etiquetaDia} · ${hhmm(n.horaInicio)} – ${hhmm(n.horaFin)}`;
}

function IconoCerrar() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
      <path d="M6 6l12 12M18 6 6 18" />
    </svg>
  );
}

/** Cómo se presenta cada tipo de aviso del backend. */
const TIPOS = {
  PUENTE_DISPONIBLE: { titulo: "Puente disponible", clase: "puente", Icono: IconoRayoApp },
  INVITACION_COMUNIDAD: { titulo: "Invitación a una comunidad", clase: "invitacion", Icono: IconoPersonas },
  COMUNIDAD_CERRADA: { titulo: "Comunidad cerrada", clase: "cerrada", Icono: IconoPersonas },
} as const;

/** A dónde lleva "Ver": un puente abre sus recomendaciones; lo demás, Comunidades. */
function rutaDe(n: Notificacion): string {
  if (n.tipo === "PUENTE_DISPONIBLE") return n.duracionMinutos ? `/recomendaciones?min=${n.duracionMinutos}` : "/recomendaciones";
  return "/comunidades";
}

function haceCuanto(iso: string): string {
  const minutos = Math.max(0, Math.round((Date.now() - new Date(iso).getTime()) / 60000));
  if (minutos < 1) return "Recién";
  if (minutos < 60) return `Hace ${minutos} min`;
  const horas = Math.round(minutos / 60);
  if (horas < 24) return `Hace ${horas} h`;
  const dias = Math.round(horas / 24);
  return dias === 1 ? "Ayer" : `Hace ${dias} días`;
}

const sinSuscripcion = () => () => {};

type Props = {
  /** Cambia cada vez que se guarda el horario: se vuelve a consultar para retirar avisos de puentes que ya no existen. */
  recarga: number;
};

/**
 * Avisos de puente disponible (HU4): campana con contador, bandeja lateral (sin leer, marcar todo leído,
 * filtros y la preferencia) y un aviso emergente cuando llega uno nuevo. El backend crea el aviso al empezar el puente;
 * aquí se consulta periódicamente (todavía no hay push).
 */
export default function Notificaciones({ recarga }: Props) {
  const [lista, setLista] = useState<Notificacion[]>([]);
  const [activas, setActivas] = useState<boolean | null>(null); // null = aún no se sabe
  const [guardandoConfig, setGuardandoConfig] = useState(false);
  const [abierto, setAbierto] = useState(false);
  const [emergente, setEmergente] = useState<Notificacion | null>(null);
  const [error, setError] = useState("");
  const yaMostradas = useRef(new Set<number>()); // para no repetir el aviso emergente
  const [filtro, setFiltro] = useState<"todas" | "sin-leer">("todas");
  const botonCampana = useRef<HTMLButtonElement>(null);
  const cerrarBoton = useRef<HTMLButtonElement>(null);
  const router = useRouter();
  const reducir = useReducedMotion();
  const campana = useAnimationControls();
  const enCliente = useSyncExternalStore(sinSuscripcion, () => true, () => false);

  const consultar = useCallback(async (sigueActivo: () => boolean) => {
    try {
      const datos = await listarNotificaciones();
      if (!sigueActivo()) return;
      setLista(datos);
      setError("");
      // Llegan de la más reciente a la más antigua: se muestra la primera sin leer que no se haya mostrado.
      const nueva = datos.find((n) => !n.leida && !yaMostradas.current.has(n.id));
      datos.forEach((n) => yaMostradas.current.add(n.id));
      // Si el puente dejó de existir, su aviso ya no viene en la lista y se retira.
      setEmergente((actual) => nueva ?? (actual && datos.some((n) => n.id === actual.id) ? actual : null));
    } catch (e) {
      if (sigueActivo()) setError((e as Error).message);
    }
  }, []);

  // Consulta periódica mientras la pestaña está visible, y al volver a ella.
  useEffect(() => {
    let activo = true;
    const sigueActivo = () => activo;
    const revisar = () => {
      if (document.visibilityState === "visible") void consultar(sigueActivo); // consultar maneja sus errores
    };
    revisar();
    const intervalo = setInterval(revisar, INTERVALO_CONSULTA_MS);
    document.addEventListener("visibilitychange", revisar);
    return () => {
      activo = false;
      clearInterval(intervalo);
      document.removeEventListener("visibilitychange", revisar);
    };
  }, [consultar, recarga]);

  useEffect(() => {
    let activo = true;
    obtenerConfiguracion()
      .then((c) => activo && setActivas(c.notificacionesActivas))
      .catch((e: Error) => activo && setError(e.message));
    return () => { activo = false; };
  }, []);

  // La bandeja se cierra con Escape (el clic afuera lo atiende el fondo).
  useEffect(() => {
    if (!abierto) return;
    const alTeclear = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setAbierto(false);
        botonCampana.current?.focus();
      }
    };
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [abierto]);

  const leer = async (id: number) => {
    setLista((l) => l.map((n) => (n.id === id ? { ...n, leida: true } : n)));
    setEmergente((actual) => (actual?.id === id ? null : actual));
    try {
      await marcarLeida(id);
    } catch {
      // Sin efecto visible: la próxima consulta trae el estado real.
    }
  };

  const cambiarPreferencia = async () => {
    if (activas === null) return;
    const nueva = !activas;
    setActivas(nueva); // se refleja al instante; si el servidor falla, se revierte
    if (!nueva) setEmergente(null);
    setGuardandoConfig(true);
    setError("");
    try {
      const c = await guardarConfiguracion({ notificacionesActivas: nueva });
      setActivas(c.notificacionesActivas);
    } catch (e) {
      setActivas(!nueva);
      setError((e as Error).message);
    } finally {
      setGuardandoConfig(false);
    }
  };

  const noLeidas = lista.filter((n) => !n.leida).length;
  const visibles = filtro === "sin-leer" ? lista.filter((n) => !n.leida) : lista;

  // La campana se balancea cuando llega un aviso nuevo.
  useEffect(() => {
    if (emergente && !reducir) void campana.start({ rotate: [0, -16, 13, -9, 6, 0], transition: { duration: 0.7 } });
  }, [emergente, reducir, campana]);

  // Al abrir, el foco va a "Cerrar"; al cerrar, vuelve a la campana.
  useEffect(() => {
    // focusVisible: false evita el anillo de foco cuando se abrió con el mouse.
    if (abierto) cerrarBoton.current?.focus({ focusVisible: false } as FocusOptions);
  }, [abierto]);

  function cerrar() {
    setAbierto(false);
    botonCampana.current?.focus();
  }

  const marcarTodas = async () => {
    const pendientes = lista.filter((n) => !n.leida);
    setLista((l) => l.map((n) => ({ ...n, leida: true })));
    setEmergente(null);
    await Promise.allSettled(pendientes.map((n) => marcarLeida(n.id)));
  };

  const ver = (n: Notificacion) => {
    void leer(n.id);
    setAbierto(false);
    router.push(rutaDe(n));
  };

  return (
    <div className={estilos.contenedor}>
      <motion.button
        ref={botonCampana}
        type="button"
        className={estilos.campana}
        aria-expanded={abierto}
        aria-controls="bandeja-notificaciones"
        aria-label={noLeidas ? `Notificaciones, ${noLeidas} sin leer` : "Notificaciones"}
        onClick={() => setAbierto((a) => !a)}
        animate={campana}
        whileTap={{ scale: 0.92 }}
      >
        <IconoCampana />
        <AnimatePresence>
          {noLeidas > 0 && (
            <motion.span
              key="contador"
              className={estilos.contador}
              aria-hidden="true"
              initial={reducir ? false : { scale: 0.4, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.4, opacity: 0 }}
              transition={{ type: "spring", stiffness: 500, damping: 22 }}
            >
              {noLeidas > 9 ? "9+" : noLeidas}
            </motion.span>
          )}
        </AnimatePresence>
      </motion.button>

      {/* En un portal: la barra superior usa backdrop-filter y atraparía los elementos fijos dentro de ella. */}
      {enCliente &&
        createPortal(
          <>
            <AnimatePresence>
              {abierto && (
                <motion.div
                  key="fondo"
                  className={estilos.fondo}
                  onClick={cerrar}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  transition={{ duration: 0.2 }}
                />
              )}
              {abierto && (
                <motion.aside
                  key="bandeja"
                  id="bandeja-notificaciones"
                  className={estilos.bandeja}
                  role="dialog"
                  aria-modal="true"
                  aria-labelledby="bandeja-titulo"
                  initial={reducir ? { opacity: 0 } : { transform: "translateX(100%)" }}
                  animate={reducir ? { opacity: 1 } : { transform: "translateX(0%)" }}
                  exit={reducir ? { opacity: 0 } : { transform: "translateX(100%)" }}
                  transition={{ type: "spring", duration: 0.45, bounce: 0.08 }}
                >
                  <header className={estilos.cabecera}>
                    <div>
                      <h2 id="bandeja-titulo" className={estilos.titulo}>
                        Notificaciones
                      </h2>
                      <p className={estilos.subtitulo}>{noLeidas ? `${noLeidas} sin leer` : "Todo al día"}</p>
                    </div>
                    <div className={estilos.cabeceraAcciones}>
                      {noLeidas > 0 && (
                        <button type="button" className={estilos.marcarTodo} onClick={() => void marcarTodas()}>
                          Marcar todo leído
                        </button>
                      )}
                      <button ref={cerrarBoton} type="button" className={estilos.cerrar} onClick={cerrar} aria-label="Cerrar notificaciones">
                        <IconoCerrar />
                      </button>
                    </div>
                  </header>

                  <div className={estilos.filtros} role="tablist" aria-label="Filtrar notificaciones">
                    {(
                      [
                        ["todas", `Todas (${lista.length})`],
                        ["sin-leer", `Sin leer (${noLeidas})`],
                      ] as const
                    ).map(([id, etiqueta]) => (
                      <button
                        key={id}
                        type="button"
                        role="tab"
                        aria-selected={filtro === id}
                        className={filtro === id ? estilos.filtroActivo : estilos.filtro}
                        onClick={() => setFiltro(id)}
                      >
                        {filtro === id && (
                          <motion.span
                            layoutId="filtro-bandeja"
                            className={estilos.filtroPastilla}
                            transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
                          />
                        )}
                        <span className={estilos.filtroTexto}>{etiqueta}</span>
                      </button>
                    ))}
                  </div>

                  <div className={estilos.cuerpo}>
                    {noLeidas > 0 && filtro === "todas" && (
                      <p className={estilos.nuevas}>
                        <span aria-hidden="true" /> {noLeidas === 1 ? "1 notificación nueva" : `${noLeidas} notificaciones nuevas`}
                      </p>
                    )}

                    {error && (
                      <p className={estilos.error} role="alert">
                        {error}
                      </p>
                    )}

                    {visibles.length === 0 ? (
                      <div className={estilos.vacio}>
                        <span className={estilos.vacioIcono} aria-hidden="true">
                          <IconoCampana />
                        </span>
                        <strong>{filtro === "sin-leer" ? "No tienes avisos sin leer" : "Sin avisos por ahora"}</strong>
                        <span>Te avisaremos en cuanto empiece uno de tus puentes.</span>
                      </div>
                    ) : (
                      <motion.ul
                        className={estilos.lista}
                        initial={reducir ? false : "oculto"}
                        animate="visible"
                        variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.05, delayChildren: 0.12 } } }}
                      >
                        <AnimatePresence initial={false}>
                          {visibles.map((n) => {
                            const tipo = TIPOS[n.tipo];
                            return (
                              <motion.li
                                key={n.id}
                                layout={!reducir}
                                className={`${estilos.item} ${estilos[tipo.clase]} ${n.leida ? "" : estilos.noLeida}`}
                                variants={{
                                  oculto: { opacity: 0, x: 24 },
                                  visible: { opacity: 1, x: 0, transition: { duration: 0.35, ease: SALIDA } },
                                }}
                                exit={{ opacity: 0, x: 24, transition: { duration: 0.2 } }}
                              >
                                <span className={estilos.itemIcono} aria-hidden="true">
                                  <tipo.Icono tamano={18} />
                                </span>
                                <div className={estilos.itemTexto}>
                                  <strong>
                                    {!n.leida && <span className={estilos.punto} aria-label="Sin leer" />}
                                    {tipo.titulo}
                                  </strong>
                                  <p>{n.mensaje}</p>
                                  {cuando(n) && <span className={estilos.rango}>{cuando(n)}</span>}
                                  <div className={estilos.itemPie}>
                                    <time dateTime={n.creadaEn}>{haceCuanto(n.creadaEn)}</time>
                                    <div className={estilos.itemAcciones}>
                                      {!n.leida && (
                                        <button type="button" className={estilos.leer} onClick={() => void leer(n.id)}>
                                          Marcar leída
                                        </button>
                                      )}
                                      <button type="button" className={estilos.ver} onClick={() => ver(n)}>
                                        Ver
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              </motion.li>
                            );
                          })}
                        </AnimatePresence>
                      </motion.ul>
                    )}
                  </div>

                  <footer className={estilos.pieBandeja}>
                    <label className={estilos.preferencia}>
                      <span>Avisarme cuando empiece un puente</span>
                      <input
                        type="checkbox"
                        role="switch"
                        className={estilos.oculto}
                        checked={activas ?? false}
                        disabled={activas === null || guardandoConfig}
                        onChange={cambiarPreferencia}
                      />
                      <span className={activas ? estilos.rielActivo : estilos.riel} aria-hidden="true">
                        <motion.span
                          className={estilos.perilla}
                          animate={{ x: activas ? 20 : 0 }}
                          transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 600, damping: 30 }}
                        />
                      </span>
                    </label>
                    {activas === false && (
                      <p className={estilos.nota}>No recibirás avisos, pero tus puentes se siguen detectando en tu horario.</p>
                    )}
                    <p className={estilos.nota}>Los avisos de puente se generan solos cuando empieza un hueco libre entre clases.</p>
                  </footer>
                </motion.aside>
              )}
            </AnimatePresence>

            <AnimatePresence>
              {emergente && !abierto && (
                <motion.div
                  key={emergente.id}
                  className={estilos.emergente}
                  role="alert"
                  initial={reducir ? { opacity: 0 } : { opacity: 0, y: 40, scale: 0.96 }}
                  animate={{ opacity: 1, y: 0, scale: 1 }}
                  exit={reducir ? { opacity: 0 } : { opacity: 0, y: 24, scale: 0.97 }}
                  transition={{ type: "spring", stiffness: 380, damping: 28 }}
                >
                  <span className={estilos.emergenteIcono}>
                    <IconoRayo />
                  </span>
                  <div className={estilos.emergenteTexto}>
                    <strong>¡Puente disponible!</strong>
                    {emergente.duracionMinutos !== null && emergente.horaInicio && emergente.horaFin ? (
                      <span>
                        Tienes <b>{formatearDuracion(emergente.duracionMinutos)}</b> libres, de {hhmm(emergente.horaInicio)} a{" "}
                        {hhmm(emergente.horaFin)}.
                      </span>
                    ) : (
                      <span>{emergente.mensaje}</span>
                    )}
                  </div>
                  <button type="button" className={estilos.entendido} onClick={() => leer(emergente.id)}>
                    Entendido
                  </button>
                </motion.div>
              )}
            </AnimatePresence>
          </>,
          document.body,
        )}
    </div>
  );
}

"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { Suspense, useCallback, useEffect, useMemo, useState } from "react";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoCheck, IconoFlechaDerecha, IconoFlechaIzquierda, IconoPersonas, IconoReloj } from "@/components/app/Iconos";
import { Entrada, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import AsistenciaComunidad from "@/components/comunidades/AsistenciaComunidad";
import CompanerosLibres from "@/components/comunidades/CompanerosLibres";
import DatoEditable from "@/components/comunidades/DatoEditable";
import InvitacionGrupo from "@/components/comunidades/InvitacionGrupo";
import ParticipantesComunidad from "@/components/comunidades/ParticipantesComunidad";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import {
  ActividadExtraida,
  CampoActividad,
  RespuestaExtraccion,
  extraerActividad,
  formatearDuracion,
  revisarActividad,
} from "@/lib/actividades";
import {
  ComunidadActiva,
  EJEMPLOS_SOLICITUD,
  MAXIMO_SOLICITUD,
  RespuestaAsistencia,
  ResultadoDescubrir,
  buscarDeNuevo,
  categoriaDe,
  listarComunidades,
} from "@/lib/comunidades";
import { ActividadCreada, comoCompleta, crearActividad } from "@/lib/crearActividad";
import { iniciales } from "@/lib/iniciales";
import { guardarConfiguracionMatching } from "@/lib/matching";
import { useSesion } from "@/lib/useSesion";
import { EventoComunidad, useComunidadesRealtime } from "@/lib/useComunidadesRealtime";
import estilos from "./comunidades.module.css";

type Pestana = "libres" | "crear" | "descubrir";

const PESTANAS: readonly Pestana[] = ["libres", "crear", "descubrir"];

function etiquetaEstado(c: ComunidadActiva): string | null {
  if (c.esMia) return "La creaste tú";
  if (c.miEstado === "PENDIENTE") return "Te invitaron";
  if (c.miEstado === "CONFIRMADO") return "Vas";
  if (c.miEstado === "RECHAZADO") return "No vas";
  return c.confirmados >= c.maxParticipantes ? "Completa" : null;
}

export default function PaginaComunidades() {
  return (
    <Suspense fallback={null}>
      <Comunidades />
    </Suspense>
  );
}

function Comunidades() {
  const reducir = useReducedMotion();
  const { sesion } = useSesion();
  const aviso = useAviso();
  const parametros = useSearchParams();
  const [pestana, setPestana] = useState<Pestana>("libres");
  const [destacada, setDestacada] = useState<number | null>(null);
  const [verQuienes, setVerQuienes] = useState<ComunidadActiva | null>(null);
  const pedida = parametros.get("pestana");
  const comunidadPedida = Number(parametros.get("comunidad")) || null;
  const clavePedida = `${pedida}|${comunidadPedida}`;
  const [claveVista, setClaveVista] = useState<string | null>(null);
  if (clavePedida !== claveVista) {
    setClaveVista(clavePedida);
    if (PESTANAS.includes(pedida as Pestana)) setPestana(pedida as Pestana);
    setDestacada(comunidadPedida);
  }
  const [texto, setTexto] = useState("");
  const [descubrir, setDescubrir] = useState<ResultadoDescubrir | null>(null);
  const [cargandoDescubrir, setCargandoDescubrir] = useState(false);
  const [errorDescubrir, setErrorDescubrir] = useState("");
  const [extraccion, setExtraccion] = useState<RespuestaExtraccion | null>(null);
  const [respuesta, setRespuesta] = useState("");
  const [buscando, setBuscando] = useState(false);
  const [error, setError] = useState("");
  const [editando, setEditando] = useState<CampoActividad | null>(null);
  const [creando, setCreando] = useState(false);
  const [creada, setCreada] = useState<ActividadCreada | null>(null);
  const [invitando, setInvitando] = useState(false);

  async function consultar(textoAEnviar: string, anterior: ActividadExtraida | null, consultaAnteriorId: number | null = null) {
    setBuscando(true);
    setError("");
    try {
      setExtraccion(await extraerActividad(textoAEnviar, anterior, consultaAnteriorId));
      setRespuesta("");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Hubo un error inesperado. Inténtalo de nuevo.");
    } finally {
      setBuscando(false);
    }
  }

  function enviar(e: React.FormEvent) {
    e.preventDefault();
    if (!texto.trim() || buscando) return;
    setExtraccion(null);
    void consultar(texto.trim(), null);
  }

  function responder(e: React.FormEvent) {
    e.preventDefault();
    if (!extraccion || !respuesta.trim() || buscando) return;
    void consultar(respuesta.trim(), extraccion.actividad, extraccion.consultaId);
  }

  async function guardarDato(campo: CampoActividad, valor: string) {
    setEditando(null);
    if (!extraccion) return;
    const actual = extraccion.actividad;
    const cambiada: ActividadExtraida = {
      ...actual,
      [campo]: campo === "duracionMin" || campo === "maxParticipantes" ? Number(valor) : valor,
    };
    if (cambiada[campo] === actual[campo]) return;
    setBuscando(true);
    setError("");
    try {
      setExtraccion(await revisarActividad(texto.trim(), cambiada, extraccion.consultaId));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No pudimos revisar el cambio. Inténtalo de nuevo.");
    } finally {
      setBuscando(false);
    }
  }

  async function confirmar() {
    if (!extraccion || extraccion.estado !== "COMPLETO" || creando) return;
    const completa = comoCompleta(extraccion.actividad);
    if (!completa) return;
    setCreando(true);
    setError("");
    try {
      setCreada(await crearActividad(completa, texto.trim(), extraccion.consultaId));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No pudimos crear la actividad. Inténtalo de nuevo.");
    } finally {
      setCreando(false);
    }
  }

  async function invitar(aceptarMatching = false) {
    if (!creada || invitando) return;
    setInvitando(true);
    setError("");
    try {
      if (aceptarMatching) await guardarConfiguracionMatching(true);
      const invitacion = await buscarDeNuevo(creada.id);
      setCreada((actual) => (actual ? { ...actual, invitacion } : actual));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No pudimos buscar compañeros. Inténtalo de nuevo.");
    } finally {
      setInvitando(false);
    }
  }

  function reiniciar() {
    setExtraccion(null);
    setEditando(null);
    setCreada(null);
    setTexto("");
    setRespuesta("");
    setError("");
  }

  const entendida = extraccion && extraccion.estado !== "NO_ENTENDIDO" ? extraccion : null;

  async function cargarDescubrir(aceptarMatching = false) {
    setCargandoDescubrir(true);
    setErrorDescubrir("");
    try {
      if (aceptarMatching) await guardarConfiguracionMatching(true);
      setDescubrir(await listarComunidades());
    } catch (e) {
      setErrorDescubrir(e instanceof Error ? e.message : "No pudimos cargar las comunidades. Inténtalo de nuevo.");
    } finally {
      setCargandoDescubrir(false);
    }
  }

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    listarComunidades()
      .then((r) => {
        if (activo) setDescubrir(r);
      })
      .catch(() => undefined);
    return () => {
      activo = false;
    };
  }, [sesion, clavePedida]);

  const hayDestacada = destacada !== null && descubrir?.comunidades.some((c) => c.id === destacada);
  useEffect(() => {
    if (!hayDestacada || pestana !== "descubrir") return;
    document.getElementById(`comunidad-${destacada}`)?.scrollIntoView({ block: "center", behavior: reducir ? "auto" : "smooth" });
    const t = setTimeout(() => setDestacada(null), 2600);
    return () => clearTimeout(t);
  }, [hayDestacada, destacada, pestana, reducir]);

  function alResponder(r: RespuestaAsistencia) {
    setDescubrir((actual) =>
      actual
        ? {
            ...actual,
            comunidades: actual.comunidades.map((c) =>
              c.id === r.comunidadId ? { ...c, miEstado: r.miEstado, confirmados: r.confirmados } : c,
            ),
          }
        : actual,
    );
    aviso.mostrar(r.mensaje);
  }

  const comunidadIds = useMemo(() => descubrir?.comunidades.map((comunidad) => comunidad.id) ?? [], [descubrir]);
  const alActualizarComunidad = useCallback((evento: EventoComunidad) => {
    setDescubrir((actual) => {
      if (!actual) return actual;
      if (evento.estado !== "ACTIVA") {
        return { ...actual, comunidades: actual.comunidades.filter((comunidad) => comunidad.id !== evento.comunidadId) };
      }
      return {
        ...actual,
        comunidades: actual.comunidades.map((comunidad) =>
          comunidad.id === evento.comunidadId ? { ...comunidad, confirmados: evento.confirmados } : comunidad,
        ),
      };
    });
  }, []);
  useComunidadesRealtime(comunidadIds, alActualizarComunidad);

  const activas = descubrir?.estado === "OK" ? descubrir.comunidades : [];
  const nombresCaras = [...new Set(activas.map((c) => (c.esMia ? (sesion?.usuario.nombre ?? "Tú") : c.creador)))].slice(0, 4);
  const textoBanner =
    descubrir?.estado === "OK"
      ? `${activas.length} ${activas.length === 1 ? "comunidad activa" : "comunidades activas"} hoy`
      : descubrir?.estado === "SIN_RESULTADOS"
        ? "Aún no hay comunidades hoy"
        : null;

  function elegirPestana(id: Pestana) {
    setPestana(id);
    if (id === "descubrir") void cargarDescubrir();
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
          {textoBanner && (
            <div className={estilos.caras} role="status">
              {nombresCaras.map((n, i) => (
                <motion.span
                  key={n}
                  className={estilos.cara}
                  style={{ zIndex: 4 - i }}
                  aria-hidden="true"
                  initial={reducir ? false : { opacity: 0, transform: "translateX(-12px) scale(0.8)" }}
                  animate={{ opacity: 1, transform: "translateX(0px) scale(1)" }}
                  transition={{ type: "spring", duration: 0.5, bounce: 0.3, delay: 0.3 + i * 0.08 }}
                >
                  {iniciales(n)}
                </motion.span>
              ))}
              <span className={estilos.carasTexto}>{textoBanner}</span>
            </div>
          )}
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
              onClick={() => elegirPestana(id)}
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
                {}
                {error && (
                  <p className={estilos.error} role="alert">
                    {error}
                  </p>
                )}

                <AnimatePresence mode="wait" initial={false}>
                  {creada ? (
                    <motion.div
                      key="creada"
                      className={estilos.resultado}
                      initial={reducir ? false : { opacity: 0, transform: "translateY(12px) scale(0.97)" }}
                      animate={{ opacity: 1, transform: "translateY(0px) scale(1)" }}
                      transition={{ type: "spring", duration: 0.5, bounce: 0.2 }}
                    >
                      <span className={estilos.resultadoIcono}>
                        {creada.invitacion?.estado === "INVITADOS" ? <IconoPersonas tamano={22} /> : <IconoCheck tamano={22} />}
                      </span>
                      <div className={estilos.entendida}>
                        <strong>{creada.invitacion?.estado === "INVITADOS" ? "¡Grupo armado!" : "¡Actividad creada!"}</strong>
                        <p>
                          “{creada.actividad}” quedó creada para hoy, de {creada.horaInicio} a {creada.horaFin}, con lugar para hasta{" "}
                          {creada.maxParticipantes} personas.
                        </p>
                        {creada.invitacion && (
                          <InvitacionGrupo
                            invitacion={creada.invitacion}
                            buscando={invitando}
                            onBuscar={() => void invitar()}
                            onAceptarYBuscar={() => void invitar(true)}
                          />
                        )}
                      </div>
                      <button type="button" className={estilos.otra} onClick={reiniciar} disabled={invitando}>
                        Crear otra
                      </button>
                    </motion.div>
                  ) : entendida ? (
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
                          <DatoEditable
                            etiqueta="Actividad"
                            tipo="texto"
                            largoMaximo={100}
                            mostrar={entendida.actividad.actividad}
                            inicial={entendida.actividad.actividad ?? ""}
                            editando={editando === "actividad"}
                            deshabilitado={buscando || creando}
                            onEditar={() => setEditando("actividad")}
                            onGuardar={(v) => void guardarDato("actividad", v)}
                            onCancelar={() => setEditando(null)}
                          />
                          <DatoEditable
                            etiqueta="Hora"
                            tipo="hora"
                            mostrar={
                              entendida.actividad.horaInicio &&
                              `${entendida.actividad.horaInicio}${entendida.supuestos.includes("horaInicio") ? " (ahora)" : ""}`
                            }
                            inicial={entendida.actividad.horaInicio ?? ""}
                            editando={editando === "horaInicio"}
                            deshabilitado={buscando || creando}
                            onEditar={() => setEditando("horaInicio")}
                            onGuardar={(v) => void guardarDato("horaInicio", v)}
                            onCancelar={() => setEditando(null)}
                          />
                          <DatoEditable
                            etiqueta="Duración"
                            tipo="numero"
                            sufijo="min"
                            mostrar={entendida.actividad.duracionMin ? formatearDuracion(entendida.actividad.duracionMin) : null}
                            inicial={entendida.actividad.duracionMin?.toString() ?? ""}
                            editando={editando === "duracionMin"}
                            deshabilitado={buscando || creando}
                            onEditar={() => setEditando("duracionMin")}
                            onGuardar={(v) => void guardarDato("duracionMin", v)}
                            onCancelar={() => setEditando(null)}
                          />
                          <DatoEditable
                            etiqueta="Participantes"
                            tipo="numero"
                            sufijo="personas"
                            mostrar={entendida.actividad.maxParticipantes ? `hasta ${entendida.actividad.maxParticipantes}` : null}
                            inicial={entendida.actividad.maxParticipantes?.toString() ?? ""}
                            editando={editando === "maxParticipantes"}
                            deshabilitado={buscando || creando}
                            onEditar={() => setEditando("maxParticipantes")}
                            onGuardar={(v) => void guardarDato("maxParticipantes", v)}
                            onCancelar={() => setEditando(null)}
                          />
                        </ul>
                        <small className={estilos.pista}>Toca un dato para corregirlo ahí mismo.</small>

                        {}
                        <form className={estilos.respuesta} onSubmit={responder}>
                          <label htmlFor="respuesta-dato" className={estilos.pregunta}>
                            {entendida.estado === "INCOMPLETO" ? entendida.mensaje : "¿Quieres cambiar algo? Dilo en tus palabras"}
                          </label>
                          <div className={estilos.respuestaFila}>
                            <input
                              id="respuesta-dato"
                              className={estilos.respuestaCampo}
                              value={respuesta}
                              maxLength={MAXIMO_SOLICITUD}
                              onChange={(e) => setRespuesta(e.target.value)}
                              placeholder={entendida.estado === "INCOMPLETO" ? "Tu respuesta" : "Ej: mejor a las 5 y somos 6"}
                              autoComplete="off"
                              autoFocus={entendida.estado === "INCOMPLETO"}
                              disabled={buscando}
                            />
                            <button type="submit" className={estilos.responder} disabled={!respuesta.trim() || buscando}>
                              {buscando ? "Revisando…" : entendida.estado === "INCOMPLETO" ? "Responder" : "Aplicar"}
                            </button>
                          </div>
                        </form>

                        <div className={estilos.acciones}>
                          {entendida.estado === "COMPLETO" && (
                            <button type="button" className={estilos.confirmar} onClick={() => void confirmar()} disabled={creando || buscando}>
                              {creando ? "Creando…" : "Confirmar y crear"}
                            </button>
                          )}
                          <button type="button" className={estilos.secundario} onClick={reiniciar} disabled={buscando || creando}>
                            {entendida.estado === "COMPLETO" ? "Descartar" : "Escribir otra"}
                          </button>
                        </div>
                      </div>
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
              <section className={estilos.descubrir} aria-label="Comunidades activas de hoy" aria-busy={cargandoDescubrir}>
                {errorDescubrir && (
                  <div className={estilos.error} role="alert">
                    <span>{errorDescubrir}</span>{" "}
                    <button type="button" className={estilos.reintentar} onClick={() => void cargarDescubrir()}>
                      Reintentar
                    </button>
                  </div>
                )}
                {cargandoDescubrir && !descubrir && (
                  <p className={estilos.cargando} role="status">
                    Buscando las comunidades de hoy…
                  </p>
                )}
                {descubrir?.mensaje && (
                  <div className={estilos.avisoDescubrir} role="status">
                    <span>{descubrir.mensaje}</span>
                    {descubrir.estado === "SIN_ZONA" && (
                      <Link href="/campus" className={estilos.accionAviso}>
                        Elegir mi campus
                      </Link>
                    )}
                    {descubrir.estado === "NO_VISIBLE" && (
                      <button type="button" className={estilos.accionAviso} onClick={() => void cargarDescubrir(true)} disabled={cargandoDescubrir}>
                        Aceptar y ver
                      </button>
                    )}
                    {descubrir.estado === "SIN_RESULTADOS" && (
                      <button type="button" className={estilos.accionAviso} onClick={() => elegirPestana("crear")}>
                        Crear una
                      </button>
                    )}
                  </div>
                )}
                {descubrir && descubrir.comunidades.length > 0 && (
                  <motion.ul
                    className={estilos.lista}
                    initial={reducir ? false : "oculto"}
                    animate="visible"
                    variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.07 } } }}
                  >
                    {descubrir.comunidades.map((c) => {
                      const estado = etiquetaEstado(c);
                      return (
                        <motion.li
                          key={c.id}
                          id={`comunidad-${c.id}`}
                          className={`${estilos.comunidad} ${c.id === destacada ? estilos.destacada : ""} lugar-arte`}
                          variants={{
                            oculto: { opacity: 0, y: 16, filter: "blur(4px)" },
                            visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: { duration: 0.45, ease: SALIDA } },
                          }}
                          whileHover={reducir ? undefined : { y: -4 }}
                        >
                          <ArteCategoria categoria={categoriaDe(c.actividad)} />
                          <div className={estilos.comunidadCuerpo}>
                            <h3>{c.actividad}</h3>
                            <p className={estilos.meta}>
                              <span>
                                <IconoReloj tamano={14} /> Hoy, {c.horaInicio}–{c.horaFin}
                              </span>
                            </p>
                            <div className={estilos.pieComunidad}>
                              <span className={estilos.miembros} aria-label={`${c.confirmados} de ${c.maxParticipantes} personas`}>
                                <IconoPersonas tamano={16} />
                                <small>
                                  {c.confirmados}/{c.maxParticipantes}
                                </small>
                              </span>
                              {estado && (
                                <span className={c.esMia || c.miEstado === "CONFIRMADO" ? estilos.estadoMio : estilos.estadoComunidad}>
                                  {estado}
                                </span>
                              )}
                            </div>
                            <small className={estilos.creador}>{c.esMia ? "Creada por ti" : `Creada por ${c.creador}`}</small>
                            <AsistenciaComunidad
                              comunidad={c}
                              onRespondida={alResponder}
                              onDesactualizada={() => void cargarDescubrir()}
                              onVerQuienes={() => setVerQuienes(c)}
                            />
                          </div>
                        </motion.li>
                      );
                    })}
                  </motion.ul>
                )}
              </section>
            )}
          </motion.div>
        </AnimatePresence>
      </Pieza>

      <AnimatePresence>
        {verQuienes && (
          <ParticipantesComunidad
            key={verQuienes.id}
            comunidadId={verQuienes.id}
            actividad={verQuienes.actividad}
            miNombre={sesion?.usuario.nombre ?? "Tú"}
            onCerrar={() => setVerQuienes(null)}
          />
        )}
      </AnimatePresence>
      <AvisoFlotante texto={aviso.texto} />
    </Entrada>
  );
}

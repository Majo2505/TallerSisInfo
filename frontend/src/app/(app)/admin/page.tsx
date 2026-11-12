"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { Suspense, useCallback, useEffect, useState } from "react";
import EliminarLugar from "@/components/admin/EliminarLugar";
import FormularioLugar from "@/components/admin/FormularioLugar";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import {
  IconoBandera,
  IconoBuscar,
  IconoDestello,
  IconoMas,
  IconoPersonas,
  IconoPin,
  IconoPulgarAbajo,
  IconoPulgarArriba,
} from "@/components/app/Iconos";
import { Entrada, Odometro, Pieza, RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import {
  ConsultaRag,
  EstadoLugar,
  LugarAdmin,
  MOTIVO_REPORTE,
  MetricasAdmin,
  MotivoRechazo,
  ReporteAdmin,
  ResumenAdmin,
  cambiarEstadoLugar,
  cambiarValidacionLugar,
  listarConsultasRag,
  listarLugaresAdmin,
  listarReportes,
  resolverReporte,
  obtenerMetricas,
  obtenerResumen,
} from "@/lib/admin";
import { ErrorApi } from "@/lib/api";
import { useSesion } from "@/lib/useSesion";
import { esAdmin } from "@/services/authService";
import estilos from "./admin.module.css";

type Pestana = "catalogo" | "reportes" | "metricas" | "auditoria";

const PESTANAS: { id: Pestana; nombre: string; Icono: typeof IconoPin }[] = [
  { id: "catalogo", nombre: "Catálogo", Icono: IconoPin },
  { id: "reportes", nombre: "Reportes", Icono: IconoBandera },
  { id: "metricas", nombre: "Métricas", Icono: IconoDestello },
  { id: "auditoria", nombre: "Auditoría RAG", Icono: IconoBuscar },
];

const ETIQUETA_ESTADO: Record<EstadoLugar, string> = { ACTIVO: "Activo", EN_REVISION: "En revisión", INACTIVO: "Inactivo" };

const ESTADO_REPORTE: Record<ReporteAdmin["estado"], { etiqueta: string; clase: string }> = {
  PENDIENTE: { etiqueta: "Pendiente", clase: "estado_EN_REVISION" },
  RESUELTO: { etiqueta: "Resuelto", clase: "estado_ACTIVO" },
  DESCARTADO: { etiqueta: "Descartado", clase: "estado_INACTIVO" },
};

const MOTIVO_RECHAZO: Record<MotivoRechazo | "SIN_MOTIVO", { etiqueta: string; ayuda: string }> = {
  INYECCION: { etiqueta: "Prompt injection", ayuda: "Intentó cambiar las instrucciones del asistente o ver su prompt" },
  FUERA_DE_ALCANCE: { etiqueta: "Datos internos", ayuda: "Pidió contraseñas, tokens, código o datos personales" },
  BIENESTAR: { etiqueta: "Bienestar", ayuda: "Mencionó un riesgo para sí: se le derivó a bienestar universitario" },
  FUERA_DE_REGLAMENTO: { etiqueta: "Fuera del reglamento", ayuda: "Alcohol, apuestas, contenido sexual, violencia…" },
  FUERA_DE_CONTEXTO: { etiqueta: "Respuesta descartada", ayuda: "La IA respondió algo que no sale del catálogo" },
  SIN_MOTIVO: { etiqueta: "Sin clasificar", ayuda: "Rechazos anteriores a la HU17" },
};

type FiltroAuditoria = "todas" | "rechazadas" | "inyeccion";

const FILTROS_AUDITORIA: [FiltroAuditoria, string][] = [
  ["todas", "Todas"],
  ["rechazadas", "Rechazadas"],
  ["inyeccion", "Prompt injection"],
];

const ETIQUETA_CONSULTA: Record<ConsultaRag["estado"], string> = {
  OK: "OK",
  SIN_RESULTADOS: "Sin resultados",
  RECHAZADO_SEGURIDAD: "Rechazada",
  ERROR: "Error",
  INCOMPLETO: "Incompleta",
};

// La pestaña vive en la URL (?pestana=…). Se lee con useSearchParams y no con window.location: al llegar desde
// Inicio con un Link, la página se dibuja antes de que cambie la URL del navegador y window.location todavía
// tenía la de Inicio, así que siempre abría Catálogo.
const pestanaDe = (pedida: string | null): Pestana =>
  PESTANAS.some((p) => p.id === pedida) ? (pedida as Pestana) : "catalogo";

const segundos = (ms: number | null) => (ms === null ? "—" : `${(ms / 1000).toLocaleString("es-BO", { maximumFractionDigits: 1 })} s`);

/** HU19: P95 de las recomendaciones frente al objetivo de 3,5 s, % a tiempo y cuántas veces respondió el plan B. */
function TarjetaRendimiento({ rendimiento: r }: { rendimiento: NonNullable<MetricasAdmin["rendimiento"]> }) {
  const aTiempo = r.p95Ms !== null && r.p95Ms < r.objetivoMs;
  return (
    <div className={estilos.tarjetaMetrica}>
      <h2>Recomendaciones a tiempo</h2>
      {r.p95Ms === null ? (
        <p className={estilos.ayuda}>Todavía no hay recomendaciones esta semana.</p>
      ) : (
        <>
          <p className={estilos.grande}>
            {segundos(r.p95Ms)} <small>en el 95 % de los casos</small>
          </p>
          <p className={aTiempo ? estilos.objetivoOk : estilos.objetivoFuera}>
            {aTiempo ? "Dentro" : "Fuera"} del objetivo de {segundos(r.objetivoMs)}
          </p>
          <p className={estilos.ayuda}>
            {r.dentroDelObjetivoPct} % de {r.consultas} respondió a tiempo; la mitad, en {segundos(r.p50Ms)}.{" "}
            {r.respaldos > 0
              ? `${r.respaldos} ${r.respaldos === 1 ? "vez respondió" : "veces respondió"} el plan B (la IA tardó o falló).`
              : "La IA respondió siempre a tiempo."}
          </p>
        </>
      )}
    </div>
  );
}

const fechaCorta = (iso: string) =>
  new Date(iso).toLocaleString("es-BO", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });

export default function PanelAdmin() {
  return (
    <Suspense fallback={null}>
      <PanelAdminContenido />
    </Suspense>
  );
}

function PanelAdminContenido() {
  const router = useRouter();
  const parametros = useSearchParams();
  const { sesion, listo } = useSesion();
  const reducir = useReducedMotion();
  const aviso = useAviso();
  const pestana = pestanaDe(parametros.get("pestana"));
  // Al cambiar de pestaña se actualiza la URL: así «Atrás», recargar o compartir el enlace abren la misma sección.
  const setPestana = (id: Pestana) => router.replace(`/admin?pestana=${id}`, { scroll: false });
  const [resumen, setResumen] = useState<ResumenAdmin | null>(null);
  const [lugares, setLugares] = useState<LugarAdmin[] | null>(null);
  const [reportes, setReportes] = useState<ReporteAdmin[] | null>(null);
  const [resolviendoReporte, setResolviendoReporte] = useState<number | null>(null);
  const [metricas, setMetricas] = useState<MetricasAdmin | null>(null);
  const [consultas, setConsultas] = useState<ConsultaRag[] | null>(null);
  const [filtroAuditoria, setFiltroAuditoria] = useState<FiltroAuditoria>("todas");
  const [error, setError] = useState("");
  const [cambiando, setCambiando] = useState<number | null>(null);
  // Catálogo: formulario abierto (null = cerrado; { id: null } = lugar nuevo) y lugar a eliminar.
  const [formulario, setFormulario] = useState<{ id: number | null } | null>(null);
  const [aEliminar, setAEliminar] = useState<LugarAdmin | null>(null);

  useEffect(() => {
    if (listo && sesion && !esAdmin(sesion)) router.replace("/inicio");
  }, [listo, sesion, router]);

  const cargar = useCallback(() => {
    void Promise.allSettled([obtenerResumen(), listarLugaresAdmin(), listarReportes(), obtenerMetricas(), listarConsultasRag(50)]).then(
      ([r, l, rep, m, c]) => {
        if (r.status === "fulfilled") setResumen(r.value);
        if (l.status === "fulfilled") setLugares(l.value);
        if (rep.status === "fulfilled") setReportes(rep.value);
        if (m.status === "fulfilled") setMetricas(m.value);
        if (c.status === "fulfilled") setConsultas(c.value);
        const fallo = [r, l, rep, m, c].find((x) => x.status === "rejected") as PromiseRejectedResult | undefined;
        setError(fallo ? (fallo.reason as Error).message : "");
      },
    );
  }, []);

  useEffect(() => {
    if (esAdmin(sesion)) cargar();
  }, [sesion, cargar]);

  async function cambiar(lugar: LugarAdmin, estado: EstadoLugar) {
    setCambiando(lugar.id);
    try {
      await cambiarEstadoLugar(lugar.id, estado);
      setLugares((ls) => ls?.map((l) => (l.id === lugar.id ? { ...l, estado } : l)) ?? null);
      aviso.mostrar(`${lugar.nombre}: ${ETIQUETA_ESTADO[estado].toLowerCase()}.`);
      void obtenerResumen().then(setResumen).catch(() => undefined);
    } catch (e) {
      aviso.mostrar((e as Error).message);
    } finally {
      setCambiando(null);
    }
  }

  // Después de crear, editar o eliminar: el catálogo y las cifras se vuelven a pedir.
  function catalogoCambio(mensaje: string) {
    setFormulario(null);
    setAEliminar(null);
    aviso.mostrar(mensaje);
    void listarLugaresAdmin().then(setLugares).catch(() => undefined);
    void obtenerResumen().then(setResumen).catch(() => undefined);
  }

  async function validar(lugar: LugarAdmin) {
    setCambiando(lugar.id);
    try {
      await cambiarValidacionLugar(lugar.id, true);
      setLugares((ls) => ls?.map((l) => (l.id === lugar.id ? { ...l, validado: true } : l)) ?? null);
      aviso.mostrar(`${lugar.nombre}: validado.`);
    } catch (e) {
      aviso.mostrar((e as Error).message);
    } finally {
      setCambiando(null);
    }
  }

  async function revisar(reporte: ReporteAdmin, decision: "RESUELTO" | "DESCARTADO") {
    setResolviendoReporte(reporte.id);
    try {
      await resolverReporte(reporte.id, decision);
      setReportes((rs) => rs?.map((r) => (r.id === reporte.id ? { ...r, estado: decision } : r)) ?? null);
      aviso.mostrar(`Reporte de ${reporte.objeto}: ${ESTADO_REPORTE[decision].etiqueta.toLowerCase()}.`);
      void obtenerResumen().then(setResumen).catch(() => undefined);
    } catch (e) {
      aviso.mostrar((e as Error).message);
      if (e instanceof ErrorApi && e.codigo === "YA_REVISADO") {
        void listarReportes().then(setReportes).catch(() => undefined);
        void obtenerResumen().then(setResumen).catch(() => undefined);
      }
    } finally {
      setResolviendoReporte(null);
    }
  }

  const consultasFiltradas = (consultas ?? []).filter((c) =>
    filtroAuditoria === "todas"
      ? true
      : filtroAuditoria === "rechazadas"
        ? c.estado === "RECHAZADO_SEGURIDAD"
        : c.motivoRechazo === "INYECCION",
  );

  if (!esAdmin(sesion)) return <main className={estilos.pagina} aria-busy="true" />;

  const cifras = [
    { valor: resumen?.lugaresActivos, etiqueta: "Lugares activos", extra: resumen?.lugaresEnRevision ? `${resumen.lugaresEnRevision} en revisión` : undefined, clase: estilos.cifraLugares, Icono: IconoPin },
    { valor: resumen?.estudiantes, etiqueta: "Estudiantes", clase: estilos.cifraEstudiantes, Icono: IconoPersonas },
    { valor: resumen?.comunidadesHoy, etiqueta: "Comunidades hoy", clase: estilos.cifraComunidades, Icono: IconoPersonas },
    { valor: resumen?.consultasRagSemana, etiqueta: "Consultas RAG (7 días)", clase: estilos.cifraRag, Icono: IconoBuscar },
    { valor: resumen?.reportesPendientes, etiqueta: "Reportes pendientes", clase: estilos.cifraReportes, Icono: IconoBandera },
  ];

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header" className={estilos.cabecera}>
        <div>
          <h1 className={estilos.titulo}>Panel Admin</h1>
          <p className={estilos.subtitulo}>BREAKU · Administración del campus</p>
        </div>
        <motion.button type="button" className={estilos.actualizar} onClick={cargar} whileTap={{ scale: 0.96 }} transition={RESORTE_TOQUE}>
          Actualizar
        </motion.button>
      </Pieza>

      {error && (
        <Pieza>
          <p className={estilos.error} role="alert">
            {error}
          </p>
        </Pieza>
      )}

      <Pieza className={estilos.cifras}>
        {cifras.map(({ valor, etiqueta, extra, clase, Icono }) => (
          <motion.div key={etiqueta} className={`${estilos.cifra} ${clase}`} whileHover={reducir ? undefined : { y: -3 }}>
            <span className={estilos.cifraIcono} aria-hidden="true">
              <Icono tamano={18} />
            </span>
            <span className={estilos.cifraValor}>{valor === undefined ? "—" : <Odometro valor={valor} />}</span>
            <span className={estilos.cifraEtiqueta}>{etiqueta}</span>
            {extra && <span className={estilos.cifraExtra}>{extra}</span>}
          </motion.div>
        ))}
      </Pieza>

      <Pieza>
        <div className={estilos.pestanas} role="tablist" aria-label="Secciones del panel">
          {PESTANAS.map(({ id, nombre, Icono }) => (
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
                  layoutId="pestana-admin"
                  className={estilos.pastilla}
                  transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
                />
              )}
              <span className={estilos.textoPestana}>
                <Icono tamano={16} /> {nombre}
              </span>
            </motion.button>
          ))}
        </div>
      </Pieza>

      <Pieza>
        <AnimatePresence mode="wait" initial={false}>
          <motion.section
            key={pestana}
            role="tabpanel"
            initial={reducir ? false : { opacity: 0, y: 12, filter: "blur(4px)" }}
            animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
            exit={reducir ? undefined : { opacity: 0, y: -8, filter: "blur(4px)" }}
            transition={{ duration: 0.25, ease: SALIDA }}
          >
            {pestana === "catalogo" && (
              <>
                <div className={estilos.cabeceraCatalogo}>
                  <p className={estilos.conteo}>{lugares ? `${lugares.length} lugares en el catálogo` : "Cargando catálogo…"}</p>
                  <motion.button type="button" className={estilos.nuevoLugar} onClick={() => setFormulario({ id: null })} whileTap={{ scale: 0.97 }}>
                    <IconoMas tamano={18} /> Nuevo lugar
                  </motion.button>
                </div>
                <div className={estilos.tabla} role="table" aria-label="Catálogo de lugares">
                  <div className={`${estilos.fila} ${estilos.encabezado}`} role="row">
                    <span role="columnheader">Nombre</span>
                    <span role="columnheader">Categoría</span>
                    <span role="columnheader">Estado</span>
                    <span role="columnheader">Duración</span>
                    <span role="columnheader">Recomendado</span>
                    <span role="columnheader">Acciones</span>
                  </div>
                  {lugares?.map((l, i) => (
                    <motion.div
                      key={l.id}
                      className={estilos.fila}
                      role="row"
                      initial={reducir ? false : { opacity: 0, y: 8 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ duration: 0.3, ease: SALIDA, delay: Math.min(i, 10) * 0.03 }}
                    >
                      <strong role="cell" className={estilos.nombreLugar}>
                        {l.nombre}
                        {!l.validado && <span className={estilos.dato}>Por validar</span>}
                        {!l.recomendable && <span className={estilos.dato}>Solo referencia</span>}
                      </strong>
                      <span role="cell" data-etiqueta="Categoría">
                        {l.categoria}
                      </span>
                      <span role="cell">
                        <motion.span key={l.estado} className={`${estilos.estado} ${estilos[`estado_${l.estado}`]}`} initial={reducir ? false : { scale: 0.85 }} animate={{ scale: 1 }}>
                          {ETIQUETA_ESTADO[l.estado]}
                        </motion.span>
                      </span>
                      <span role="cell" data-etiqueta="Duración">
                        {l.duracionEstimadaMin} min
                      </span>
                      <span role="cell" data-etiqueta="Recomendado">
                        {l.vecesRecomendado} {l.vecesRecomendado === 1 ? "vez" : "veces"}
                      </span>
                      <span role="cell" className={estilos.accionesFila}>
                        {!l.validado && (
                          <button type="button" className={estilos.validar} disabled={cambiando === l.id} onClick={() => void validar(l)}>
                            Validar
                          </button>
                        )}
                        {l.estado === "EN_REVISION" && (
                          <>
                            <button type="button" className={estilos.aprobar} disabled={cambiando === l.id} onClick={() => void cambiar(l, "ACTIVO")}>
                              Aprobar
                            </button>
                            <button type="button" className={estilos.desactivar} disabled={cambiando === l.id} onClick={() => void cambiar(l, "INACTIVO")}>
                              Rechazar
                            </button>
                          </>
                        )}
                        {l.estado === "ACTIVO" && (
                          <button type="button" className={estilos.desactivar} disabled={cambiando === l.id} onClick={() => void cambiar(l, "INACTIVO")}>
                            Desactivar
                          </button>
                        )}
                        {l.estado === "INACTIVO" && (
                          <button type="button" className={estilos.aprobar} disabled={cambiando === l.id} onClick={() => void cambiar(l, "ACTIVO")}>
                            Activar
                          </button>
                        )}
                        <button type="button" className={estilos.editar} onClick={() => setFormulario({ id: l.id })} aria-label={`Editar ${l.nombre}`}>
                          Editar
                        </button>
                        <button type="button" className={estilos.eliminar} onClick={() => setAEliminar(l)} aria-label={`Eliminar ${l.nombre}`}>
                          Eliminar
                        </button>
                      </span>
                    </motion.div>
                  ))}
                </div>
              </>
            )}

            {pestana === "reportes" &&
              (reportes && reportes.length > 0 ? (
                <ul className={estilos.listaReportes}>
                  {reportes.map((r) => (
                    <li key={r.id} className={estilos.reporte}>
                      <span className={`${estilos.estado} ${estilos[ESTADO_REPORTE[r.estado].clase]}`}>{ESTADO_REPORTE[r.estado].etiqueta}</span>
                      <strong>{r.objeto}</strong>
                      <p>
                        {MOTIVO_REPORTE[r.motivo] ?? r.motivo}
                        {r.descripcion ? ` · ${r.descripcion}` : ""}
                      </p>
                      <small>
                        {r.autor} · {fechaCorta(r.creadoEn)}
                      </small>
                      {r.estado === "PENDIENTE" && (
                        <span className={estilos.accionesLugar}>
                          <button
                            type="button"
                            className={estilos.aprobar}
                            disabled={resolviendoReporte === r.id}
                            onClick={() => void revisar(r, "RESUELTO")}
                            aria-label={`Marcar como resuelto el reporte de ${r.objeto}`}
                          >
                            Resolver
                          </button>
                          <button
                            type="button"
                            className={estilos.desactivar}
                            disabled={resolviendoReporte === r.id}
                            onClick={() => void revisar(r, "DESCARTADO")}
                            aria-label={`Descartar el reporte de ${r.objeto}`}
                          >
                            Descartar
                          </button>
                        </span>
                      )}
                    </li>
                  ))}
                </ul>
              ) : (
                <div className={estilos.vacio}>
                  <span className={estilos.vacioIcono} aria-hidden="true">
                    <IconoBandera tamano={24} />
                  </span>
                  <strong>{reportes ? "Sin reportes por ahora" : "Cargando reportes…"}</strong>
                  <span>Cuando un estudiante reporte un lugar o una comunidad, aparecerá aquí.</span>
                </div>
              ))}

            {pestana === "metricas" && metricas && (
              <div className={estilos.metricas}>
                <div className={estilos.tarjetaMetrica}>
                  <h2>Consultas al asistente · 7 días</h2>
                  <p className={estilos.grande}>
                    <Odometro valor={metricas.consultasTotales} /> <small>consultas</small>
                  </p>
                  {(
                    [
                      ["OK", metricas.ok, estilos.barraOk],
                      ["Sin resultados", metricas.sinResultados, estilos.barraSin],
                      ["Rechazadas por seguridad", metricas.rechazadas, estilos.barraRechazo],
                      ["Errores", metricas.errores, estilos.barraError],
                    ] as const
                  ).map(([nombre, valor, clase], i) => (
                    <div key={nombre} className={estilos.barraFila}>
                      <span>{nombre}</span>
                      <span className={estilos.barraPista}>
                        <motion.span
                          className={`${estilos.barra} ${clase}`}
                          initial={reducir ? false : { scaleX: 0 }}
                          animate={{ scaleX: metricas.consultasTotales ? valor / metricas.consultasTotales : 0 }}
                          transition={{ duration: 0.8, ease: SALIDA, delay: 0.1 + i * 0.08 }}
                        />
                      </span>
                      <strong>{valor}</strong>
                    </div>
                  ))}
                </div>
                <div className={estilos.tarjetaMetrica}>
                  <h2>Latencia promedio</h2>
                  <p className={estilos.grande}>{segundos(metricas.latenciaPromedioMs)}</p>
                  <p className={estilos.ayuda}>Tiempo que tarda el asistente en responder cada consulta.</p>
                </div>
                {metricas.rendimiento && <TarjetaRendimiento rendimiento={metricas.rendimiento} />}
                <div className={estilos.tarjetaMetrica}>
                  <h2>Valoraciones de los estudiantes</h2>
                  <div className={estilos.votosResumen}>
                    <p className={estilos.grande}>
                      <span className={estilos.votoArriba} aria-hidden="true">
                        <IconoPulgarArriba tamano={22} />
                      </span>
                      <Odometro valor={metricas.meGusta} /> <small>me gusta</small>
                    </p>
                    <p className={estilos.grande}>
                      <span className={estilos.votoAbajo} aria-hidden="true">
                        <IconoPulgarAbajo tamano={22} />
                      </span>
                      <Odometro valor={metricas.noMeSirve} /> <small>no me sirve</small>
                    </p>
                  </div>
                  {metricas.meGusta + metricas.noMeSirve > 0 ? (
                    <span className={estilos.barraPista} aria-hidden="true">
                      <motion.span
                        className={`${estilos.barra} ${estilos.barraOk}`}
                        initial={reducir ? false : { scaleX: 0 }}
                        animate={{ scaleX: metricas.meGusta / (metricas.meGusta + metricas.noMeSirve) }}
                        transition={{ duration: 0.8, ease: SALIDA, delay: 0.1 }}
                      />
                    </span>
                  ) : null}
                  <p className={estilos.ayuda}>
                    {metricas.meGusta + metricas.noMeSirve > 0
                      ? `${Math.round((100 * metricas.meGusta) / (metricas.meGusta + metricas.noMeSirve))} % de las valoraciones son positivas.`
                      : "Todavía nadie valoró una recomendación esta semana."}
                  </p>
                </div>
                <div className={estilos.tarjetaMetrica}>
                  <h2>Solicitudes bloqueadas · 7 días</h2>
                  <p className={estilos.grande}>
                    <Odometro valor={metricas.rechazadas} /> <small>{metricas.rechazadas === 1 ? "bloqueada" : "bloqueadas"}</small>
                  </p>
                  {(metricas.rechazosPorMotivo ?? []).length === 0 ? (
                    <p className={estilos.ayuda}>Nadie intentó nada fuera de lugar esta semana.</p>
                  ) : (
                    (metricas.rechazosPorMotivo ?? []).map((r, i) => (
                      <div key={r.motivo} className={estilos.barraFila} title={MOTIVO_RECHAZO[r.motivo].ayuda}>
                        <span>{MOTIVO_RECHAZO[r.motivo].etiqueta}</span>
                        <span className={estilos.barraPista}>
                          <motion.span
                            className={`${estilos.barra} ${r.motivo === "INYECCION" ? estilos.barraError : estilos.barraRechazo}`}
                            initial={reducir ? false : { scaleX: 0 }}
                            animate={{ scaleX: metricas.rechazadas ? r.cantidad / metricas.rechazadas : 0 }}
                            transition={{ duration: 0.8, ease: SALIDA, delay: 0.1 + i * 0.08 }}
                          />
                        </span>
                        <strong>{r.cantidad}</strong>
                      </div>
                    ))
                  )}
                  <p className={estilos.ayuda}>Se rechazan antes de llegar a la IA: no gastan consultas.</p>
                </div>
                <div className={`${estilos.tarjetaMetrica} ${estilos.ancha}`}>
                  <h2>Lugares más recomendados</h2>
                  {metricas.masRecomendados.length === 0 && <p className={estilos.ayuda}>Todavía no hay recomendaciones esta semana.</p>}
                  {metricas.masRecomendados.map((l, i) => (
                    <div key={l.nombre} className={estilos.barraFila}>
                      <span>
                        {l.nombre} <small>· {l.categoria}</small>
                      </span>
                      <span className={estilos.barraPista}>
                        <motion.span
                          className={`${estilos.barra} ${estilos.barraTop}`}
                          initial={reducir ? false : { scaleX: 0 }}
                          animate={{ scaleX: l.veces / metricas.masRecomendados[0].veces }}
                          transition={{ duration: 0.8, ease: SALIDA, delay: 0.1 + i * 0.08 }}
                        />
                      </span>
                      <strong>{l.veces}</strong>
                      {l.meGusta + l.noMeSirve > 0 && (
                        <span
                          className={estilos.votosLugar}
                          aria-label={`${l.meGusta} me gusta y ${l.noMeSirve} no me sirve`}
                        >
                          <IconoPulgarArriba tamano={13} /> {l.meGusta}
                          <IconoPulgarAbajo tamano={13} /> {l.noMeSirve}
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {pestana === "auditoria" && (
              <>
                <div className={estilos.filtrosAuditoria} role="group" aria-label="Filtrar consultas">
                  {FILTROS_AUDITORIA.map(([id, nombre]) => (
                    <button
                      key={id}
                      type="button"
                      className={filtroAuditoria === id ? estilos.filtroActivo : estilos.filtro}
                      aria-pressed={filtroAuditoria === id}
                      onClick={() => setFiltroAuditoria(id)}
                    >
                      {nombre}
                    </button>
                  ))}
                </div>
                <p className={estilos.conteo}>
                  {filtroAuditoria === "todas"
                    ? `Últimas ${consultas?.length ?? 0} consultas al asistente (cada conversación, en una sola)`
                    : `${consultasFiltradas.length} de las últimas ${consultas?.length ?? 0} consultas`}
                </p>
                {consultasFiltradas.length === 0 && consultas && (
                  <p className={estilos.ayuda}>
                    {filtroAuditoria === "inyeccion" ? "Ningún intento de prompt injection entre estas consultas." : "Ninguna consulta rechazada entre estas."}
                  </p>
                )}
                <ul className={estilos.listaConsultas}>
                  {consultasFiltradas.map((c) => (
                    <li key={c.id} className={estilos.consulta}>
                      <div className={estilos.consultaCabecera}>
                        <span className={`${estilos.estado} ${estilos[`consulta_${c.estado}`]}`}>{ETIQUETA_CONSULTA[c.estado]}</span>
                        {c.estado === "RECHAZADO_SEGURIDAD" && (
                          <span
                            className={c.motivoRechazo === "INYECCION" ? estilos.motivoInyeccion : estilos.motivo}
                            title={MOTIVO_RECHAZO[c.motivoRechazo ?? "SIN_MOTIVO"].ayuda}
                          >
                            {MOTIVO_RECHAZO[c.motivoRechazo ?? "SIN_MOTIVO"].etiqueta}
                          </span>
                        )}
                        <small>
                          #{c.id} · {fechaCorta(c.fecha)} · {c.usuario}
                        </small>
                      </div>
                      <p className={estilos.entrada}>“{c.entrada ?? "(sin preferencia)"}”</p>
                      {(c.respuestas?.length ?? 0) > 0 && (
                        <ol className={estilos.respuestas} aria-label="Lo que respondió después">
                          {c.respuestas!.map((texto, i) => (
                            <li key={i}>
                              <span className={estilos.respondio}>Respondió</span> “{texto}”
                            </li>
                          ))}
                        </ol>
                      )}
                      <div className={estilos.consultaDatos}>
                        <span>{c.modelo ?? "Sin LLM"}</span>
                        <span>{segundos(c.latenciaMs)}</span>
                        {(c.turnos ?? 1) > 1 && <span>{c.turnos} turnos</span>}
                        {c.tipo === "RECOMENDACION" && (
                          <span>
                            {c.lugaresRecuperados} recuperados → {c.recomendados} recomendados
                          </span>
                        )}
                      </div>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </motion.section>
        </AnimatePresence>
      </Pieza>

      <AnimatePresence>
        {formulario && (
          <FormularioLugar key="formulario" lugarId={formulario.id} onCerrar={() => setFormulario(null)} onGuardado={catalogoCambio} />
        )}
        {aEliminar && (
          <EliminarLugar
            key="eliminar"
            lugar={aEliminar}
            onCerrar={() => setAEliminar(null)}
            onEliminado={catalogoCambio}
            onDesactivar={(l) => {
              setAEliminar(null);
              void cambiar(l, "INACTIVO");
            }}
          />
        )}
      </AnimatePresence>

      <AvisoFlotante texto={aviso.texto} />
    </Entrada>
  );
}

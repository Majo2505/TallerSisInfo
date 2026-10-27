"use client";

import { useRouter } from "next/navigation";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useCallback, useEffect, useState } from "react";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import { IconoBandera, IconoBuscar, IconoDestello, IconoPersonas, IconoPin } from "@/components/app/Iconos";
import { Entrada, Odometro, Pieza, RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import {
  ConsultaRag,
  EstadoLugar,
  LugarAdmin,
  MetricasAdmin,
  ReporteAdmin,
  ResumenAdmin,
  cambiarEstadoLugar,
  listarConsultasRag,
  listarLugaresAdmin,
  listarReportes,
  obtenerMetricas,
  obtenerResumen,
} from "@/lib/admin";
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

const ETIQUETA_CONSULTA: Record<ConsultaRag["estado"], string> = {
  OK: "OK",
  SIN_RESULTADOS: "Sin resultados",
  RECHAZADO_SEGURIDAD: "Rechazada",
  ERROR: "Error",
};

const segundos = (ms: number | null) => (ms === null ? "—" : `${(ms / 1000).toLocaleString("es-BO", { maximumFractionDigits: 1 })} s`);

const fechaCorta = (iso: string) =>
  new Date(iso).toLocaleString("es-BO", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });

/** Panel de administración del campus (rol ADMIN): cifras, catálogo, reportes, métricas y auditoría del asistente. */
export default function PanelAdmin() {
  const router = useRouter();
  const { sesion, listo } = useSesion();
  const reducir = useReducedMotion();
  const aviso = useAviso();
  const [pestana, setPestana] = useState<Pestana>("catalogo");
  const [resumen, setResumen] = useState<ResumenAdmin | null>(null);
  const [lugares, setLugares] = useState<LugarAdmin[] | null>(null);
  const [reportes, setReportes] = useState<ReporteAdmin[] | null>(null);
  const [metricas, setMetricas] = useState<MetricasAdmin | null>(null);
  const [consultas, setConsultas] = useState<ConsultaRag[] | null>(null);
  const [error, setError] = useState("");
  const [cambiando, setCambiando] = useState<number | null>(null);

  // Solo administradores: el resto vuelve a su inicio (el backend igual responde 403).
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
                <p className={estilos.conteo}>{lugares ? `${lugares.length} lugares en el catálogo` : "Cargando catálogo…"}</p>
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
                      <span className={`${estilos.estado} ${r.estado === "PENDIENTE" ? estilos.estado_EN_REVISION : estilos.estado_ACTIVO}`}>{r.estado}</span>
                      <strong>{r.objeto}</strong>
                      <p>
                        {r.motivo}
                        {r.descripcion ? ` · ${r.descripcion}` : ""}
                      </p>
                      <small>
                        {r.autor} · {fechaCorta(r.creadoEn)}
                      </small>
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
                    </div>
                  ))}
                </div>
              </div>
            )}

            {pestana === "auditoria" && (
              <>
                <p className={estilos.conteo}>Últimas {consultas?.length ?? 0} consultas al asistente</p>
                <ul className={estilos.listaConsultas}>
                  {consultas?.map((c) => (
                    <li key={c.id} className={estilos.consulta}>
                      <div className={estilos.consultaCabecera}>
                        <span className={`${estilos.estado} ${estilos[`consulta_${c.estado}`]}`}>{ETIQUETA_CONSULTA[c.estado]}</span>
                        <small>
                          #{c.id} · {fechaCorta(c.fecha)} · {c.usuario}
                        </small>
                      </div>
                      <p className={estilos.entrada}>“{c.entrada ?? "(sin preferencia)"}”</p>
                      <div className={estilos.consultaDatos}>
                        <span>{c.modelo ?? "Sin LLM"}</span>
                        <span>{segundos(c.latenciaMs)}</span>
                        <span>
                          {c.lugaresRecuperados} recuperados → {c.recomendados} recomendados
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </motion.section>
        </AnimatePresence>
      </Pieza>

      <AvisoFlotante texto={aviso.texto} />
    </Entrada>
  );
}

"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { IconoPersonas } from "@/components/app/Iconos";
import { UsuarioDetalle, UsuarioLista, levantarSancion, listarUsuarios, obtenerUsuario } from "@/lib/usuarios";
import {
  ETIQUETA_ESTADO,
  ETIQUETA_TIPO,
  EstadoUsuario,
  TipoSancion,
  aceptaSanciones,
  sePuedeLevantar,
  sugiereSuspension,
} from "@/lib/reglasUsuarios";
import FormularioSancion from "./FormularioSancion";
import estilos from "./Usuarios.module.css";

const fecha = (iso: string) =>
  new Date(iso).toLocaleString("es-BO", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });

const plural = (n: number, uno: string, varios: string) => `${n} ${n === 1 ? uno : varios}`;

function Estado({ estado }: { estado: EstadoUsuario }) {
  return <span className={`${estilos.estado} ${estilos[`estado_${estado}`]}`}>{ETIQUETA_ESTADO[estado]}</span>;
}

type Props = {
  /** Avisos breves del panel (el mismo toast de las demás pestañas). */
  onAviso: (texto: string) => void;
  /** Se pide el detalle de este usuario al abrir (por ejemplo, desde un enlace). */
  usuarioInicial?: number | null;
};

/** HU28: lista de estudiantes con búsqueda y, al abrir uno, su detalle, su historial y las acciones de sanción. */
export default function Usuarios({ onAviso, usuarioInicial = null }: Props) {
  const [abierto, setAbierto] = useState<number | null>(usuarioInicial);
  const [buscar, setBuscar] = useState("");
  const [lista, setLista] = useState<UsuarioLista[] | null>(null);
  const [errorLista, setErrorLista] = useState("");
  const [detalle, setDetalle] = useState<UsuarioDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState("");
  const [formulario, setFormulario] = useState<TipoSancion | null>(null);
  const [levantando, setLevantando] = useState<number | null>(null);
  const pedido = useRef(0);

  const cargarLista = useCallback((texto: string) => {
    const mio = ++pedido.current;
    void listarUsuarios(texto)
      .then((datos) => {
        if (mio !== pedido.current) return;
        setLista(datos);
        setErrorLista("");
      })
      .catch((e: Error) => mio === pedido.current && setErrorLista(e.message));
  }, []);

  // La búsqueda espera a que termines de escribir; sin texto trae a todos.
  useEffect(() => {
    const t = setTimeout(() => cargarLista(buscar), buscar ? 300 : 0);
    return () => clearTimeout(t);
  }, [buscar, cargarLista]);

  const cargarDetalle = useCallback((id: number) => {
    void obtenerUsuario(id)
      .then((d) => {
        setDetalle(d);
        setErrorDetalle("");
      })
      .catch((e: Error) => setErrorDetalle(e.message));
  }, []);

  useEffect(() => {
    if (abierto !== null) cargarDetalle(abierto);
  }, [abierto, cargarDetalle]);

  function abrir(id: number) {
    setDetalle(null);
    setErrorDetalle("");
    setAbierto(id);
  }

  function volver() {
    setAbierto(null);
    setDetalle(null);
    cargarLista(buscar);
  }

  async function levantar(sancionId: number) {
    if (!detalle) return;
    setLevantando(sancionId);
    try {
      await levantarSancion(detalle.id, sancionId);
      onAviso("Sanción levantada.");
    } catch (e) {
      onAviso((e as Error).message);
    } finally {
      setLevantando(null);
      cargarDetalle(detalle.id);
    }
  }

  function sancionAplicada(mensaje: string) {
    setFormulario(null);
    onAviso(mensaje);
    if (abierto !== null) cargarDetalle(abierto);
  }

  // ---------------------------------------------------------------- detalle
  if (abierto !== null) {
    return (
      <div className={estilos.contenedor}>
        <button type="button" className={`hz-btn ${estilos.volver}`} onClick={volver}>
          ← Volver a la lista
        </button>

        {errorDetalle && (
          <p className={estilos.error} role="alert">
            {errorDetalle}
          </p>
        )}
        {!detalle && !errorDetalle && <div className="hz-skeleton" style={{ height: 220 }} aria-busy="true" aria-label="Cargando usuario" />}

        {detalle && (
          <>
            <div className={estilos.cabecera}>
              <div>
                <h2>{detalle.nombre}</h2>
                <p>
                  {detalle.correo}
                  {detalle.carrera ? ` · ${detalle.carrera}` : ""}
                  {detalle.semestre ? ` · ${detalle.semestre}.º semestre` : ""}
                </p>
              </div>
              <Estado estado={detalle.estado} />
            </div>

            {detalle.bloqueo && (
              <p className={estilos.bloqueo} role="status">
                {detalle.bloqueo.tipo === "SUSPENSION"
                  ? `Suspendido${detalle.bloqueo.hasta ? ` hasta el ${fecha(detalle.bloqueo.hasta)}` : ""}. `
                  : "Cuenta dada de baja. "}
                Motivo: {detalle.bloqueo.motivo}
              </p>
            )}

            <section className={estilos.tarjeta} aria-labelledby="titulo-advertencias">
              <h3 id="titulo-advertencias">Advertencias vigentes</h3>
              <p className={estilos.grande}>
                {detalle.advertencias} <small>de {detalle.advertenciasParaSuspension} · las levantadas no cuentan</small>
              </p>
              {sugiereSuspension(detalle.advertencias) && aceptaSanciones(detalle.estado) && (
                <p className={estilos.sugerencia} role="status">
                  Tiene {detalle.advertencias} advertencias vigentes: el sistema sugiere una SUSPENSIÓN (por defecto,{" "}
                  {detalle.diasSuspensionPorDefecto} días). Es solo una sugerencia: la decisión es tuya.
                </p>
              )}
            </section>

            {aceptaSanciones(detalle.estado) ? (
              <div className={estilos.acciones}>
                <button type="button" className="hz-btn" onClick={() => setFormulario("ADVERTENCIA")}>
                  Advertir
                </button>
                <button type="button" className="hz-btn" onClick={() => setFormulario("SUSPENSION")}>
                  Suspender
                </button>
                <button type="button" className="hz-btn hz-btn-peligro" onClick={() => setFormulario("BAJA")}>
                  Dar de baja
                </button>
              </div>
            ) : (
              <p className={estilos.conteo}>Esta cuenta ya no recibe sanciones.</p>
            )}

            <section aria-labelledby="titulo-historial" className={estilos.contenedor}>
              <h3 id="titulo-historial" className={estilos.conteo}>
                Historial de sanciones ({detalle.sanciones.length})
              </h3>
              {detalle.sanciones.length === 0 ? (
                <div className={estilos.vacio}>
                  <strong>Sin sanciones</strong>
                  <span>Este estudiante no tiene sanciones registradas.</span>
                </div>
              ) : (
                <ul className={estilos.historial}>
                  {detalle.sanciones.map((s) => (
                    <li key={s.id} className={`${estilos.sancion} ${s.levantadaEn ? estilos.sancionLevantada : ""}`}>
                      <div className={estilos.sancionCabecera}>
                        <span className={`${estilos.estado} ${estilos[`tipo_${s.tipo}`]}`}>{ETIQUETA_TIPO[s.tipo]}</span>
                        {s.levantadaEn && <span className={`${estilos.estado} ${estilos.estado_ACTIVO}`}>Levantada</span>}
                        {!s.levantadaEn && s.tipo === "SUSPENSION" && !s.vigente && (
                          <span className={`${estilos.estado} ${estilos.estado_ACTIVO}`}>Terminó</span>
                        )}
                        {sePuedeLevantar(s) && (
                          <button
                            type="button"
                            className="hz-btn"
                            style={{ minHeight: 38, marginLeft: "auto" }}
                            disabled={levantando === s.id}
                            onClick={() => void levantar(s.id)}
                            aria-label={`Levantar ${ETIQUETA_TIPO[s.tipo].toLowerCase()} del ${fecha(s.creadoEn)}`}
                          >
                            {levantando === s.id ? "Levantando…" : "Levantar"}
                          </button>
                        )}
                      </div>
                      <p>{s.motivo}</p>
                      <small>
                        Aplicada el {fecha(s.creadoEn)} por {s.creadoPor}
                        {s.hasta ? ` · hasta el ${fecha(s.hasta)}` : ""}
                        {s.reporteId ? ` · reporte n.º ${s.reporteId}` : ""}
                      </small>
                      {s.levantadaEn && (
                        <small>
                          Levantada el {fecha(s.levantadaEn)}
                          {s.levantadaPor ? ` por ${s.levantadaPor}` : ""}
                        </small>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </section>

            {formulario && (
              <FormularioSancion usuario={{ id: detalle.id, nombre: detalle.nombre }} tipo={formulario} onCerrar={() => setFormulario(null)} onAplicada={sancionAplicada} />
            )}
          </>
        )}
      </div>
    );
  }

  // ------------------------------------------------------------------ lista
  return (
    <div className={estilos.contenedor}>
      <div className={estilos.barra}>
        <label className={estilos.buscador}>
          Buscar por nombre o correo
          <input type="search" value={buscar} onChange={(e) => setBuscar(e.target.value)} placeholder="Ej: ana, @ucb.edu.bo" />
        </label>
      </div>

      {errorLista && (
        <p className={estilos.error} role="alert">
          {errorLista}
        </p>
      )}

      {!lista && !errorLista && <div className="hz-skeleton" style={{ height: 220 }} aria-busy="true" aria-label="Cargando usuarios" />}

      {lista && (
        <>
          <p className={estilos.conteo} role="status">
            {plural(lista.length, "estudiante", "estudiantes")}
            {buscar.trim() ? " con esa búsqueda" : ""}
          </p>
          {lista.length === 0 ? (
            <div className={estilos.vacio}>
              <span aria-hidden="true">
                <IconoPersonas tamano={24} />
              </span>
              <strong>{buscar.trim() ? "Nadie coincide con la búsqueda" : "Todavía no hay estudiantes"}</strong>
              <span>{buscar.trim() ? "Prueba con otro nombre o correo." : "Cuando se registren, aparecerán aquí."}</span>
            </div>
          ) : (
            <ul className={estilos.lista}>
              {lista.map((u) => (
                <li key={u.id}>
                  <button type="button" className={estilos.fila} onClick={() => abrir(u.id)} aria-label={`Abrir a ${u.nombre}, ${ETIQUETA_ESTADO[u.estado]}`}>
                    <span className={estilos.filaNombre}>{u.nombre}</span>
                    <Estado estado={u.estado} />
                    <span className={estilos.filaDato}>
                      {u.correo}
                      {u.carrera ? ` · ${u.carrera}` : ""}
                    </span>
                    <span className={estilos.filaCifras}>
                      <span>{plural(u.sanciones, "sanción", "sanciones")}</span>
                      <span>{plural(u.advertencias, "advertencia vigente", "advertencias vigentes")}</span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  );
}

"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import AlertaDuplicado from "@/components/eventos/AlertaDuplicado";
import FotoEvento from "@/components/eventos/FotoEvento";
import CampoFechaHora from "@/components/eventos/CampoFechaHora";
import FondoVivo from "@/components/app/FondoVivo";
import fotoEventos from "@/components/app/fotos/eventos.webp";
import { IconoBandera, IconoCalendario, IconoCheck, IconoLapiz, IconoPin, IconoReloj } from "@/components/app/Iconos";
import { Entrada, Odometro, Pieza, seguirPuntero } from "@/components/app/Movimiento";
import { Selector } from "@/components/app/Selectores";
import { ErrorApi } from "@/lib/api";
import { debeIrAlInicio } from "@/lib/reglasAdmin";
import { useSesion } from "@/lib/useSesion";
import { esAdmin } from "@/services/authService";
import {
  bajarEvento,
  EventoCampus,
  EventoRechazado,
  listarEventosPendientes,
  listarEventosPublicadosAdmin,
  listarEventosRechazadosAdmin,
  listarLugaresEventos,
  LugarEvento,
  moderarEvento,
} from "@/lib/eventos";
import {
  cambiosEventoParaApi,
  camposDesdeEvento,
  CamposFormularioEvento,
  esMotivoDuplicado,
  validarFormularioEvento,
  validarMotivoModeracion,
} from "@/lib/reglasEventos";
import { formatearFechaHora } from "@/lib/reglasFlyer";
import { useRefrescoAutomatico } from "@/lib/useRefrescoAutomatico";
import estilos from "./eventos.module.css";

type Pestana = "pendientes" | "publicados" | "rechazados";

const NOMBRE_PESTANA: Record<Pestana, string> = {
  pendientes: "En revisión",
  publicados: "Publicados",
  rechazados: "Descartados",
};
type Campos = CamposFormularioEvento & { descripcion: string };
const OTRO_LUGAR = "__otro__";

const textoError = (e: unknown, porDefecto: string) =>
  e instanceof ErrorApi ? e.message : e instanceof Error ? e.message : porDefecto;

export default function EventosPendientesPage() {
  const router = useRouter();
  // HU37: como /admin, esta pantalla es solo para administradores.
  const { sesion, listo } = useSesion();
  const fueraDeLugar = debeIrAlInicio(listo, Boolean(sesion), Boolean(sesion && esAdmin(sesion)));
  useEffect(() => {
    if (fueraDeLugar) router.replace("/inicio");
  }, [fueraDeLugar, router]);
  const [pestana, setPestana] = useState<Pestana>("pendientes");
  const [eventos, setEventos] = useState<EventoCampus[]>([]);
  const [publicados, setPublicados] = useState<EventoCampus[]>([]);
  // HU26: historial de rechazados y bajados (solo lectura).
  const [rechazados, setRechazados] = useState<EventoRechazado[]>([]);
  const [lugares, setLugares] = useState<LugarEvento[]>([]);
  const [motivos, setMotivos] = useState<Record<number, string>>({});
  // HU26: el evento que el admin está editando antes de aprobarlo, con sus campos.
  const [editando, setEditando] = useState<{ id: number; campos: Campos } | null>(null);
  const [cargando, setCargando] = useState(true);
  const [procesando, setProcesando] = useState<number | null>(null);
  const [error, setError] = useState("");
  const [mensaje, setMensaje] = useState("");

  // Sin botón «Actualizar»: un evento nuevo en revisión (aviso en vivo) o volver a la pestaña trae las listas al día, sin parpadeo.
  const refrescar = useCallback(() => {
    void Promise.allSettled([listarEventosPendientes(), listarEventosPublicadosAdmin(), listarEventosRechazadosAdmin()]).then(
      ([p, pub, rech]) => {
        if (p.status === "fulfilled") setEventos(p.value);
        if (pub.status === "fulfilled") setPublicados(pub.value);
        if (rech.status === "fulfilled") setRechazados(rech.value);
        if (p.status === "fulfilled" && pub.status === "fulfilled") setError("");
      },
    );
  }, []);
  useRefrescoAutomatico(refrescar);

  useEffect(() => {
    let activo = true;
    void Promise.allSettled([
      listarEventosPendientes(),
      listarEventosPublicadosAdmin(),
      listarLugaresEventos(),
      listarEventosRechazadosAdmin(),
    ]).then(
      ([p, pub, l, rech]) => {
        if (!activo) return;
        if (p.status === "fulfilled") setEventos(p.value);
        if (pub.status === "fulfilled") setPublicados(pub.value);
        if (rech.status === "fulfilled") setRechazados(rech.value);
        if (l.status === "fulfilled") setLugares(l.value);
        const fallo = [p, pub].find((r) => r.status === "rejected");
        if (fallo) setError(textoError((fallo as PromiseRejectedResult).reason, "No se pudieron cargar los eventos."));
        setCargando(false);
      },
    );
    return () => {
      activo = false;
    };
  }, []);

  function olvidarMotivo(id: number) {
    setMotivos((actuales) => {
      const nuevos = { ...actuales };
      delete nuevos[id];
      return nuevos;
    });
  }

  async function ejecutar(evento: EventoCampus, accion: () => Promise<void>, alTerminar: () => void, exito: string) {
    setProcesando(evento.id);
    setError("");
    setMensaje("");
    try {
      await accion();
      alTerminar();
      olvidarMotivo(evento.id);
      setMensaje(exito);
    } catch (e) {
      setError(textoError(e, "No se pudo revisar el evento."));
    } finally {
      setProcesando(null);
    }
  }

  function resolver(evento: EventoCampus, estado: "PUBLICADO" | "RECHAZADO") {
    const motivo = motivos[evento.id] ?? "";
    if (estado === "RECHAZADO") {
      const falta = validarMotivoModeracion(motivo, "rechazar");
      if (falta) return setError(`«${evento.titulo}»: ${falta}`);
    }
    void ejecutar(
      evento,
      () => moderarEvento(evento.id, { estado, motivo: estado === "RECHAZADO" ? motivo.trim() : null }),
      () => {
        setEventos((actuales) => actuales.filter((e) => e.id !== evento.id));
        refrescar();
      },
      estado === "PUBLICADO" ? "Evento aprobado y estudiante notificado." : "Evento rechazado y estudiante notificado.",
    );
  }

  function aprobarConCambios(evento: EventoCampus, campos: Campos) {
    const invalido = validarFormularioEvento(campos);
    if (invalido) return setError(invalido);
    const nombreLugar = lugares.find((l) => String(l.id) === campos.lugarId)?.nombre;
    void ejecutar(
      evento,
      () => moderarEvento(evento.id, { estado: "PUBLICADO", cambios: cambiosEventoParaApi(campos, nombreLugar) }),
      () => {
        setEventos((actuales) => actuales.filter((e) => e.id !== evento.id));
        setEditando(null);
        refrescar();
      },
      "Evento corregido, aprobado y estudiante notificado.",
    );
  }

  function bajar(evento: EventoCampus) {
    const motivo = motivos[evento.id] ?? "";
    const falta = validarMotivoModeracion(motivo, "bajar");
    if (falta) return setError(`«${evento.titulo}»: ${falta}`);
    void ejecutar(
      evento,
      () => bajarEvento(evento.id, motivo.trim()),
      () => {
        setPublicados((actuales) => actuales.filter((e) => e.id !== evento.id));
        refrescar();
      },
      "Evento bajado: ya no se muestra en Eventos y su autor recibió el aviso con el motivo.",
    );
  }

  const lista = pestana === "pendientes" ? eventos : pestana === "publicados" ? publicados : [];
  const cantidad: Record<Pestana, number> = { pendientes: eventos.length, publicados: publicados.length, rechazados: rechazados.length };
  const vacia = pestana === "rechazados" ? rechazados.length === 0 : lista.length === 0;

  if (fueraDeLugar) return null;

  return (
    <Entrada as="main" className={estilos.pagina}>
      {/* Misma portada que Eventos del estudiante: foto, luz que sigue al cursor y pastilla con ícono. */}
      <Pieza as="header">
        <div className={estilos.portada} onPointerMove={seguirPuntero}>
          <FondoVivo foto={fotoEventos} enfoque="72% 45%" />
          <span className={estilos.etiqueta}>
            <IconoCalendario tamano={14} /> Moderación
          </span>
          <h1>Moderación de eventos</h1>
          <p>
            Aprueba, corrige o rechaza los eventos en revisión, y baja los publicados que no corresponden. Quien lo creó recibe
            un aviso con el resultado.
          </p>
          {!cargando && (
            <div className={estilos.cifrasPortada} aria-live="polite">
              <span>
                <b><Odometro valor={eventos.length} /></b> en revisión
              </span>
              <span>
                <b><Odometro valor={publicados.length} /></b> {publicados.length === 1 ? "publicado vigente" : "publicados vigentes"}
              </span>
            </div>
          )}
        </div>
      </Pieza>

      <div className={estilos.pestanas} role="tablist" aria-label="Eventos por estado">
        {(["pendientes", "publicados", "rechazados"] as const).map((id) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={pestana === id}
            className={pestana === id ? estilos.pestanaActiva : estilos.pestana}
            onClick={() => {
              setPestana(id);
              setError("");
              setMensaje("");
            }}
          >
            {`${NOMBRE_PESTANA[id]} (${cantidad[id]})`}
          </button>
        ))}
      </div>

      {error && <p className={`hz-banner ${estilos.banner}`} role="alert">{error}</p>}
      {mensaje && <p className={estilos.mensaje} role="status"><IconoCheck tamano={16} /> {mensaje}</p>}

      <Pieza as="section" className={estilos.lista}>
        <h2 className={estilos.tituloLista}>
          {pestana === "pendientes"
            ? "En revisión"
            : pestana === "publicados"
              ? "Publicados que todavía no terminaron"
              : "Historial de rechazados y bajados"}
        </h2>

        {cargando ? (
          <div className={estilos.cargando} role="status" aria-label="Cargando eventos">
            <span className="hz-skeleton" />
            <span className="hz-skeleton" />
          </div>
        ) : vacia ? (
          <div className={estilos.vacio}>
            <span className={estilos.vacioIcono} aria-hidden="true"><IconoCheck tamano={26} /></span>
            <strong>Todo al día</strong>
            <span>
              {pestana === "pendientes"
                ? "No hay eventos pendientes de revisión. Cuando alguien publique uno que necesite revisión, aparece aquí solo."
                : pestana === "publicados"
                  ? "No hay eventos publicados por delante."
                  : "Todavía no se rechazó ni se bajó ningún evento."}
            </span>
          </div>
        ) : pestana === "rechazados" ? (
          rechazados.map((r) => <TarjetaRechazado key={r.evento.id} rechazado={r} />)
        ) : lista.map((evento) => (
          <article key={evento.id} className={estilos.evento}>
            {evento.fotoUrl && <FotoEvento ruta={evento.fotoUrl} titulo={evento.titulo} />}
            <div className={estilos.detalle}>
              <span className={evento.estado === "PUBLICADO" ? estilos.estadoPublicado : estilos.estado}>
                {evento.estado === "PUBLICADO" ? "Publicado" : "En revisión"}
              </span>
              <h3>{evento.titulo}</h3>
              <ul className={estilos.datos}>
                <li><IconoReloj tamano={15} /> {formatearFechaHora(evento.inicio)} – {formatearFechaHora(evento.fin)}</li>
                <li><IconoPin tamano={15} /> {evento.lugarId === null ? `Fuera del campus · ${evento.lugar}` : evento.lugar}</li>
                <li><IconoCalendario tamano={15} /> {evento.asistencia === "FIJA" ? "Hay que llegar a la hora" : `Asistencia libre, ${evento.visitaMin}–${evento.visitaMax} min`}</li>
              </ul>
              {evento.descripcion && <p className={estilos.descripcion}>{evento.descripcion}</p>}
              {evento.estado === "PENDIENTE" && evento.motivo && (
                esMotivoDuplicado(evento.motivo)
                  ? <AlertaDuplicado compacta mensaje={evento.motivo} />
                  : <p className={estilos.motivo}><strong>Por qué está en revisión:</strong> {evento.motivo}</p>
              )}

              {evento.estado === "PENDIENTE" && editando?.id === evento.id && (
                <FormularioEdicion
                  campos={editando.campos}
                  lugares={lugares}
                  ocupado={procesando !== null}
                  guardando={procesando === evento.id}
                  onCambio={(campos) => setEditando({ id: evento.id, campos })}
                  onCancelar={() => setEditando(null)}
                  onAprobar={() => aprobarConCambios(evento, editando.campos)}
                />
              )}

              {evento.estado === "PENDIENTE" && editando?.id !== evento.id && (
                <>
                  <label className={`hz-campo ${estilos.campo}`}>
                    <span>Motivo si lo rechazas <em>(obligatorio para rechazar)</em></span>
                    <textarea maxLength={500} rows={3} value={motivos[evento.id] ?? ""} onChange={(e) => setMotivos((actuales) => ({ ...actuales, [evento.id]: e.target.value }))} />
                  </label>
                  <div className={estilos.acciones}>
                    <button type="button" className="hz-btn hz-btn-peligro" disabled={procesando !== null} onClick={() => resolver(evento, "RECHAZADO")}>
                      {procesando === evento.id ? "Guardando…" : "Rechazar"}
                    </button>
                    <button type="button" className="hz-btn" disabled={procesando !== null} onClick={() => { setError(""); setEditando({ id: evento.id, campos: camposDesdeEvento(evento) }); }}>
                      <IconoLapiz tamano={15} /> Editar y aprobar
                    </button>
                    <button type="button" className="hz-btn hz-btn-primary" disabled={procesando !== null} onClick={() => resolver(evento, "PUBLICADO")}>
                      {procesando === evento.id ? "Guardando…" : "Aprobar y publicar"}
                    </button>
                  </div>
                </>
              )}

              {evento.estado === "PUBLICADO" && (
                <>
                  <label className={`hz-campo ${estilos.campo}`}>
                    <span>Motivo para bajarlo <em>(obligatorio; se lo mandamos a quien lo creó)</em></span>
                    <textarea maxLength={500} rows={3} value={motivos[evento.id] ?? ""} onChange={(e) => setMotivos((actuales) => ({ ...actuales, [evento.id]: e.target.value }))} />
                  </label>
                  <div className={estilos.acciones}>
                    <button type="button" className="hz-btn hz-btn-peligro" disabled={procesando !== null} onClick={() => bajar(evento)}>
                      <IconoBandera tamano={15} /> {procesando === evento.id ? "Bajando…" : "Bajar evento"}
                    </button>
                  </div>
                </>
              )}
            </div>
          </article>
        ))}
      </Pieza>
    </Entrada>
  );
}

/** HU26: «Editar y aprobar». Mismos campos y reglas que el formulario del estudiante (HU23), sin la foto. */
function FormularioEdicion({
  campos,
  lugares,
  ocupado,
  guardando,
  onCambio,
  onCancelar,
  onAprobar,
}: {
  campos: Campos;
  lugares: LugarEvento[];
  ocupado: boolean;
  guardando: boolean;
  onCambio: (campos: Campos) => void;
  onCancelar: () => void;
  onAprobar: () => void;
}) {
  const cambiar = <K extends keyof Campos>(campo: K, valor: Campos[K]) => onCambio({ ...campos, [campo]: valor });
  // «Otro lugar» elegido en el selector: se muestra elegido aunque el nombre todavía esté vacío.
  const [otroLugar, setOtroLugar] = useState(false);
  const seleccion = lugares.find((l) => String(l.id) === campos.lugarId);
  const opcionesLugar = [
    ...lugares.map((l) => ({ valor: String(l.id), etiqueta: l.nombre })),
    { valor: OTRO_LUGAR, etiqueta: "Otro lugar (fuera del catálogo)" },
  ];

  return (
    <form
      className={estilos.edicion}
      onSubmit={(e) => {
        e.preventDefault();
        onAprobar();
      }}
    >
      <label className={`hz-campo ${estilos.campo}`}>
        <span>Título</span>
        <input value={campos.titulo} maxLength={120} required onChange={(e) => cambiar("titulo", e.target.value)} />
      </label>
      <label className={`hz-campo ${estilos.campo}`}>
        <span>Descripción <em>(opcional)</em></span>
        <textarea value={campos.descripcion} maxLength={4000} rows={3} onChange={(e) => cambiar("descripcion", e.target.value)} />
      </label>
      <div className={estilos.dosColumnas}>
        <div className={estilos.campo} role="group" aria-label="Empieza">
          <span>Empieza</span>
          <CampoFechaHora nombre="inicio" valor={campos.inicio} onCambio={(v) => cambiar("inicio", v)} />
        </div>
        <div className={estilos.campo} role="group" aria-label="Termina">
          <span>Termina</span>
          <CampoFechaHora nombre="fin" valor={campos.fin} onCambio={(v) => cambiar("fin", v)} />
        </div>
      </div>
      <div className={estilos.campo}>
        <span>Lugar</span>
        <Selector
          valor={campos.lugarId || (otroLugar || campos.lugarTexto ? OTRO_LUGAR : "")}
          opciones={opcionesLugar}
          onCambio={(v) => {
            setOtroLugar(v === OTRO_LUGAR);
            cambiar("lugarId", v === OTRO_LUGAR ? "" : v);
          }}
          etiqueta="Lugar"
          placeholder="Elige un lugar del catálogo"
        />
      </div>
      {!seleccion && (
        <label className={`hz-campo ${estilos.campo}`}>
          <span>Nombre del lugar</span>
          <input value={campos.lugarTexto} maxLength={150} onChange={(e) => cambiar("lugarTexto", e.target.value)} />
        </label>
      )}
      <div className={estilos.dosColumnas}>
        <div className={estilos.campo}>
          <span>Asistencia</span>
          <Selector
            valor={campos.asistencia}
            opciones={[
              { valor: "FIJA", etiqueta: "Hay que llegar a la hora" },
              { valor: "LIBRE", etiqueta: "Se puede pasar un rato" },
            ]}
            onCambio={(v) => cambiar("asistencia", v as Campos["asistencia"])}
            etiqueta="Asistencia"
          />
        </div>
        {campos.asistencia === "LIBRE" && (
          <div className={estilos.dosColumnas}>
            <label className={`hz-campo ${estilos.campo}`}>
              <span>Visita mín. (min)</span>
              <input type="number" min={1} max={825} value={campos.visitaMin} onChange={(e) => cambiar("visitaMin", e.target.value)} />
            </label>
            <label className={`hz-campo ${estilos.campo}`}>
              <span>Visita máx. (min)</span>
              <input type="number" min={1} max={825} value={campos.visitaMax} onChange={(e) => cambiar("visitaMax", e.target.value)} />
            </label>
          </div>
        )}
      </div>
      <p className={estilos.ayudaEdicion}>La foto no cambia. Al aprobar, el evento se publica con estos datos y su autor recibe el aviso.</p>
      <div className={estilos.acciones}>
        <button type="button" className="hz-btn" disabled={ocupado} onClick={onCancelar}>
          Cancelar
        </button>
        <button type="submit" className="hz-btn hz-btn-primary" disabled={ocupado}>
          {guardando ? "Guardando…" : "Guardar y publicar"}
        </button>
      </div>
    </form>
  );
}

/** HU26: un evento del historial de rechazados. Solo lectura: el motivo, cuándo y quién. */
function TarjetaRechazado({ rechazado }: { rechazado: EventoRechazado }) {
  const { evento, fueBajado, admin, rechazadoEn } = rechazado;
  return (
    <article className={estilos.evento}>
      {evento.fotoUrl && <FotoEvento ruta={evento.fotoUrl} titulo={evento.titulo} />}
      <div className={estilos.detalle}>
        <span className={estilos.estadoRechazado}>{fueBajado ? "Bajado" : "Rechazado"}</span>
        <h3>{evento.titulo}</h3>
        <ul className={estilos.datos}>
          <li><IconoReloj tamano={15} /> {formatearFechaHora(evento.inicio)} – {formatearFechaHora(evento.fin)}</li>
          <li><IconoPin tamano={15} /> {evento.lugarId === null ? `Fuera del campus · ${evento.lugar}` : evento.lugar}</li>
        </ul>
        {evento.motivo && (
          <p className={estilos.motivoRechazo}>
            <strong>{fueBajado ? "Por qué se bajó:" : "Por qué se rechazó:"}</strong> {evento.motivo}
          </p>
        )}
        <p className={estilos.ayudaEdicion}>
          {fueBajado ? "Bajado" : "Rechazado"} el {formatearFechaHora(rechazadoEn)}
          {admin ? ` por ${admin}` : ""}.
        </p>
      </div>
    </article>
  );
}


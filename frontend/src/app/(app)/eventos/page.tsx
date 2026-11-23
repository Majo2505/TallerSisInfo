"use client";

import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useSearchParams } from "next/navigation";
import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import FondoVivo from "@/components/app/FondoVivo";
import fotoEventos from "@/components/app/fotos/eventos.webp";
import { IconoCalendario, IconoCamara } from "@/components/app/Iconos";
import { Selector } from "@/components/app/Selectores";
import CampoFechaHora from "@/components/eventos/CampoFechaHora";
import SubirFlyer, { BorradorFlyer } from "@/components/eventos/SubirFlyer";
import EventosDelCampus from "@/components/eventos/EventosDelCampus";
import FotoEvento from "@/components/eventos/FotoEvento";
import VistaPreviaEvento from "@/components/eventos/VistaPreviaEvento";
import VistaPreviaFoto from "@/components/eventos/VistaPreviaFoto";
import {
  EstadoFoto,
  FOTO_SIN_TOCAR,
  accionesFoto,
  archivoNuevo,
  datosDeFoto,
  deshacerQuitar,
  elegirFoto,
  quitarFotoActual,
  vistaFoto,
} from "@/lib/reglasFotoEvento";
import { Entrada, Odometro, Pieza, seguirPuntero } from "@/components/app/Movimiento";
import { ErrorApi } from "@/lib/api";
import { prepararFotoDelBorrador } from "@/lib/fotoEvento";
import {
  CampoFormulario,
  avisoLugarFueraDelCatalogo,
  avisosDelBorrador,
  borradorACampos,
  camposQueFaltan,
} from "@/lib/reglasBorradorFlyer";
import {
  actualizarEvento,
  cancelarEvento,
  crearEvento,
  EventoCampus,
  listarLugaresEventos,
  listarMisEventos,
  LugarEvento,
} from "@/lib/eventos";
import {
  ahoraBoliviaParaFormulario,
  CamposFormularioEvento,
  fechaBoliviaParaFormulario,
  formDataEvento,
  FOTO_EVENTO_MAX_BYTES,
  motivosDeRevision,
  presentacionEvento,
  validarFotoEvento,
  validarFormularioEvento,
} from "@/lib/reglasEventos";
import { formatearFechaHora } from "@/lib/reglasFlyer";
import { useRefrescoAutomatico } from "@/lib/useRefrescoAutomatico";
import estilos from "./eventos.module.css";

type Pestana = "campus" | "crear" | "mios";
type Campos = CamposFormularioEvento & { descripcion: string };

const CAMPOS_VACIOS: Campos = {
  titulo: "",
  descripcion: "",
  inicio: "",
  fin: "",
  lugarId: "",
  lugarTexto: "",
  asistencia: "FIJA",
  visitaMin: "",
  visitaMax: "",
};

const ETIQUETAS_ESTADO: Record<EventoCampus["estado"], string> = {
  PENDIENTE: "En revisión",
  PUBLICADO: "Publicado",
  RECHAZADO: "Rechazado",
  CANCELADO: "Cancelado",
};

export default function EventosPage() {
  return (
    <Suspense fallback={null}>
      <EventosDesdeEnlace />
    </Suspense>
  );
}

// La campanita lleva a «Mis eventos» con ?pestana=mios (cuando el admin aprueba o rechaza uno).
function EventosDesdeEnlace() {
  const inicial: Pestana = useSearchParams().get("pestana") === "mios" ? "mios" : "campus";
  return <Eventos key={inicial} inicial={inicial} />;
}

function Eventos({ inicial }: { inicial: Pestana }) {
  const reducir = useReducedMotion();
  const [pestana, setPestana] = useState<Pestana>(inicial);
  const [campos, setCampos] = useState<Campos>(CAMPOS_VACIOS);
  // «Otro lugar» elegido en el selector: se muestra elegido aunque el nombre todavía esté vacío.
  const [otroLugar, setOtroLugar] = useState(false);
  const [lugares, setLugares] = useState<LugarEvento[]>([]);
  const [eventos, setEventos] = useState<EventoCampus[]>([]);
  // Foto del formulario: sin tocar, una nueva elegida, o (solo al editar) la actual marcada para quitar.
  const [estadoFoto, setEstadoFoto] = useState<EstadoFoto<File>>(FOTO_SIN_TOCAR);
  // Al editar: ruta de la foto que el evento ya tiene (null si no tiene).
  const [fotoActual, setFotoActual] = useState<string | null>(null);
  const foto = archivoNuevo(estadoFoto);
  const setFoto = (archivo: File | null) => setEstadoFoto(elegirFoto(archivo));
  const [editando, setEditando] = useState<number | null>(null);
  const [cargando, setCargando] = useState(true);
  const [ahora, setAhora] = useState(0);
  const [guardando, setGuardando] = useState(false);
  const [mostrandoFlyer, setMostrandoFlyer] = useState(false);
  // HU24 parte 2: el formulario viene de un borrador de afiche.
  const [desdeFlyer, setDesdeFlyer] = useState(false);
  const [marcados, setMarcados] = useState<CampoFormulario[]>([]);
  const [avisos, setAvisos] = useState<string[]>([]);
  const [preparandoFoto, setPreparandoFoto] = useState(false);
  const [claveFoto, setClaveFoto] = useState(0);
  const turnoFoto = useRef(0);
  const [error, setError] = useState("");
  const [mensaje, setMensaje] = useState("");

  const obtenerDatos = useCallback(() => Promise.all([listarLugaresEventos(), listarMisEventos()]), []);

  // Sin botón «Actualizar»: cuando el admin aprueba o rechaza (aviso en vivo) o al volver a la pestaña, «Mis eventos» se
  // pone al día solo y sin parpadeo. No toca el formulario.
  const refrescar = useCallback(() => {
    listarMisEventos()
      .then((propios) => {
        setEventos(propios);
        setAhora(Date.now());
      })
      .catch(() => undefined);
  }, []);
  useRefrescoAutomatico(refrescar);

  useEffect(() => {
    let activo = true;
    obtenerDatos()
      .then(([opciones, propios]) => {
        if (!activo) return;
        setLugares(opciones);
        setEventos(propios);
        setAhora(Date.now());
      })
      .catch((e: unknown) => {
        if (activo) setError(e instanceof Error ? e.message : "No se pudieron cargar tus eventos.");
      })
      .finally(() => {
        if (activo) setCargando(false);
      });
    return () => { activo = false; };
  }, [obtenerDatos]);

  function cambiar<K extends keyof Campos>(campo: K, valor: Campos[K]) {
    setCampos((actuales) => ({ ...actuales, [campo]: valor }));
    setError("");
  }

  function quitarFoto() {
    turnoFoto.current += 1; // si todavía se estaba preparando una foto, se ignora su resultado
    setPreparandoFoto(false);
    setFoto(null);
    setClaveFoto((k) => k + 1);
  }

  function limpiarFormulario() {
    setCampos(CAMPOS_VACIOS);
    setOtroLugar(false);
    quitarFoto();
    setFotoActual(null);
    setEditando(null);
    setDesdeFlyer(false);
    setMarcados([]);
    setAvisos([]);
  }

  async function guardar(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (guardando) return;
    const problema = validarFormularioEvento(campos);
    if (problema) {
      setError(problema);
      return;
    }
    if (foto) {
      const problemaFoto = validarFotoEvento(foto);
      if (problemaFoto) {
        setError(problemaFoto);
        return;
      }
    }

    setGuardando(true);
    setError("");
    setMensaje("");
    try {
      const nombreLugar = lugares.find((l) => String(l.id) === campos.lugarId)?.nombre;
      const origen = editando !== null ? undefined : desdeFlyer ? "FLYER" : "FORMULARIO";
      const { foto: fotoNueva, quitarFoto: pedirQuitarFoto } = datosDeFoto(estadoFoto);
      const datos = formDataEvento(campos, fotoNueva, nombreLugar, origen, pedirQuitarFoto);
      const resultado = editando === null
        ? await crearEvento(datos)
        : await actualizarEvento(editando, datos);
      setMensaje(
        resultado.estado === "PUBLICADO"
          ? editando === null ? "Tu evento se publicó correctamente." : "Los cambios quedaron guardados y el evento está publicado."
          : resultado.estado === "PENDIENTE"
            ? `Tu evento quedó en revisión. ${resultado.motivo ?? ""}`.trim()
            : `El evento se guardó con estado: ${ETIQUETAS_ESTADO[resultado.estado].toLowerCase()}.`,
      );
      if (resultado.estado === "PUBLICADO") limpiarFormulario();
      else if (editando !== null) {
        // el formulario sigue en edición: ahora la foto actual es la que quedó guardada
        setFotoActual(resultado.fotoUrl);
        quitarFoto();
      }
      setPestana("mios");
      try {
        setEventos(await listarMisEventos());
      } catch (e) {
        setError(`El evento sí se guardó, pero no se pudo actualizar la lista: ${e instanceof Error ? e.message : "error inesperado"}`);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo guardar el evento.");
    } finally {
      setGuardando(false);
    }
  }

  function editar(evento: EventoCampus) {
    setOtroLugar(evento.lugarId === null);
    setCampos({
      titulo: evento.titulo,
      descripcion: evento.descripcion ?? "",
      inicio: fechaBoliviaParaFormulario(evento.inicio),
      fin: fechaBoliviaParaFormulario(evento.fin),
      lugarId: evento.lugarId === null ? "" : String(evento.lugarId),
      lugarTexto: evento.lugarId === null ? evento.lugar : "",
      asistencia: evento.asistencia,
      visitaMin: evento.visitaMin === null ? "" : String(evento.visitaMin),
      visitaMax: evento.visitaMax === null ? "" : String(evento.visitaMax),
    });
    quitarFoto();
    setFotoActual(evento.fotoUrl);
    setDesdeFlyer(false);
    setMarcados([]);
    setAvisos([]);
    setEditando(evento.id);
    setPestana("crear");
    setMensaje("");
    setError("");
    window.scrollTo({ top: 0, behavior: reducir ? "instant" : "smooth" });
  }

  async function cancelar(evento: EventoCampus) {
    if (!window.confirm(`¿Cancelar «${evento.titulo}»? Esta acción cambia el estado del evento a Cancelado.`)) return;
    setGuardando(true);
    setError("");
    setMensaje("");
    try {
      const actualizado = await cancelarEvento(evento.id);
      setEventos((actuales) => actuales.map((e) => e.id === actualizado.id ? actualizado : e));
      setMensaje("El evento quedó cancelado.");
    } catch (e) {
      setError(e instanceof ErrorApi ? e.message : e instanceof Error ? e.message : "No se pudo cancelar el evento.");
    } finally {
      setGuardando(false);
    }
  }

  async function usarBorrador(borrador: BorradorFlyer) {
    const { campos: nuevos, marcados: porCompletar } = borradorACampos(borrador.evento, borrador.faltantes, lugares);
    const imagen = borrador.archivo ?? imagenDelFlyer(borrador);
    quitarFoto();
    setFotoActual(null); // un borrador siempre crea un evento nuevo
    const turno = turnoFoto.current;
    setCampos(nuevos);
    setOtroLugar(false);
    setMarcados(porCompletar);
    setAvisos(avisosDelBorrador(borrador));
    setDesdeFlyer(true);
    setEditando(null);
    setMostrandoFlyer(false);
    setMensaje(
      porCompletar.length > 0
        ? "Revisa el borrador y completa los campos marcados con «falta»."
        : "Revisa los datos del borrador antes de publicar.",
    );
    setError("");

    if (!imagen) return;
    setPreparandoFoto(true);
    const preparada = await prepararFotoDelBorrador(imagen);
    if (turno !== turnoFoto.current) return; // el estudiante la quitó, eligió otra o cambió de formulario
    setPreparandoFoto(false);
    setFoto(preparada.foto);
    if (preparada.aviso) setAvisos((actuales) => [...actuales, preparada.aviso as string]);
  }

  const vistaDeFoto = vistaFoto(estadoFoto, fotoActual !== null);
  const accionesDeFoto = accionesFoto(estadoFoto, fotoActual !== null);
  const seleccion = lugares.find((l) => String(l.id) === campos.lugarId);
  const faltan = camposQueFaltan(marcados, campos);
  const publicados = eventos.filter((e) => e.estado === "PUBLICADO").length;
  const enRevision = eventos.filter((e) => e.estado === "PENDIENTE").length;
  const revision = motivosDeRevision(campos.inicio, campos.fin, Boolean(seleccion), Boolean(campos.lugarTexto.trim()), ahoraBoliviaParaFormulario());
  const opcionesLugar = [
    ...lugares.map((lugar) => ({ valor: String(lugar.id), etiqueta: lugar.nombre })),
    { valor: OTRO_LUGAR, etiqueta: "Otro lugar (lo escribo yo)" },
  ];
  const avisoLugar = desdeFlyer && editando === null ? avisoLugarFueraDelCatalogo(campos) : null;
  const marcaFalta = (campo: CampoFormulario) =>
    faltan.has(campo) ? <em className={estilos.falta}>falta</em> : null;

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header">
        <div className={estilos.portada} onPointerMove={seguirPuntero}>
          <FondoVivo foto={fotoEventos} enfoque="72% 45%" />
          <span className={estilos.etiqueta}>
            <IconoCalendario tamano={14} /> Vida universitaria
          </span>
          <h1>Eventos del campus</h1>
          <p>Publica un evento y compártelo con la comunidad. Si algo necesita revisión, verás el motivo en «Mis eventos».</p>
          {!cargando && publicados + enRevision > 0 && (
            <div className={estilos.cifrasPortada} aria-live="polite">
              {publicados > 0 && (
                <span>
                  <b><Odometro valor={publicados} /></b> {publicados === 1 ? "publicado" : "publicados"}
                </span>
              )}
              {enRevision > 0 && (
                <span>
                  <b><Odometro valor={enRevision} /></b> en revisión
                </span>
              )}
            </div>
          )}
        </div>
      </Pieza>

      <div className={estilos.pestanas} role="tablist" aria-label="Secciones de eventos">
        <button type="button" role="tab" aria-selected={pestana === "campus"} className={pestana === "campus" ? estilos.pestanaActiva : estilos.pestana} onClick={() => setPestana("campus")}>
          Del campus
        </button>
        <button type="button" role="tab" aria-selected={pestana === "crear"} className={pestana === "crear" ? estilos.pestanaActiva : estilos.pestana} onClick={() => setPestana("crear")}>
          {editando === null ? "Crear evento" : "Editar evento"}
        </button>
        <button type="button" role="tab" aria-selected={pestana === "mios"} className={pestana === "mios" ? estilos.pestanaActiva : estilos.pestana} onClick={() => setPestana("mios")}>
          Mis eventos <span>{eventos.length}</span>
        </button>
      </div>

      <AnimatePresence mode="wait" initial={false}>
        {error && (
          <motion.p key={error} className={estilos.error} role="alert" initial={reducir ? false : { opacity: 0, y: -6 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}>
            {error}
          </motion.p>
        )}
        {mensaje && (
          <motion.p key={mensaje} className={estilos.mensaje} role="status" initial={reducir ? false : { opacity: 0, y: -6 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}>
            {mensaje}
          </motion.p>
        )}
      </AnimatePresence>

      {pestana === "campus" ? (
        <EventosDelCampus />
      ) : pestana === "crear" ? (
        <div className={estilos.zonaCrear}>
        <Pieza as="section" className={estilos.tarjeta}>
          <div className={estilos.encabezadoFormulario}>
            <div>
              <h2>{editando === null ? "Cuéntanos qué va a pasar" : "Actualiza los datos"}</h2>
              <p>Los campos con * son obligatorios. Descripción y foto son opcionales.</p>
            </div>
            <button type="button" className={estilos.botonSecundario} onClick={() => setMostrandoFlyer(true)} disabled={guardando}>
              Completar desde un afiche
            </button>
          </div>

          {desdeFlyer && editando === null && (avisos.length > 0 || avisoLugar) && (
            <div className={estilos.aviso} role="status">
              <strong>Revisa estos avisos del afiche</strong>
              <ul>
                {avisos.map((aviso) => <li key={aviso}>{aviso}</li>)}
                {avisoLugar && <li>{avisoLugar}</li>}
              </ul>
            </div>
          )}

          <form className={estilos.formulario} onSubmit={(e) => void guardar(e)} noValidate>
            <label className={estilos.campo}>
              <span>Título <b>*</b> {marcaFalta("titulo")}</span>
              <input value={campos.titulo} maxLength={120} required aria-invalid={faltan.has("titulo") || undefined} onChange={(e) => cambiar("titulo", e.target.value)} placeholder="Ej.: Feria de ciencias" />
            </label>

            <label className={estilos.campo}>
              <span>Descripción <small>(opcional)</small></span>
              <textarea value={campos.descripcion} maxLength={4000} rows={4} onChange={(e) => cambiar("descripcion", e.target.value)} placeholder="Agrega detalles para quienes quieran participar." />
              <small className={estilos.contador}>{campos.descripcion.length}/4000</small>
            </label>

            <div className={estilos.dosColumnas}>
              <div className={estilos.campo} role="group" aria-label="Empieza">
                <span>Empieza <b>*</b> {marcaFalta("inicio")}</span>
                <CampoFechaHora nombre="inicio" valor={campos.inicio} invalido={faltan.has("inicio")} onCambio={(v) => cambiar("inicio", v)} />
              </div>
              <div className={estilos.campo} role="group" aria-label="Termina">
                <span>Termina <b>*</b> {marcaFalta("fin")}</span>
                <CampoFechaHora nombre="fin" valor={campos.fin} invalido={faltan.has("fin")} onCambio={(v) => cambiar("fin", v)} />
              </div>
            </div>
            <p className={estilos.ayuda}>Hora de Bolivia. Entre las 07:15 y las 21:00 del mismo día se publica al instante; fuera de ese horario pasa por revisión.</p>

            <div className={estilos.campo}>
              <span id="etiqueta-lugar">Lugar <b>*</b> {marcaFalta("lugar")}</span>
              <Selector
                valor={campos.lugarId || (otroLugar || campos.lugarTexto ? OTRO_LUGAR : "")}
                opciones={opcionesLugar}
                onCambio={(v) => {
                  setOtroLugar(v === OTRO_LUGAR);
                  cambiar("lugarId", v === OTRO_LUGAR ? "" : v);
                }}
                etiqueta="Lugar"
                placeholder="Elige un lugar del catálogo o escribe uno"
                invalido={faltan.has("lugar")}
              />
            </div>
            {!seleccion && (
              <label className={estilos.campo}>
                <span>Nombre del lugar <b>*</b></span>
                <input value={campos.lugarTexto} maxLength={150} required aria-invalid={faltan.has("lugar") || undefined} onChange={(e) => cambiar("lugarTexto", e.target.value)} placeholder="Ej.: Patio junto al coliseo" />
                <small className={estilos.ayuda}>Los lugares que no estén en el catálogo se envían a revisión.</small>
              </label>
            )}

            <fieldset className={estilos.grupo} role="radiogroup" aria-invalid={faltan.has("asistencia") || undefined}>
              <legend>Tipo de asistencia <b>*</b> {marcaFalta("asistencia")}</legend>
              <label className={estilos.opcion}>
                <input type="radio" name="asistencia" checked={campos.asistencia === "FIJA"} onChange={() => cambiar("asistencia", "FIJA")} />
                <span><strong>Hay que llegar a la hora</strong><small>La asistencia es fija durante el evento.</small></span>
              </label>
              <label className={estilos.opcion}>
                <input type="radio" name="asistencia" checked={campos.asistencia === "LIBRE"} onChange={() => cambiar("asistencia", "LIBRE")} />
                <span><strong>Se puede pasar un rato</strong><small>Indica cuánto tiempo se recomienda quedarse.</small></span>
              </label>
            </fieldset>

            {campos.asistencia === "LIBRE" && (
              <div className={estilos.dosColumnas}>
                <label className={estilos.campo}>
                  <span>Visita mínima (min) <b>*</b> {marcaFalta("visitaMin")}</span>
                  <input type="number" min={1} max={825} step={1} value={campos.visitaMin} aria-invalid={faltan.has("visitaMin") || undefined} onChange={(e) => cambiar("visitaMin", e.target.value)} />
                </label>
                <label className={estilos.campo}>
                  <span>Visita máxima (min) <b>*</b> {marcaFalta("visitaMax")}</span>
                  <input type="number" min={1} max={825} step={1} value={campos.visitaMax} aria-invalid={faltan.has("visitaMax") || undefined} onChange={(e) => cambiar("visitaMax", e.target.value)} />
                </label>
              </div>
            )}

            {vistaDeFoto === "actual" && fotoActual && (
              <figure className={`${estilos.fotoVista} ${estilos.soloAngosto}`}>
                <figcaption>Foto actual del evento</figcaption>
                <FotoEvento ruta={fotoActual} titulo={campos.titulo || "el evento"} />
              </figure>
            )}
            {vistaDeFoto === "nueva" && foto && (
              <figure className={`${estilos.fotoVista} ${estilos.soloAngosto}`}>
                <figcaption>{editando === null ? "Foto del evento" : "Foto nueva (se guardará al guardar los cambios)"}</figcaption>
                <VistaPreviaFoto archivo={foto} titulo={campos.titulo || "el evento"} />
              </figure>
            )}
            {vistaDeFoto === "quitada" && <p className={estilos.aviso} role="status">La foto actual se quitará al guardar los cambios.</p>}
            {editando !== null && vistaDeFoto === "ninguna" && <p className={estilos.ayudaFoto}>Este evento todavía no tiene foto.</p>}

            <label className={`${estilos.campo} ${estilos.campoFoto}`}>
              <span>{editando === null ? "Foto" : "Cambiar foto"} <small>(opcional; JPG o PNG, hasta 2 MB)</small></span>
              <span className={estilos.zonaFoto}>
                <IconoCamara tamano={20} />
                <span>{foto ? "Elegir otra foto" : "Elegir una foto"}</span>
                <small>{foto ? foto.name : "Una imagen ayuda a que más gente se anime"}</small>
              </span>
              <input key={claveFoto} className={estilos.archivoOculto} type="file" accept="image/jpeg,image/png,.jpg,.jpeg,.png" onChange={(e) => {
                turnoFoto.current += 1; // lo que el estudiante elige gana sobre la foto del afiche que se esté preparando
                setPreparandoFoto(false);
                const archivo = e.target.files?.[0] ?? null;
                const problema = archivo ? validarFotoEvento(archivo) : null;
                if (problema) {
                  if (editando === null) setFoto(null); // al editar se conserva lo que ya estaba decidido
                  setError(problema);
                  e.target.value = "";
                  return;
                }
                setError("");
                setFoto(archivo);
              }} />
              {preparandoFoto && <small className={estilos.ayuda}>Preparando la foto del afiche…</small>}
              {foto && <small className={estilos.ayuda}>{(foto.size / 1024).toFixed(0)} KB de {FOTO_EVENTO_MAX_BYTES / 1024} KB</small>}
            </label>
            {(accionesDeFoto.quitar || accionesDeFoto.descartarNueva || accionesDeFoto.deshacer) && (
              <div className={estilos.accionesFoto}>
                {accionesDeFoto.quitar && (
                  <button type="button" className={estilos.botonSecundario} disabled={guardando} onClick={() => setEstadoFoto(quitarFotoActual(estadoFoto))}>Quitar foto</button>
                )}
                {accionesDeFoto.descartarNueva && (
                  <button type="button" className={estilos.botonSecundario} disabled={guardando} onClick={quitarFoto}>
                    {editando === null ? "Quitar foto" : "Descartar la foto nueva"}
                  </button>
                )}
                {accionesDeFoto.deshacer && (
                  <button type="button" className={estilos.botonSecundario} disabled={guardando} onClick={() => setEstadoFoto(deshacerQuitar(estadoFoto))}>Deshacer</button>
                )}
              </div>
            )}

            <div className={estilos.accionesFormulario}>
              {editando !== null && <button type="button" className={estilos.botonSecundario} disabled={guardando} onClick={limpiarFormulario}>Salir de edición</button>}
              <button type="submit" className={estilos.botonPrimario} disabled={guardando || cargando || preparandoFoto}>
                {guardando ? "Guardando…" : editando === null ? "Publicar evento" : "Guardar cambios"}
              </button>
            </div>
          </form>
        </Pieza>
        <aside className={estilos.lateral} aria-label="Vista previa del evento">
          <VistaPreviaEvento
            titulo={campos.titulo}
            descripcion={campos.descripcion}
            inicio={campos.inicio}
            fin={campos.fin}
            lugar={seleccion?.nombre ?? campos.lugarTexto.trim()}
            revision={revision}
            asistencia={campos.asistencia}
            visitaMin={campos.visitaMin}
            visitaMax={campos.visitaMax}
            fotoNueva={vistaDeFoto === "nueva" ? foto : null}
            fotoActual={vistaDeFoto === "actual" ? fotoActual : null}
          />
        </aside>
        </div>
      ) : (
        <section className={estilos.lista} aria-busy={cargando}>
          <div className={estilos.tituloLista}>
            <div><h2>Mis eventos</h2><p>Consulta su estado y el motivo si alguno quedó pendiente o fue rechazado.</p></div>
          </div>
          {cargando ? <p className={estilos.vacio}>Cargando tus eventos…</p> : eventos.length === 0 ? (
            <div className={estilos.vacio}><strong>Aún no has creado eventos.</strong><span>Cuando publiques uno, aparecerá aquí.</span><button type="button" className={estilos.botonPrimario} onClick={() => setPestana("crear")}>Crear mi primer evento</button></div>
          ) : eventos.map((evento) => {
            const vista = presentacionEvento(evento.estado, evento.inicio, evento.fin, ahora);
            return (
            <article key={evento.id} className={estilos.evento}>
              {evento.fotoUrl && (
                <FotoEvento ruta={evento.fotoUrl} titulo={evento.titulo} />
              )}
              <div className={estilos.detalleEvento}>
                <div className={estilos.tituloEvento}>
                  <div><span className={`${estilos.estado} ${estilos[`estado${vista.clave}`]}`}>{vista.etiqueta}</span><h3>{evento.titulo}</h3></div>
                  <span className={estilos.horario}>{formatearFechaHora(evento.inicio)} – {formatearFechaHora(evento.fin)}</span>
                </div>
                <p className={estilos.lugar}>{evento.lugarId === null ? `Fuera del campus · ${evento.lugar}` : evento.lugar}</p>
                {evento.descripcion && <p className={estilos.descripcionEvento}>{evento.descripcion}</p>}
                {evento.asistencia === "FIJA" ? <p className={estilos.ayuda}>Hay que llegar a la hora</p> : <p className={estilos.ayuda}>Visita libre: {evento.visitaMin}–{evento.visitaMax} min</p>}
                {(evento.estado === "PENDIENTE" || evento.estado === "RECHAZADO") && evento.motivo && <p className={estilos.motivo}><strong>Motivo:</strong> {evento.motivo}</p>}
                {(vista.editable || vista.cancelable) && (
                  <div className={estilos.accionesEvento}>
                    {vista.editable && <button type="button" className={estilos.botonSecundario} onClick={() => editar(evento)} disabled={guardando}>Editar</button>}
                    {vista.cancelable && <button type="button" className={estilos.botonPeligro} onClick={() => void cancelar(evento)} disabled={guardando}>Cancelar evento</button>}
                  </div>
                )}
              </div>
            </article>
            );
          })}
        </section>
      )}

      {mostrandoFlyer && (
        <SubirFlyer onCerrar={() => setMostrandoFlyer(false)} onBorrador={(borrador) => void usarBorrador(borrador)} />
      )}
    </Entrada>
  );
}

// Opción del selector de lugar para volver a escribir uno a mano (no es un id del catálogo).
const OTRO_LUGAR = "otro";

function imagenDelFlyer(borrador: BorradorFlyer): File | null {
  if (!borrador.imagen) return null;
  const binario = atob(borrador.imagen.contenidoBase64);
  const bytes = new Uint8Array(binario.length);
  for (let i = 0; i < binario.length; i++) bytes[i] = binario.charCodeAt(i);
  return new File([bytes], "afiche.jpg", { type: borrador.imagen.tipoMime });
}

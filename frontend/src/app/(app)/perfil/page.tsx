"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import FondoVivo from "@/components/app/FondoVivo";
import {
  IconoAjustes,
  IconoBandera,
  IconoCalendario,
  IconoCandado,
  IconoCheck,
  IconoPersona,
  IconoSalir,
} from "@/components/app/Iconos";
import { Entrada, Pieza, RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { Selector } from "@/components/app/Selectores";
import { guardarConfiguracionMatching, obtenerConfiguracionMatching } from "@/lib/matching";
import { guardarConfiguracion, obtenerConfiguracion } from "@/lib/notificaciones";
import { CARRERAS, LARGO_MAXIMO_NOMBRE, Perfil as DatosPerfil, SEMESTRES, actualizarPerfil, obtenerPerfil } from "@/lib/perfil";
import { actualizarNombreSesion, cerrarSesion } from "@/services/authService";
import { iniciales } from "@/lib/iniciales";
import { useSesion } from "@/lib/useSesion";
import estilos from "./perfil.module.css";

type Pestana = "informacion" | "ajustes" | "seguridad" | "reportar";

// Un reporte es un aviso puntual, no un texto largo.
const LARGO_MAXIMO_REPORTE = 500;
const TIPOS_REPORTE = [
  { valor: "error", etiqueta: "Algo no funciona" },
  { valor: "lugar", etiqueta: "Información incorrecta de un lugar" },
  { valor: "sugerencia", etiqueta: "Sugerencia" },
];

const PESTANAS: { id: Pestana; nombre: string; corto: string; Icono: typeof IconoPersona }[] = [
  { id: "informacion", nombre: "Información personal", corto: "Datos", Icono: IconoPersona },
  { id: "ajustes", nombre: "Ajustes", corto: "Ajustes", Icono: IconoAjustes },
  { id: "seguridad", nombre: "Seguridad", corto: "Seguridad", Icono: IconoCandado },
  { id: "reportar", nombre: "Reportar", corto: "Reportar", Icono: IconoBandera },
];

export default function Perfil() {
  const router = useRouter();
  const { sesion } = useSesion();
  const aviso = useAviso();
  const reducir = useReducedMotion();
  const [pestana, setPestana] = useState<Pestana>("informacion");
  // HU21: datos reales del perfil; el formulario parte de ellos y se compara para saber si hay cambios.
  const [perfil, setPerfil] = useState<DatosPerfil | null>(null);
  const [errorPerfil, setErrorPerfil] = useState("");
  const [nombre, setNombre] = useState("");
  const [carrera, setCarrera] = useState("");
  const [semestre, setSemestre] = useState("");
  const [errorNombre, setErrorNombre] = useState("");
  const [errorGuardar, setErrorGuardar] = useState("");
  const [guardandoPerfil, setGuardandoPerfil] = useState(false);
  const [avisosActivos, setAvisosActivos] = useState<boolean | null>(null);
  const [errorAvisos, setErrorAvisos] = useState("");
  const [guardandoAvisos, setGuardandoAvisos] = useState(false);
  const [tipoReporte, setTipoReporte] = useState("error");
  const [textoReporte, setTextoReporte] = useState("");
  const [errorReporte, setErrorReporte] = useState("");
  // HU12: consentimiento para que compañeros con tiempo libre compatible te encuentren. Desactivado por defecto.
  const [visibleMatching, setVisibleMatching] = useState<boolean | null>(null);
  const [errorMatching, setErrorMatching] = useState("");
  const [guardandoMatching, setGuardandoMatching] = useState(false);

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    obtenerPerfil()
      .then((datos) => {
        if (!activo) return;
        setPerfil(datos);
        setNombre(datos.nombre);
        setCarrera(datos.carrera ?? "");
        setSemestre(datos.semestre ? String(datos.semestre) : "");
      })
      .catch((e: Error) => {
        if (activo) setErrorPerfil(e.message);
      });
    return () => {
      activo = false;
    };
  }, [sesion]);

  const hayCambios =
    perfil !== null &&
    (nombre.trim() !== perfil.nombre || (carrera || null) !== perfil.carrera || (semestre ? Number(semestre) : null) !== perfil.semestre);

  async function guardarPerfil(e: React.FormEvent) {
    e.preventDefault();
    setErrorGuardar("");
    const limpio = nombre.trim();
    if (!limpio) return setErrorNombre("Tu nombre es obligatorio.");
    if (limpio.length > LARGO_MAXIMO_NOMBRE) return setErrorNombre(`Tu nombre puede tener hasta ${LARGO_MAXIMO_NOMBRE} caracteres.`);
    setErrorNombre("");
    setGuardandoPerfil(true);
    try {
      const nuevo = await actualizarPerfil({ nombre: limpio, carrera: carrera || null, semestre: semestre ? Number(semestre) : null });
      setPerfil(nuevo);
      setNombre(nuevo.nombre);
      actualizarNombreSesion(nuevo.nombre);
      aviso.mostrar("Tu perfil se actualizó.");
    } catch (err) {
      setErrorGuardar((err as Error).message);
    } finally {
      setGuardandoPerfil(false);
    }
  }

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    obtenerConfiguracion()
      .then((c) => {
        if (activo) setAvisosActivos(c.notificacionesActivas);
      })
      .catch((e: Error) => {
        if (activo) setErrorAvisos(e.message);
      });
    return () => {
      activo = false;
    };
  }, [sesion]);

  async function cambiarAvisos(valor: boolean) {
    setGuardandoAvisos(true);
    setErrorAvisos("");
    try {
      const c = await guardarConfiguracion({ notificacionesActivas: valor });
      setAvisosActivos(c.notificacionesActivas);
    } catch (e) {
      setErrorAvisos((e as Error).message);
    } finally {
      setGuardandoAvisos(false);
    }
  }

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    obtenerConfiguracionMatching()
      .then((c) => {
        if (activo) setVisibleMatching(c.visible);
      })
      .catch((e: Error) => {
        if (activo) setErrorMatching(e.message);
      });
    return () => {
      activo = false;
    };
  }, [sesion]);

  async function cambiarMatching(valor: boolean) {
    setGuardandoMatching(true);
    setErrorMatching("");
    try {
      const c = await guardarConfiguracionMatching(valor);
      setVisibleMatching(c.visible);
    } catch (e) {
      setErrorMatching((e as Error).message);
    } finally {
      setGuardandoMatching(false);
    }
  }

  function salir() {
    cerrarSesion();
    router.replace("/login");
  }

  const usuario = sesion?.usuario;
  const actual = PESTANAS.find((p) => p.id === pestana)!;

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header" className={estilos.cabecera}>
        <FondoVivo />
        <motion.div
          className={estilos.avatar}
          aria-hidden="true"
          initial={reducir ? false : { scale: 0.8, rotate: -12 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 260, damping: 16, delay: 0.15 }}
        >
          {iniciales(usuario?.nombre ?? "")}
        </motion.div>
        <div className={estilos.datos}>
          <h1 className={estilos.nombre}>{usuario?.nombre}</h1>
          <p className={estilos.correo}>{usuario?.correo}</p>
          <div className={estilos.insignias}>
            <span className={estilos.verificado}>
              <IconoCheck tamano={14} /> Verificado
            </span>
            {perfil?.carrera && <span className={estilos.insignia}>{perfil.carrera}</span>}
            {perfil?.semestre && <span className={estilos.insignia}>Semestre {perfil.semestre}</span>}
          </div>
        </div>
      </Pieza>

      <Pieza>
        <div className={estilos.pestanas} role="tablist" aria-label="Secciones del perfil">
          {PESTANAS.map(({ id, nombre: etiqueta, corto, Icono }) => (
            <motion.button
              key={id}
              type="button"
              role="tab"
              aria-selected={pestana === id}
              title={etiqueta}
              className={pestana === id ? estilos.pestanaActiva : estilos.pestana}
              onClick={() => setPestana(id)}
              whileTap={{ scale: 0.95 }}
              transition={RESORTE_TOQUE}
            >
              {pestana === id && (
                <motion.span
                  layoutId="pestana-perfil"
                  className={estilos.pastilla}
                  transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
                />
              )}
              <span className={estilos.textoPestana}>
                <Icono tamano={18} />
                <span>{corto}</span>
              </span>
            </motion.button>
          ))}
        </div>
      </Pieza>

      <Pieza>
        <AnimatePresence mode="wait" initial={false}>
          <motion.section
            key={pestana}
            className={estilos.panel}
            role="tabpanel"
            aria-label={actual.nombre}
            initial={reducir ? false : { opacity: 0, y: 12, filter: "blur(4px)" }}
            animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
            exit={reducir ? undefined : { opacity: 0, y: -8, filter: "blur(4px)" }}
            transition={{ duration: 0.28, ease: SALIDA }}
          >
            {pestana === "informacion" && (
              <form className={estilos.formulario} onSubmit={(e) => void guardarPerfil(e)} noValidate>
                <h2 className={estilos.seccion}>Información personal</h2>
                {errorPerfil && (
                  <p className={estilos.error} role="alert">
                    {errorPerfil}
                  </p>
                )}
                {errorGuardar && (
                  <motion.p
                    key={errorGuardar}
                    className={estilos.error}
                    role="alert"
                    initial={reducir ? false : { x: 0 }}
                    animate={{ x: [0, -8, 8, -5, 5, 0] }}
                    transition={{ duration: 0.42 }}
                  >
                    {errorGuardar}
                  </motion.p>
                )}
                <label className={estilos.campo}>
                  Nombre completo
                  <input
                    value={nombre}
                    onChange={(e) => {
                      setNombre(e.target.value);
                      if (errorNombre) setErrorNombre("");
                    }}
                    autoComplete="name"
                    maxLength={LARGO_MAXIMO_NOMBRE}
                    disabled={!perfil}
                    aria-invalid={!!errorNombre}
                    aria-describedby={errorNombre ? "error-nombre" : undefined}
                  />
                  {errorNombre && (
                    <motion.small
                      key={errorNombre}
                      id="error-nombre"
                      className={estilos.errorCampo}
                      initial={reducir ? false : { x: 0 }}
                      animate={{ x: [0, -6, 6, -3, 3, 0] }}
                      transition={{ duration: 0.4 }}
                    >
                      {errorNombre}
                    </motion.small>
                  )}
                </label>
                <label className={estilos.campo}>
                  Correo institucional
                  <input value={perfil?.correo ?? usuario?.correo ?? ""} disabled readOnly />
                  <small>Tu correo institucional y tu rol no se pueden cambiar.</small>
                </label>
                <div className={estilos.fila}>
                  <div className={estilos.campo}>
                    Carrera
                    <Selector
                      valor={carrera}
                      etiqueta="Carrera"
                      deshabilitado={!perfil}
                      onCambio={setCarrera}
                      opciones={[
                        { valor: "", etiqueta: "Sin especificar" },
                        // Si la carrera guardada no está en la lista, igual se muestra.
                        ...(carrera && !CARRERAS.includes(carrera) ? [{ valor: carrera, etiqueta: carrera }] : []),
                        ...CARRERAS.map((c) => ({ valor: c, etiqueta: c })),
                      ]}
                    />
                  </div>
                  <div className={estilos.campo}>
                    Semestre
                    <Selector
                      valor={semestre}
                      etiqueta="Semestre"
                      deshabilitado={!perfil}
                      onCambio={setSemestre}
                      opciones={[{ valor: "", etiqueta: "Sin especificar" }, ...SEMESTRES.map((s) => ({ valor: String(s), etiqueta: `${s}.° semestre` }))]}
                    />
                  </div>
                </div>
                <div className={estilos.acciones}>
                  <button type="submit" className={estilos.primario} disabled={!hayCambios || guardandoPerfil}>
                    {guardandoPerfil ? "Guardando…" : "Guardar cambios"}
                  </button>
                  <Link href="/horario" className={estilos.secundario}>
                    <IconoCalendario tamano={18} /> Editar mi horario
                  </Link>
                </div>
              </form>
            )}

            {pestana === "ajustes" && (
              <div className={estilos.formulario}>
                <h2 className={estilos.seccion}>Ajustes</h2>
                <label className={estilos.interruptor}>
                  <span className={estilos.interruptorTexto}>
                    <strong>Avisos de puente disponible</strong>
                    <small>Te avisamos cuando empieza un hueco libre entre clases.</small>
                  </span>
                  <input
                    type="checkbox"
                    role="switch"
                    className={estilos.oculto}
                    checked={avisosActivos ?? false}
                    disabled={avisosActivos === null || guardandoAvisos}
                    onChange={(e) => cambiarAvisos(e.target.checked)}
                  />
                  <span className={avisosActivos ? estilos.rielActivo : estilos.riel} aria-hidden="true">
                    <motion.span
                      className={estilos.perilla}
                      animate={{ x: avisosActivos ? 22 : 0 }}
                      transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 600, damping: 30 }}
                    />
                  </span>
                </label>
                {errorAvisos && (
                  <motion.p
                    className={estilos.error}
                    role="alert"
                    initial={reducir ? false : { x: 0 }}
                    animate={{ x: [0, -8, 8, -5, 5, 0] }}
                    transition={{ duration: 0.42 }}
                  >
                    {errorAvisos}
                  </motion.p>
                )}
                <label className={estilos.interruptor}>
                  <span className={estilos.interruptorTexto}>
                    <strong>Dejar que compañeros con tiempo compatible me encuentren</strong>
                    <small>Tus compañeros de campus verán tu nombre, tu carrera y el tramo libre que comparten. Nunca tu correo.</small>
                  </span>
                  <input
                    type="checkbox"
                    role="switch"
                    className={estilos.oculto}
                    checked={visibleMatching ?? false}
                    disabled={visibleMatching === null || guardandoMatching}
                    onChange={(e) => cambiarMatching(e.target.checked)}
                  />
                  <span className={visibleMatching ? estilos.rielActivo : estilos.riel} aria-hidden="true">
                    <motion.span
                      className={estilos.perilla}
                      animate={{ x: visibleMatching ? 22 : 0 }}
                      transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 600, damping: 30 }}
                    />
                  </span>
                </label>
                {errorMatching && (
                  <motion.p
                    className={estilos.error}
                    role="alert"
                    initial={reducir ? false : { x: 0 }}
                    animate={{ x: [0, -8, 8, -5, 5, 0] }}
                    transition={{ duration: 0.42 }}
                  >
                    {errorMatching}
                  </motion.p>
                )}
                <Link href="/configuracion/ubicacion" className={estilos.secundario}>
                  <IconoAjustes tamano={18} /> Configurar campus y permiso de ubicación
                </Link>
                <button type="button" className={estilos.salir} onClick={salir}>
                  <IconoSalir tamano={18} /> Cerrar sesión
                </button>
              </div>
            )}

            {pestana === "seguridad" && (
              <form className={estilos.formulario} onSubmit={(e) => e.preventDefault()}>
                <h2 className={estilos.seccion}>Seguridad</h2>
                <label className={estilos.campo}>
                  Contraseña actual
                  <input type="password" disabled autoComplete="current-password" />
                </label>
                <label className={estilos.campo}>
                  Nueva contraseña
                  <input type="password" disabled autoComplete="new-password" />
                </label>
                <label className={estilos.campo}>
                  Confirmar nueva contraseña
                  <input type="password" disabled autoComplete="new-password" />
                </label>
                <p className={estilos.nota}>Cambiar la contraseña va a estar disponible pronto.</p>
                <button type="submit" className={estilos.primario} disabled>
                  Cambiar contraseña
                </button>
              </form>
            )}

            {pestana === "reportar" && (
              <form
                className={estilos.formulario}
                noValidate
                onSubmit={(e) => {
                  e.preventDefault();
                  const limpio = textoReporte.trim();
                  if (limpio.length < 10) return setErrorReporte("Cuéntanos un poco más: al menos 10 caracteres.");
                  if (limpio.length > LARGO_MAXIMO_REPORTE) return setErrorReporte(`Máximo ${LARGO_MAXIMO_REPORTE} caracteres.`);
                  setErrorReporte("");
                  aviso.mostrar("Enviar reportes va a estar disponible pronto.");
                }}
              >
                <h2 className={estilos.seccion}>Reportar un problema</h2>
                <div className={estilos.campo}>
                  Tipo de reporte
                  <Selector valor={tipoReporte} opciones={TIPOS_REPORTE} onCambio={setTipoReporte} etiqueta="Tipo de reporte" />
                </div>
                <label className={estilos.campo}>
                  Cuéntanos qué pasó
                  <textarea
                    rows={4}
                    value={textoReporte}
                    maxLength={LARGO_MAXIMO_REPORTE}
                    placeholder="Ej: la cafetería figura abierta, pero cierra a las 15:00."
                    aria-invalid={!!errorReporte}
                    aria-describedby="contador-reporte"
                    onChange={(e) => {
                      setTextoReporte(e.target.value);
                      if (errorReporte) setErrorReporte("");
                    }}
                  />
                  <span className={estilos.pieCampo}>
                    {errorReporte ? (
                      <motion.small
                        key={errorReporte}
                        className={estilos.errorCampo}
                        role="alert"
                        initial={reducir ? false : { x: 0 }}
                        animate={{ x: [0, -6, 6, -3, 3, 0] }}
                        transition={{ duration: 0.4 }}
                      >
                        {errorReporte}
                      </motion.small>
                    ) : (
                      <small>Un aviso breve y concreto.</small>
                    )}
                    <small
                      id="contador-reporte"
                      className={textoReporte.length >= LARGO_MAXIMO_REPORTE * 0.9 ? estilos.contadorLimite : estilos.contador}
                    >
                      {textoReporte.length}/{LARGO_MAXIMO_REPORTE}
                    </small>
                  </span>
                </label>
                <button type="submit" className={estilos.primario}>
                  Enviar reporte
                </button>
              </form>
            )}
          </motion.section>
        </AnimatePresence>
      </Pieza>

      <AvisoFlotante texto={aviso.texto} />
    </Entrada>
  );
}

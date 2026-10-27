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
import { guardarConfiguracion, obtenerConfiguracion } from "@/lib/notificaciones";
import { CARRERAS, SEMESTRES } from "@/lib/maqueta";
import { cerrarSesion } from "@/services/authService";
import { iniciales } from "@/lib/iniciales";
import { useSesion } from "@/lib/useSesion";
import estilos from "./perfil.module.css";

type Pestana = "informacion" | "ajustes" | "seguridad" | "reportar";

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
  const [nombre, setNombre] = useState(sesion?.usuario.nombre ?? "");
  const [carrera, setCarrera] = useState(CARRERAS[0]);
  const [semestre, setSemestre] = useState(5);
  const [avisosActivos, setAvisosActivos] = useState<boolean | null>(null);
  const [errorAvisos, setErrorAvisos] = useState("");
  const [guardandoAvisos, setGuardandoAvisos] = useState(false);

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
            <span className={estilos.insignia}>{carrera}</span>
            <span className={estilos.insignia}>Semestre {semestre}</span>
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
              <form
                className={estilos.formulario}
                onSubmit={(e) => {
                  e.preventDefault();
                  aviso.mostrar("Guardar el perfil estará disponible pronto.");
                }}
              >
                <h2 className={estilos.seccion}>Información personal</h2>
                <label className={estilos.campo}>
                  Nombre completo
                  <input value={nombre} onChange={(e) => setNombre(e.target.value)} autoComplete="name" />
                </label>
                <label className={estilos.campo}>
                  Correo institucional
                  <input value={usuario?.correo ?? ""} disabled readOnly />
                  <small>Tu correo institucional está verificado y no se puede cambiar.</small>
                </label>
                <div className={estilos.fila}>
                  <label className={estilos.campo}>
                    Carrera
                    <select value={carrera} onChange={(e) => setCarrera(e.target.value)}>
                      {CARRERAS.map((c) => (
                        <option key={c}>{c}</option>
                      ))}
                    </select>
                  </label>
                  <label className={estilos.campo}>
                    Semestre
                    <select value={semestre} onChange={(e) => setSemestre(Number(e.target.value))}>
                      {SEMESTRES.map((s) => (
                        <option key={s} value={s}>
                          {s}°
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
                <div className={estilos.acciones}>
                  <button type="submit" className={estilos.primario}>
                    Guardar cambios
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
                <p className={estilos.nota}>Cambiar la contraseña estará disponible pronto.</p>
                <button type="submit" className={estilos.primario} disabled>
                  Cambiar contraseña
                </button>
              </form>
            )}

            {pestana === "reportar" && (
              <form
                className={estilos.formulario}
                onSubmit={(e) => {
                  e.preventDefault();
                  aviso.mostrar("El envío de reportes estará disponible pronto.");
                }}
              >
                <h2 className={estilos.seccion}>Reportar un problema</h2>
                <label className={estilos.campo}>
                  Tipo de reporte
                  <select defaultValue="error">
                    <option value="error">Algo no funciona</option>
                    <option value="lugar">Información incorrecta de un lugar</option>
                    <option value="sugerencia">Sugerencia</option>
                  </select>
                </label>
                <label className={estilos.campo}>
                  Cuéntanos qué pasó
                  <textarea rows={4} />
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

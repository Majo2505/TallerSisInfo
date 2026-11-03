"use client";

import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useEffect, useRef, useState } from "react";
import { IconoCamara, IconoCheck, IconoDestello } from "@/components/app/Iconos";
import { RESORTE_TOQUE, SALIDA } from "@/components/app/Movimiento";
import { Selector, SelectorHora } from "@/components/app/Selectores";
import {
  ClaseLeida,
  FOTO_BYTES_MAXIMOS,
  FOTO_TIPOS,
  HORA_MAXIMA,
  HORA_MINIMA,
  OpcionAsistencia,
  ResultadoLote,
  cambiarAsistencias,
  gruposEnChoque,
  guardarLote,
  leerFotoHorario,
} from "@/lib/horario";
import estilos from "./FotoHorario.module.css";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const OPCIONES_DIA = [1, 2, 3, 4, 5, 6].map((d) => ({ valor: String(d), etiqueta: DIAS[d] }));
const MENSAJES = ["Buscando tus materias…", "Leyendo días y horas…", "Revisando las aulas…", "Ordenando tu semana…"];

type Fila = ClaseLeida & { clave: number; incluir: boolean };
type Paso = "elegir" | "leyendo" | "revisar" | "guardando" | "asistencia" | "resultado";

type Props = {
  onCerrar: () => void;
  /** Se guardó al menos una clase: la pantalla recarga el horario y los puentes. */
  onGuardado: (cantidad: number) => void;
  /** Cambió a cuáles clases asiste: la pantalla recarga los puentes. */
  onActualizado: () => void;
};

/**
 * Registro del horario con una foto (HU2): el estudiante elige o toma la foto, el backend la lee con un modelo
 * de visión, el estudiante revisa y corrige las clases detectadas y las guarda de una vez (origen FOTO).
 */
export default function FotoHorario({ onCerrar, onGuardado, onActualizado }: Props) {
  const reducir = useReducedMotion();
  const [paso, setPaso] = useState<Paso>("elegir");
  const [archivo, setArchivo] = useState<File | null>(null);
  const [vista, setVista] = useState<string | null>(null);
  const [filas, setFilas] = useState<Fila[]>([]);
  const [descartadas, setDescartadas] = useState(0);
  const [resultado, setResultado] = useState<ResultadoLote | null>(null);
  const [grupos, setGrupos] = useState<OpcionAsistencia[][]>([]);
  const [autorizo, setAutorizo] = useState(false);
  const [guardandoAsistencia, setGuardandoAsistencia] = useState(false);
  const [error, setError] = useState("");
  const [mensaje, setMensaje] = useState(0);
  const [arrastrando, setArrastrando] = useState(false);
  const galeria = useRef<HTMLInputElement>(null);
  const camara = useRef<HTMLInputElement>(null);
  const ocupado = paso === "leyendo" || paso === "guardando" || guardandoAsistencia;

  // La vista previa usa una URL temporal: se libera al cambiar de foto y, la última, al cerrar.
  const urlVista = useRef<string | null>(null);
  useEffect(() => () => {
    if (urlVista.current) URL.revokeObjectURL(urlVista.current);
  }, []);

  useEffect(() => {
    if (paso !== "leyendo") return;
    const t = setInterval(() => setMensaje((m) => (m + 1) % MENSAJES.length), 1600);
    return () => clearInterval(t);
  }, [paso]);

  useEffect(() => {
    const alTeclear = (e: KeyboardEvent) => e.key === "Escape" && !ocupado && onCerrar();
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [ocupado, onCerrar]);

  function elegir(f: File | undefined) {
    setError("");
    if (!f) return;
    if (!FOTO_TIPOS.includes(f.type)) return setError("La foto tiene que ser JPG, PNG o WebP.");
    if (f.size > FOTO_BYTES_MAXIMOS) return setError("La foto no puede superar los 8 MB.");
    if (urlVista.current) URL.revokeObjectURL(urlVista.current);
    urlVista.current = URL.createObjectURL(f);
    setVista(urlVista.current);
    setArchivo(f);
  }

  async function leer() {
    if (!archivo) return;
    setPaso("leyendo");
    setError("");
    setMensaje(0);
    try {
      const r = await leerFotoHorario(archivo);
      setFilas(r.clases.map((c, i) => ({ ...c, clave: i, incluir: true })));
      setDescartadas(r.descartadas);
      setPaso("revisar");
    } catch (e) {
      setError((e as Error).message);
      setPaso("elegir");
    }
  }

  async function guardar() {
    const elegidas = filas.filter((f) => f.incluir);
    if (elegidas.length === 0) return;
    setPaso("guardando");
    setError("");
    try {
      const r = await guardarLote(elegidas.map(({ materia, diaSemana, horaInicio, horaFin, aula }) => ({ materia, diaSemana, horaInicio, horaFin, aula: aula || null })));
      setResultado(r);
      if (r.guardados.length > 0) onGuardado(r.guardados.length);
      const choques = gruposEnChoque(r.guardados);
      setGrupos(choques);
      setPaso(choques.length > 0 ? "asistencia" : "resultado");
    } catch (e) {
      setError((e as Error).message);
      setPaso("revisar");
    }
  }

  async function guardarAsistencia() {
    setGuardandoAsistencia(true);
    setError("");
    try {
      const opciones = grupos.flat();
      await cambiarAsistencias(opciones.map((o) => ({ id: o.id, asiste: o.asiste, ...(autorizo ? { autorizacionCarrera: true } : {}) })));
      onActualizado();
      setPaso("resultado");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setGuardandoAsistencia(false);
    }
  }

  const marcarAsistencia = (id: number, asiste: boolean) =>
    setGrupos((gs) => gs.map((g) => g.map((o) => (o.id === id ? { ...o, asiste } : o))));

  const cambiarFila = (clave: number, cambios: Partial<Fila>) =>
    setFilas((fs) => fs.map((f) => (f.clave === clave ? { ...f, ...cambios } : f)));

  const incluidas = filas.filter((f) => f.incluir).length;

  return (
    <motion.div
      className={estilos.fondo}
      onMouseDown={(e) => e.target === e.currentTarget && !ocupado && onCerrar()}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
    >
      <motion.section
        className={estilos.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="foto-titulo"
        initial={reducir ? false : { opacity: 0, scale: 0.95, y: 24 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={reducir ? { opacity: 0 } : { opacity: 0, scale: 0.97, y: 12 }}
        transition={{ type: "spring", stiffness: 380, damping: 32 }}
      >
        <header className={estilos.cabecera}>
          <span className={estilos.icono} aria-hidden="true">
            <IconoCamara tamano={22} />
          </span>
          <div>
            <h2 id="foto-titulo">Horario desde una foto</h2>
            <p>
              {paso === "revisar" || paso === "guardando"
                ? "Revisa lo que leímos antes de guardar."
                : paso === "asistencia"
                  ? "Estas clases coinciden, ¿a cuáles asistes?"
                  : "La IA lee tus materias, días, horas y aulas."}
            </p>
          </div>
          <button type="button" className={estilos.cerrar} onClick={onCerrar} disabled={ocupado} aria-label="Cerrar">
            ×
          </button>
        </header>

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

        <AnimatePresence mode="wait" initial={false}>
          {(paso === "elegir" || paso === "leyendo") && (
            <motion.div key="elegir" className={estilos.cuerpo} exit={{ opacity: 0, y: -8 }} transition={{ duration: 0.2, ease: SALIDA }}>
              <div
                className={`${estilos.zona} ${arrastrando ? estilos.zonaActiva : ""}`}
                onDragOver={(e) => {
                  e.preventDefault();
                  setArrastrando(true);
                }}
                onDragLeave={() => setArrastrando(false)}
                onDrop={(e) => {
                  e.preventDefault();
                  setArrastrando(false);
                  if (!ocupado) elegir(e.dataTransfer.files[0]);
                }}
              >
                {vista ? (
                  <div className={estilos.vista}>
                    {/* eslint-disable-next-line @next/next/no-img-element -- vista previa local (blob:), no pasa por el optimizador */}
                    <img src={vista} alt="Vista previa de tu horario" />
                    {paso === "leyendo" && (
                      <>
                        <span className={estilos.escaneo} aria-hidden="true" />
                        <span className={estilos.velo} aria-hidden="true" />
                      </>
                    )}
                  </div>
                ) : (
                  <div className={estilos.vacio}>
                    <span className={estilos.vacioIcono} aria-hidden="true">
                      <IconoCamara tamano={28} />
                    </span>
                    <strong>Toma o elige la foto de tu horario</strong>
                    <span>Que se vean bien los días, las horas y las materias. JPG, PNG o WebP, hasta 8 MB.</span>
                  </div>
                )}
              </div>

              {paso === "leyendo" ? (
                <div className={estilos.leyendo} role="status">
                  <AnimatePresence mode="wait">
                    <motion.span
                      key={mensaje}
                      initial={reducir ? false : { opacity: 0, y: 8 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={{ opacity: 0, y: -8 }}
                      transition={{ duration: 0.25 }}
                    >
                      <IconoDestello tamano={16} /> {MENSAJES[mensaje]}
                    </motion.span>
                  </AnimatePresence>
                </div>
              ) : (
                <div className={estilos.acciones}>
                  <input ref={camara} type="file" accept="image/*" capture="environment" hidden onChange={(e) => elegir(e.target.files?.[0])} />
                  <input ref={galeria} type="file" accept="image/jpeg,image/png,image/webp" hidden onChange={(e) => elegir(e.target.files?.[0])} />
                  <button type="button" className={estilos.secundario} onClick={() => camara.current?.click()}>
                    Tomar foto
                  </button>
                  <button type="button" className={estilos.secundario} onClick={() => galeria.current?.click()}>
                    {archivo ? "Elegir otra" : "Elegir de la galería"}
                  </button>
                  <motion.button
                    type="button"
                    className={estilos.primario}
                    disabled={!archivo}
                    onClick={() => void leer()}
                    whileTap={{ scale: 0.97 }}
                    transition={RESORTE_TOQUE}
                  >
                    <IconoDestello tamano={18} /> Leer horario
                  </motion.button>
                </div>
              )}
            </motion.div>
          )}

          {(paso === "revisar" || paso === "guardando") && (
            <motion.div
              key="revisar"
              className={estilos.cuerpo}
              initial={reducir ? false : { opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ duration: 0.25, ease: SALIDA }}
            >
              {filas.length === 0 ? (
                <div className={estilos.vacio}>
                  <strong>No encontramos clases en la foto</strong>
                  <span>Prueba con una foto más nítida y de frente, o registra tus clases a mano.</span>
                  <button type="button" className={estilos.secundario} onClick={() => setPaso("elegir")}>
                    Probar con otra foto
                  </button>
                </div>
              ) : (
                <>
                  <p className={estilos.resumen}>
                    Encontramos <strong>{filas.length}</strong> {filas.length === 1 ? "clase" : "clases"}. Corrige lo que haga falta y quita las que no
                    sean tuyas.
                    {descartadas > 0 && ` ${descartadas === 1 ? "Una no se pudo leer bien" : `${descartadas} no se pudieron leer bien`} y no aparece.`}
                  </p>
                  <motion.ul
                    className={estilos.filas}
                    initial={reducir ? false : "oculto"}
                    animate="visible"
                    variants={{ oculto: {}, visible: { transition: { staggerChildren: 0.05 } } }}
                  >
                    {filas.map((f) => (
                      <motion.li
                        key={f.clave}
                        className={f.incluir ? estilos.fila : `${estilos.fila} ${estilos.filaQuitada}`}
                        variants={{ oculto: { opacity: 0, y: 10 }, visible: { opacity: 1, y: 0, transition: { duration: 0.3, ease: SALIDA } } }}
                      >
                        <label className={estilos.incluir}>
                          <input
                            type="checkbox"
                            checked={f.incluir}
                            onChange={(e) => cambiarFila(f.clave, { incluir: e.target.checked })}
                            aria-label={`Incluir ${f.materia}`}
                          />
                        </label>
                        <input
                          className={estilos.materia}
                          value={f.materia}
                          maxLength={100}
                          onChange={(e) => cambiarFila(f.clave, { materia: e.target.value })}
                          aria-label="Materia"
                        />
                        <Selector
                          compacto
                          clase={estilos.dia}
                          valor={String(f.diaSemana)}
                          opciones={OPCIONES_DIA}
                          onCambio={(v) => cambiarFila(f.clave, { diaSemana: Number(v) })}
                          etiqueta="Día"
                        />
                        <SelectorHora
                          compacto
                          valor={f.horaInicio}
                          min={HORA_MINIMA}
                          max={HORA_MAXIMA}
                          onCambio={(v) => cambiarFila(f.clave, { horaInicio: v })}
                          etiqueta="Inicio"
                        />
                        <SelectorHora
                          compacto
                          valor={f.horaFin}
                          min={HORA_MINIMA}
                          max={HORA_MAXIMA}
                          onCambio={(v) => cambiarFila(f.clave, { horaFin: v })}
                          etiqueta="Fin"
                        />
                        <input
                          className={estilos.aula}
                          value={f.aula ?? ""}
                          maxLength={30}
                          placeholder="Aula"
                          onChange={(e) => cambiarFila(f.clave, { aula: e.target.value })}
                          aria-label="Aula"
                        />
                      </motion.li>
                    ))}
                  </motion.ul>
                  <div className={estilos.acciones}>
                    <button type="button" className={estilos.secundario} onClick={() => setPaso("elegir")} disabled={paso === "guardando"}>
                      Otra foto
                    </button>
                    <motion.button
                      type="button"
                      className={estilos.primario}
                      disabled={incluidas === 0 || paso === "guardando"}
                      onClick={() => void guardar()}
                      whileTap={{ scale: 0.97 }}
                      transition={RESORTE_TOQUE}
                    >
                      {paso === "guardando" ? "Guardando…" : `Guardar ${incluidas} ${incluidas === 1 ? "clase" : "clases"}`}
                    </motion.button>
                  </div>
                </>
              )}
            </motion.div>
          )}

          {paso === "asistencia" && (
            <motion.div
              key="asistencia"
              className={estilos.cuerpo}
              initial={reducir ? false : { opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ duration: 0.25, ease: SALIDA }}
            >
              <p className={estilos.resumen}>
                Guardamos todas tus clases, pero {grupos.length === 1 ? "estas se cruzan" : "algunas se cruzan"} en el horario. Marca las que{" "}
                <strong>sí asistes</strong>: las que dejes sin marcar no cuentan como tiempo ocupado y tus puentes se calculan con las demás.
              </p>
              {grupos.map((g) => (
                <fieldset key={g[0].id} className={estilos.grupo}>
                  <legend className={estilos.grupoTitulo}>
                    {DIAS[g[0].diaSemana]} · {g[0].horaInicio.slice(0, 5)}
                  </legend>
                  {g.map((o) => (
                    <label key={o.id} className={estilos.opcion}>
                      <input type="checkbox" checked={o.asiste} onChange={(e) => marcarAsistencia(o.id, e.target.checked)} />
                      <span>
                        {o.materia}
                        <small>
                          {o.horaInicio.slice(0, 5)} – {o.horaFin.slice(0, 5)}
                        </small>
                      </span>
                    </label>
                  ))}
                </fieldset>
              ))}
              <label className={estilos.autorizo}>
                <input type="checkbox" checked={autorizo} onChange={(e) => setAutorizo(e.target.checked)} />
                <span>Tengo autorización de mi carrera para cursar estas clases a la misma hora (opcional).</span>
              </label>
              <div className={estilos.acciones}>
                <button type="button" className={estilos.secundario} onClick={() => setPaso("resultado")} disabled={guardandoAsistencia}>
                  Asisto a todas
                </button>
                <motion.button
                  type="button"
                  className={estilos.primario}
                  disabled={guardandoAsistencia}
                  onClick={() => void guardarAsistencia()}
                  whileTap={{ scale: 0.97 }}
                  transition={RESORTE_TOQUE}
                >
                  {guardandoAsistencia ? "Guardando…" : "Guardar asistencia"}
                </motion.button>
              </div>
            </motion.div>
          )}

          {paso === "resultado" && resultado && (
            <motion.div
              key="resultado"
              className={estilos.cuerpo}
              initial={reducir ? false : { opacity: 0, scale: 0.97 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ type: "spring", duration: 0.45, bounce: 0.2 }}
            >
              <div className={estilos.exito}>
                <motion.span
                  className={estilos.exitoIcono}
                  initial={reducir ? false : { scale: 0.6, rotate: -20 }}
                  animate={{ scale: 1, rotate: 0 }}
                  transition={{ type: "spring", stiffness: 300, damping: 14 }}
                  aria-hidden="true"
                >
                  <IconoCheck tamano={26} />
                </motion.span>
                <strong>
                  {resultado.guardados.length === 0
                    ? "No se guardó ninguna clase"
                    : `¡${resultado.guardados.length} ${resultado.guardados.length === 1 ? "clase guardada" : "clases guardadas"}!`}
                </strong>
                <span>Tus puentes ya se recalcularon.</span>
              </div>
              {resultado.rechazados.length > 0 && (
                <div className={estilos.rechazos}>
                  <p>Estas no se guardaron; puedes agregarlas a mano:</p>
                  <ul>
                    {resultado.rechazados.map((r) => (
                      <li key={r.indice}>
                        <strong>{r.materia || "Sin materia"}</strong> — {r.motivo}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <div className={estilos.acciones}>
                <button type="button" className={estilos.primario} onClick={onCerrar}>
                  Listo
                </button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.section>
    </motion.div>
  );
}

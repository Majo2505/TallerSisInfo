"use client";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoCalendario, IconoCheck, IconoMas } from "@/components/app/Iconos";
import { Odometro, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import { Selector, SelectorHora } from "@/components/app/Selectores";
import {
  Bloque,
  BloqueInput,
  HORA_MAXIMA,
  HORA_MINIMA,
  cambiarAsistencia,
  columnasPorSolape,
  crearBloque,
  editarBloque,
  eliminarBloque,
  enChoque,
  listarHorario,
  minutosOcupados,
} from "@/lib/horario";
import { Puente, duracionMin, hhmm, listarPuentes } from "@/lib/puentes";
import { useSesion } from "@/lib/useSesion";
import ResumenSemanal from "@/components/horario/ResumenSemanal";
import PuentesDetectados from "@/components/puentes/puentesDetectados";
import HorarioPorDia from "@/components/horario/HorarioPorDia";
import FotoHorario from "@/components/horario/FotoHorario";
import RegistroHorario from "@/components/horario/RegistroHorario";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const PX_HORA = 56; // alto de una hora en la grilla

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};
const pad = (n: number) => String(n).padStart(2, "0");
const aHora = (min: number) => `${pad(Math.floor(min / 60))}:${pad(min % 60)}`;
const OPCIONES_DIA = [1, 2, 3, 4, 5, 6].map((d) => ({ valor: String(d), etiqueta: DIAS[d] }));

// Colores de los bloques de clase (paleta UCB). Cada materia usa siempre el mismo color, según su nombre.
const COLORES_BLOQUE = [
  { fondo: "#dcebf7", borde: "#11446d", texto: "#0b2a44" }, // azul institucional
  { fondo: "#d6eef9", borde: "#0088cc", texto: "#04405f" }, // celeste
  { fondo: "#d3f0f5", borde: "#009dc1", texto: "#04434f" }, // turquesa
  { fondo: "#fff3bf", borde: "#e0b400", texto: "#4a3b00" }, // amarillo
  { fondo: "#e3e8ef", borde: "#052845", texto: "#052845" }, // azul marino
  { fondo: "#e1eaf5", borde: "#2f6fb5", texto: "#0b2a5a" }, // azul medio
];
const colorBloque = (s: string) => {
  let h = 0;
  for (const c of s.toLowerCase().trim()) h = (h * 31 + c.charCodeAt(0)) % COLORES_BLOQUE.length;
  return COLORES_BLOQUE[h];
};

type ModalState = { bloque?: Bloque; base?: Partial<BloqueInput> } | null;
type ToastState = { texto: string; tipo: "ok" | "error" | "aviso" } | null;

export default function HorarioPage() {
  const router = useRouter();
  const { sesion, listo } = useSesion();
  const [bloques, setBloques] = useState<Bloque[]>([]);
  const [cargando, setCargando] = useState(true);
  const [errorCarga, setErrorCarga] = useState("");
  const [puentes, setPuentes] = useState<Puente[]>([]);
  const [errorPuentes, setErrorPuentes] = useState("");
  const [modal, setModal] = useState<ModalState>(null);
  const [fotoAbierta, setFotoAbierta] = useState(false);
  const [toast, setToast] = useState<ToastState>(null);
  const [intento, setIntento] = useState(0);
  const reducir = useReducedMotion();
  const [ahora, setAhora] = useState(() => new Date());

  // El marcador de "ahora" avanza solo, una vez por minuto.
  useEffect(() => {
    const t = setInterval(() => setAhora(new Date()), 60_000);
    return () => clearInterval(t);
  }, []);

  // Para reintentar o refrescar tras guardar: muestra el estado de carga y dispara una nueva petición.
  const cargar = useCallback(() => {
    setCargando(true);
    setErrorCarga("");
    setErrorPuentes("");
    setIntento((n) => n + 1);
  }, []);

  // Pantalla protegida: sin sesión (nunca inició, la cerró o el servidor devolvió 401) se va al login.
  useEffect(() => {
    if (listo && !sesion) router.replace("/login");
  }, [listo, sesion, router]);

  // Solo se piden los datos cuando ya hay una sesión restaurada (evita mandar un token vacío al refrescar).
  // Horario y puentes se piden juntos pero por separado: si falla uno, el otro se sigue mostrando.
  // El backend recalcula los puentes al guardar un bloque, así que recargar tras guardar los deja al día.
  useEffect(() => {
    if (!listo || !sesion) return;
    let activo = true; // evita actualizar el estado si el componente ya se desmontó o llegó otra petición
    Promise.allSettled([listarHorario(), listarPuentes()]).then(([horario, listaPuentes]) => {
      if (!activo) return;
      if (horario.status === "fulfilled") setBloques(horario.value);
      else setErrorCarga((horario.reason as Error).message);
      if (listaPuentes.status === "fulfilled") setPuentes(listaPuentes.value);
      else setErrorPuentes((listaPuentes.reason as Error).message);
      setCargando(false);
    });
    return () => { activo = false; };
  }, [listo, sesion, intento]);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 3500);
    return () => clearTimeout(t);
  }, [toast]);

  // Rango visible: 07:00–21:00 por defecto, se amplía si hay clases fuera de ese rango.
  const { desde, hasta } = useMemo(() => {
    let d = 7, h = 21;
    for (const b of bloques) {
      d = Math.min(d, Math.floor(aMin(b.horaInicio) / 60));
      h = Math.max(h, Math.ceil(aMin(b.horaFin) / 60));
    }
    return { desde: d, hasta: h };
  }, [bloques]);

  const horas = Array.from({ length: hasta - desde }, (_, i) => desde + i);
  const totalHoras = useMemo(() => minutosOcupados(bloques) / 60, [bloques]);
  const clasesAsistidas = useMemo(() => bloques.filter((b) => b.asiste).length, [bloques]);
  const columnasPorDia = useMemo(() => {
    const mapa = new Map<number, { columna: number; columnas: number }>();
    for (let d = 1; d <= 6; d++) {
      const delDia = bloques.filter((b) => b.diaSemana === d);
      columnasPorSolape(delDia).forEach((v, k) => mapa.set(k, v));
    }
    return mapa;
  }, [bloques]);

  const tiempoLibreMin = useMemo(() => puentes.reduce((acc, p) => acc + duracionMin(p), 0), [puentes]);

  const hoy = ahora.getDay();
  const minutoAhora = ahora.getHours() * 60 + ahora.getMinutes();
  const ahoraVisible = minutoAhora >= desde * 60 && minutoAhora < hasta * 60;

  const clickEnColumna = (e: React.MouseEvent<HTMLDivElement>, dia: number) => {
    if (e.target !== e.currentTarget) return;
    const y = e.clientY - e.currentTarget.getBoundingClientRect().top;
    // La hora tocada, ajustada al horario de clases (07:15 a 21:00) con una clase de 1 hora.
    const inicio = Math.min(Math.max((desde + Math.floor(y / PX_HORA)) * 60, aMin(HORA_MINIMA)), aMin(HORA_MAXIMA) - 60);
    setModal({ base: { diaSemana: dia, horaInicio: aHora(inicio), horaFin: aHora(inicio + 60) } });
  };

  const alGuardar = (texto: string, aviso?: string) => {
    setModal(null);
    setToast(aviso ? { texto: `${texto}. ${aviso}`, tipo: "aviso" } : { texto, tipo: "ok" });
    cargar();
  };

  const alternarAsistencia = async (b: Bloque) => {
    try {
      await cambiarAsistencia(b.id, !b.asiste);
      setToast({ texto: b.asiste ? `Ya no cuentas «${b.materia}» como ocupada` : `Asistes a «${b.materia}»`, tipo: "ok" });
      cargar();
    } catch (e) {
      setToast({ texto: (e as Error).message, tipo: "error" });
    }
  };

  if (!listo || !sesion) {
    return (
      <main className="hz">
        <div className="hz-skeleton" aria-busy="true" aria-label="Verificando sesión" />
      </main>
    );
  }

  return (
    <main className="hz">
      <motion.header
        className="hz-head"
        onPointerMove={seguirPuntero}
        initial={reducir ? false : { opacity: 0, y: 16, filter: "blur(4px)" }}
        animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
        transition={{ duration: 0.6, ease: SALIDA }}
      >
        <FondoVivo />
        <div>
          <h1>Mi horario académico</h1>
          <p className="hz-sub">
            {bloques.length === 0
              ? "Registra tus clases para que BREAKU detecte tus puentes."
              : `${totalHoras.toLocaleString("es", { maximumFractionDigits: 1 })} h de clase por semana. Toca una clase para editarla.`}
          </p>
        </div>
        <div className="hz-head-acciones">
          <motion.button
            type="button"
            className="hz-agregar"
            onClick={() => setModal({})}
            whileTap={{ scale: 0.96 }}
            transition={RESORTE_TOQUE}
          >
            <IconoMas tamano={18} />
            <span>Agregar clase</span>
          </motion.button>
        </div>
        <dl className="hz-cifras">
          <div className="hz-cifra">
            <dt>{clasesAsistidas === 1 ? "clase" : "clases"}</dt>
            <dd><Odometro valor={clasesAsistidas} /></dd>
          </div>
          <div className="hz-cifra">
            <dt>{puentes.length === 1 ? "puente" : "puentes"}</dt>
            <dd><Odometro valor={puentes.length} /></dd>
          </div>
          <div className="hz-cifra hz-cifra-libre">
            <dt>min libres</dt>
            <dd><Odometro valor={tiempoLibreMin} /></dd>
          </div>
        </dl>
      </motion.header>

      {errorCarga && (
        <div className="hz-banner" role="alert">
          <span>{errorCarga}</span>
          <button className="hz-btn" onClick={cargar}>Reintentar</button>
        </div>
      )}

      <div className="hz-layout">
        <div className="hz-principal">
          <RegistroHorario
            onManual={() => setModal({})}
            onFoto={() => setFotoAbierta(true)}
          />
          {cargando ? (
            <div className="hz-skeleton" aria-busy="true" aria-label="Cargando horario" />
          ) : (
            <>
              {bloques.length === 0 && !errorCarga && (
                <div className="hz-vacio">
                  <strong>Todavía no registraste clases</strong>
                  <span>Pulsa «Agregar clase» o haz clic sobre un día en la grilla.</span>
                </div>
              )}

              <div className="hz-vista-dia">
                <HorarioPorDia
                  bloques={bloques}
                  puentes={puentes}
                  colorDe={colorBloque}
                  onAsistencia={(b) => void alternarAsistencia(b)}
                  onAgregar={(dia) => setModal({ base: { diaSemana: dia, horaInicio: "08:00", horaFin: "09:30" } })}
                  onEditar={(b) => setModal({ bloque: b })}
                />
              </div>

              <div className="hz-vista-grilla">
              <div className="hz-scroll">
                <div className="hz-grid" style={{ ["--ph" as string]: `${PX_HORA}px` }}>
                  <div className="hz-corner" />
                  {[1, 2, 3, 4, 5, 6].map((d) => (
                    <div key={d} className={d === hoy ? "hz-dia-head hz-dia-hoy" : "hz-dia-head"}>
                      <span>{DIAS[d]}</span>
                    </div>
                  ))}

                  <div className="hz-horas">
                    {horas.map((h) => (
                      <div key={h} className="hz-hora">{pad(h)}:00</div>
                    ))}
                  </div>

                  {[1, 2, 3, 4, 5, 6].map((d) => (
                    <div
                      key={d}
                      className={d === hoy ? "hz-col hz-col-hoy" : "hz-col"}
                      style={{ height: horas.length * PX_HORA }}
                      onClick={(e) => clickEnColumna(e, d)}
                    >
                      {d === hoy && ahoraVisible && (
                        <div
                          className="hz-ahora"
                          style={{ top: ((minutoAhora - desde * 60) / 60) * PX_HORA }}
                          aria-hidden="true"
                        />
                      )}
                      {bloques.filter((b) => b.diaSemana === d).map((b, i) => {
                        const top = ((aMin(b.horaInicio) - desde * 60) / 60) * PX_HORA;
                        const alto = ((aMin(b.horaFin) - aMin(b.horaInicio)) / 60) * PX_HORA;
                        const color = colorBloque(b.materia);
                        const choca = enChoque(b);
                        const { columna, columnas } = columnasPorDia.get(b.id) ?? { columna: 0, columnas: 1 };
                        const clases = ["hz-bloque", choca ? "hz-bloque-choque" : "", b.asiste ? "" : "hz-bloque-ausente"].filter(Boolean).join(" ");
                        return (
                          <motion.button
                            key={b.id}
                            className={clases}
                            style={{
                              top, height: alto - 2,
                              left: `calc(4px + (100% - 8px) * ${columna} / ${columnas})`,
                              right: "auto",
                              width: `calc((100% - 8px) / ${columnas} - 2px)`,
                              background: color.fondo,
                              color: color.texto,
                              ["--acento" as string]: color.borde,
                            }}
                            onClick={() => setModal({ bloque: b })}
                            aria-label={`Editar ${b.materia}, ${DIAS[b.diaSemana]} de ${hhmm(b.horaInicio)} a ${hhmm(b.horaFin)}${choca ? ", coincide con otra clase" : ""}${b.asiste ? "" : ", no asistes"}`}
                            initial={reducir ? false : { opacity: 0, scale: 0.92, y: 8 }}
                            animate={{ opacity: 1, scale: 1, y: 0 }}
                            transition={{ duration: 0.45, ease: SALIDA, delay: 0.05 * d + 0.06 * i }}
                            whileHover={reducir ? undefined : { y: -2 }}
                            whileTap={{ scale: 0.97 }}
                          >
                            <strong>{b.materia}</strong>
                            <span>{hhmm(b.horaInicio)}–{hhmm(b.horaFin)}</span>
                            {choca && <span className="hz-etiqueta-choque">Coincide</span>}
                            {!b.asiste && <span className="hz-etiqueta-ausente">No asisto</span>}
                            {b.aula && alto > 100 && <span>{b.aula}</span>}
                          </motion.button>
                        );
                      })}
                    </div>
                  ))}
                </div>
              </div>
              </div>
            </>
          )}
        </div>

        <aside className="hz-lateral" aria-label="Puentes y resumen semanal">
          <PuentesDetectados puentes={puentes} cargando={cargando} error={errorPuentes} onReintentar={cargar} />
          <ResumenSemanal bloques={bloques} puentes={puentes} cargando={cargando} />
        </aside>
      </div>

      <AnimatePresence>
        {modal && <BloqueModal estado={modal} onClose={() => setModal(null)} onSaved={alGuardar} />}
      </AnimatePresence>
      <AnimatePresence>
        {fotoAbierta && (
          <FotoHorario
            onCerrar={() => setFotoAbierta(false)}
            onGuardado={(n) => {
              setToast({ texto: n === 1 ? "1 clase agregada desde la foto" : `${n} clases agregadas desde la foto`, tipo: "ok" });
              cargar();
            }}
            onActualizado={cargar}
          />
        )}
      </AnimatePresence>
      <AnimatePresence>
        {toast && (
          <motion.div
            key={toast.texto}
            className={`hz-toast hz-toast-${toast.tipo}`}
            role="status"
            initial={reducir ? false : { opacity: 0, y: 24, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 12, scale: 0.98 }}
            transition={{ type: "spring", stiffness: 420, damping: 30 }}
          >
            {toast.tipo === "ok" && <IconoCheck tamano={16} />}
            {toast.texto}
          </motion.div>
        )}
      </AnimatePresence>
    </main>
  );
}

/* ------------------------------ Modal ------------------------------ */

type Errores = Partial<Record<"materia" | "horaInicio" | "horaFin", string>>;

function BloqueModal({ estado, onClose, onSaved }: {
  estado: NonNullable<ModalState>;
  onClose: () => void;
  onSaved: (msg: string, aviso?: string) => void;
}) {
  const reducir = useReducedMotion();
  const editando = estado.bloque;
  const [form, setForm] = useState<BloqueInput>({
    materia: editando?.materia ?? "",
    diaSemana: editando?.diaSemana ?? estado.base?.diaSemana ?? 1,
    horaInicio: editando?.horaInicio ?? estado.base?.horaInicio ?? "",
    horaFin: editando?.horaFin ?? estado.base?.horaFin ?? "",
    aula: editando?.aula ?? "",
    asiste: editando?.asiste ?? true,
    autorizacionCarrera: editando?.autorizacionCarrera ?? false,
  });
  const [errores, setErrores] = useState<Errores>({});
  const [errorServidor, setErrorServidor] = useState("");
  const [guardando, setGuardando] = useState(false);
  const [confirmando, setConfirmando] = useState(false);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && !guardando && onClose();
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose, guardando]);

  const set = <K extends keyof BloqueInput>(k: K, v: BloqueInput[K]) => {
    setForm((f) => ({ ...f, [k]: v }));
    setErrorServidor("");
  };

  // Validación de formato en el cliente (las reglas reales las aplica el backend; un choque con otra clase no impide guardar, solo avisa).
  const validar = (): Errores => {
    const e: Errores = {};
    if (!form.materia.trim()) e.materia = "Escribe el nombre de la materia.";
    else if (form.materia.trim().length > 100) e.materia = "Máximo 100 caracteres.";
    if (!form.horaInicio) e.horaInicio = "Indica la hora de inicio.";
    else if (form.horaInicio < HORA_MINIMA) e.horaInicio = "Las clases empiezan desde las 07:15.";
    if (!form.horaFin) e.horaFin = "Indica la hora de fin.";
    else if (form.horaFin > HORA_MAXIMA) e.horaFin = "Las clases terminan, como muy tarde, a las 21:00.";
    else if (form.horaInicio && form.horaFin <= form.horaInicio) e.horaFin = "Tiene que ser posterior a la hora de inicio.";
    return e;
  };

  const guardar = async (ev: React.FormEvent) => {
    ev.preventDefault();
    const e = validar();
    setErrores(e);
    if (Object.keys(e).length) return;
    setGuardando(true);
    setErrorServidor("");
    try {
      const datos = { ...form, materia: form.materia.trim(), aula: form.aula?.trim() || null };
      const guardado = editando ? await editarBloque(editando.id, datos) : await crearBloque(datos);
      const aviso = guardado.advertencias.length > 0
        ? `${guardado.advertencias[0]} Marca a cuál asistes con la casilla «Asisto».`
        : undefined;
      onSaved(editando ? "Clase actualizada" : "Clase agregada", aviso);
    } catch (err) {
      setErrorServidor((err as Error).message);
      setGuardando(false);
    }
  };

  const borrar = async () => {
    if (!editando) return;
    setGuardando(true);
    try {
      await eliminarBloque(editando.id);
      onSaved("Clase eliminada");
    } catch (err) {
      setErrorServidor((err as Error).message);
      setGuardando(false);
      setConfirmando(false);
    }
  };

  return (
    <motion.div
      className="hz-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && !guardando && onClose()}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
    >
      <motion.form
        className="hz-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="hz-titulo"
        onSubmit={guardar}
        noValidate
        initial={reducir ? false : { opacity: 0, scale: 0.95, y: 24 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={reducir ? { opacity: 0 } : { opacity: 0, scale: 0.97, y: 12 }}
        transition={{ type: "spring", stiffness: 380, damping: 32 }}
      >
        <div className="hz-modal-cabecera">
          <span className="hz-modal-icono" aria-hidden="true"><IconoCalendario tamano={22} /></span>
          <h2 id="hz-titulo">{editando ? "Editar clase" : "Agregar clase"}</h2>
        </div>

        <label className="hz-campo">
          <span>Materia</span>
          <input
            autoFocus
            value={form.materia}
            maxLength={100}
            placeholder="Ej: Cálculo II"
            aria-invalid={!!errores.materia}
            onChange={(e) => set("materia", e.target.value)}
          />
          {errores.materia && <small className="hz-err">{errores.materia}</small>}
        </label>

        <div className="hz-campo">
          <span id="hz-dia">Día</span>
          <Selector
            valor={String(form.diaSemana)}
            opciones={OPCIONES_DIA}
            onCambio={(v) => set("diaSemana", Number(v))}
            etiqueta="Día"
          />
        </div>

        <div className="hz-fila">
          <div className="hz-campo">
            <span>Inicio</span>
            <SelectorHora
              valor={form.horaInicio}
              min={HORA_MINIMA}
              // Tiene que quedar al menos 5 minutos de clase antes de las 21:00.
              max={aHora(aMin(HORA_MAXIMA) - 5)}
              etiqueta="Hora de inicio"
              invalido={!!errores.horaInicio}
              onCambio={(v) => {
                set("horaInicio", v);
                if (errores.horaInicio) setErrores((x) => ({ ...x, horaInicio: undefined }));
              }}
            />
            {errores.horaInicio && <small className="hz-err">{errores.horaInicio}</small>}
          </div>
          <div className="hz-campo">
            <span>Fin</span>
            <SelectorHora
              valor={form.horaFin}
              min={form.horaInicio ? aHora(aMin(form.horaInicio) + 5) : aHora(aMin(HORA_MINIMA) + 5)}
              max={HORA_MAXIMA}
              etiqueta="Hora de fin"
              invalido={!!errores.horaFin}
              onCambio={(v) => {
                set("horaFin", v);
                if (errores.horaFin) setErrores((x) => ({ ...x, horaFin: undefined }));
              }}
            />
            {errores.horaFin && <small className="hz-err">{errores.horaFin}</small>}
          </div>
        </div>
        <p className="hz-nota">Las clases de la UCB van de 07:15 a 21:00.</p>

        <label className="hz-campo">
          <span>Aula <em>(opcional)</em></span>
          <input value={form.aula ?? ""} maxLength={30} placeholder="Ej: Bloque B – 204"
            onChange={(e) => set("aula", e.target.value)} />
        </label>

        <label className="hz-check">
          <input type="checkbox" checked={form.asiste ?? true} onChange={(e) => set("asiste", e.target.checked)} />
          <span>Asisto a esta clase</span>
        </label>

        {editando && form.asiste && editando.coincide.some((c) => c.asiste) && (
          <div className="hz-aviso" role="status">
            <p>
              Coincide con {editando.coincide.filter((c) => c.asiste).map((c) => `«${c.materia}» (${hhmm(c.horaInicio)}–${hhmm(c.horaFin)})`).join(", ")}.
              Si no asistes a una de las dos, desmarca «Asisto» en esa clase.
            </p>
            <label className="hz-check">
              <input
                type="checkbox"
                checked={form.autorizacionCarrera ?? false}
                onChange={(e) => set("autorizacionCarrera", e.target.checked)}
              />
              <span>Tengo autorización de mi carrera para cursarlas a la misma hora</span>
            </label>
          </div>
        )}

        {errorServidor && <div className="hz-banner" role="alert">{errorServidor}</div>}

        <div className="hz-acciones">
          {editando && !confirmando && (
            <button type="button" className="hz-btn hz-btn-peligro" disabled={guardando} onClick={() => setConfirmando(true)}>
              Eliminar
            </button>
          )}
          {editando && confirmando && (
            <button type="button" className="hz-btn hz-btn-peligro-fuerte" disabled={guardando} onClick={borrar}>
              {guardando ? "Eliminando…" : "Sí, eliminar"}
            </button>
          )}
          <span className="hz-espacio" />
          <button type="button" className="hz-btn" disabled={guardando} onClick={onClose}>Cancelar</button>
          <button type="submit" className="hz-btn hz-btn-primary" disabled={guardando}>
            {guardando && !confirmando ? "Guardando…" : editando ? "Guardar cambios" : "Agregar"}
          </button>
        </div>
      </motion.form>
    </motion.div>
  );
}

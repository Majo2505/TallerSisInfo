"use client";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Bloque, BloqueInput, crearBloque, editarBloque, eliminarBloque, listarHorario } from "@/lib/horario";
import { Puente, duracionMin, listarPuentes } from "@/lib/puentes";
import { useSesion } from "@/lib/useSesion";
import ResumenSemanal from "@/components/horario/ResumenSemanal";
import PuentesDetectados from "@/components/puentes/puentesDetectados";

const DIAS = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
const PX_HORA = 56; // alto de una hora en la grilla

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};
const pad = (n: number) => String(n).padStart(2, "0");

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
type ToastState = { texto: string; tipo: "ok" | "error" } | null;

export default function HorarioPage() {
  const router = useRouter();
  const { sesion, listo } = useSesion();
  const [bloques, setBloques] = useState<Bloque[]>([]);
  const [cargando, setCargando] = useState(true);
  const [errorCarga, setErrorCarga] = useState("");
  const [puentes, setPuentes] = useState<Puente[]>([]);
  const [errorPuentes, setErrorPuentes] = useState("");
  const [modal, setModal] = useState<ModalState>(null);
  const [toast, setToast] = useState<ToastState>(null);
  const [intento, setIntento] = useState(0);

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
  const totalHoras = useMemo(
    () => bloques.reduce((acc, b) => acc + (aMin(b.horaFin) - aMin(b.horaInicio)) / 60, 0),
    [bloques]
  );

  const tiempoLibreMin = useMemo(() => puentes.reduce((acc, p) => acc + duracionMin(p), 0), [puentes]);

  const clickEnColumna = (e: React.MouseEvent<HTMLDivElement>, dia: number) => {
    if (e.target !== e.currentTarget) return;
    const y = e.clientY - e.currentTarget.getBoundingClientRect().top;
    const h = Math.min(desde + Math.floor(y / PX_HORA), 22);
    setModal({ base: { diaSemana: dia, horaInicio: `${pad(h)}:00`, horaFin: `${pad(h + 1)}:00` } });
  };

  const alGuardar = (texto: string) => {
    setModal(null);
    setToast({ texto, tipo: "ok" });
    cargar();
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
      <header className="hz-head">
        <div>
          <h1>Mi horario académico</h1>
          <p className="hz-sub">
            {bloques.length === 0
              ? "Registra tus clases para que BREAKU detecte tus puentes."
              : `${bloques.length} ${bloques.length === 1 ? "clase" : "clases"} · ${totalHoras.toLocaleString("es", { maximumFractionDigits: 1 })} h por semana`}
          </p>
        </div>
        <button className="hz-btn hz-btn-primary" onClick={() => setModal({})}>+ Agregar clase</button>
      </header>

      {errorCarga && (
        <div className="hz-banner" role="alert">
          <span>{errorCarga}</span>
          <button className="hz-btn" onClick={cargar}>Reintentar</button>
        </div>
      )}

      <div className="hz-layout">
        <div className="hz-principal">
          {cargando ? (
            <div className="hz-skeleton" aria-busy="true" aria-label="Cargando horario" />
          ) : (
            <>
              {bloques.length === 0 && !errorCarga && (
                <div className="hz-vacio">
                  <strong>Aún no registraste clases</strong>
                  <span>Pulsa «Agregar clase» o haz clic sobre un día en la grilla.</span>
                </div>
              )}

              <div className="hz-scroll">
                <div className="hz-grid" style={{ ["--ph" as string]: `${PX_HORA}px` }}>
                  <div className="hz-corner" />
                  {[1, 2, 3, 4, 5, 6].map((d) => (
                    <div key={d} className="hz-dia-head">{DIAS[d]}</div>
                  ))}

                  <div className="hz-horas">
                    {horas.map((h) => (
                      <div key={h} className="hz-hora">{pad(h)}:00</div>
                    ))}
                  </div>

                  {[1, 2, 3, 4, 5, 6].map((d) => (
                    <div
                      key={d}
                      className="hz-col"
                      style={{ height: horas.length * PX_HORA }}
                      onClick={(e) => clickEnColumna(e, d)}
                    >
                      {bloques.filter((b) => b.diaSemana === d).map((b) => {
                        const top = ((aMin(b.horaInicio) - desde * 60) / 60) * PX_HORA;
                        const alto = ((aMin(b.horaFin) - aMin(b.horaInicio)) / 60) * PX_HORA;
                        const color = colorBloque(b.materia);
                        return (
                          <button
                            key={b.id}
                            className="hz-bloque"
                            style={{
                              top, height: alto - 2,
                              background: color.fondo,
                              borderLeftColor: color.borde,
                              color: color.texto,
                            }}
                            onClick={() => setModal({ bloque: b })}
                            aria-label={`Editar ${b.materia}, ${DIAS[b.diaSemana]} de ${b.horaInicio} a ${b.horaFin}`}
                          >
                            <strong>{b.materia}</strong>
                            <span>{b.horaInicio}–{b.horaFin}</span>
                            {b.aula && alto > 60 && <span>{b.aula}</span>}
                          </button>
                        );
                      })}
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}
        </div>

        <aside className="hz-lateral" aria-label="Puentes y resumen semanal">
          <PuentesDetectados puentes={puentes} cargando={cargando} error={errorPuentes} onReintentar={cargar} />
          <ResumenSemanal clases={bloques.length} puentes={puentes.length} tiempoLibreMin={tiempoLibreMin} cargando={cargando} />
        </aside>
      </div>

      {modal && <BloqueModal estado={modal} onClose={() => setModal(null)} onSaved={alGuardar} />}
      {toast && <div className={`hz-toast hz-toast-${toast.tipo}`} role="status">{toast.texto}</div>}
    </main>
  );
}

/* ------------------------------ Modal ------------------------------ */

type Errores = Partial<Record<"materia" | "horaInicio" | "horaFin", string>>;

function BloqueModal({ estado, onClose, onSaved }: {
  estado: NonNullable<ModalState>;
  onClose: () => void;
  onSaved: (msg: string) => void;
}) {
  const editando = estado.bloque;
  const [form, setForm] = useState<BloqueInput>({
    materia: editando?.materia ?? "",
    diaSemana: editando?.diaSemana ?? estado.base?.diaSemana ?? 1,
    horaInicio: editando?.horaInicio ?? estado.base?.horaInicio ?? "",
    horaFin: editando?.horaFin ?? estado.base?.horaFin ?? "",
    aula: editando?.aula ?? "",
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

  // Validación de formato en el cliente (las reglas reales, incluido el solapamiento, las aplica el backend).
  const validar = (): Errores => {
    const e: Errores = {};
    if (!form.materia.trim()) e.materia = "Escribe el nombre de la materia.";
    else if (form.materia.trim().length > 100) e.materia = "Máximo 100 caracteres.";
    if (!form.horaInicio) e.horaInicio = "Indica la hora de inicio.";
    if (!form.horaFin) e.horaFin = "Indica la hora de fin.";
    if (form.horaInicio && form.horaFin && form.horaFin <= form.horaInicio)
      e.horaFin = "Debe ser posterior a la hora de inicio.";
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
      if (editando) await editarBloque(editando.id, datos);
      else await crearBloque(datos);
      onSaved(editando ? "Clase actualizada" : "Clase agregada");
    } catch (err) {
      setErrorServidor((err as Error).message); // incluye el mensaje de solapamiento
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
    <div className="hz-overlay" onMouseDown={(e) => e.target === e.currentTarget && !guardando && onClose()}>
      <form className="hz-modal" role="dialog" aria-modal="true" aria-labelledby="hz-titulo" onSubmit={guardar} noValidate>
        <h2 id="hz-titulo">{editando ? "Editar clase" : "Agregar clase"}</h2>

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

        <label className="hz-campo">
          <span>Día</span>
          <select value={form.diaSemana} onChange={(e) => set("diaSemana", Number(e.target.value))}>
            {[1, 2, 3, 4, 5, 6].map((d) => <option key={d} value={d}>{DIAS[d]}</option>)}
          </select>
        </label>

        <div className="hz-fila">
          <label className="hz-campo">
            <span>Inicio</span>
            <input type="time" value={form.horaInicio} aria-invalid={!!errores.horaInicio}
              onChange={(e) => set("horaInicio", e.target.value)} />
            {errores.horaInicio && <small className="hz-err">{errores.horaInicio}</small>}
          </label>
          <label className="hz-campo">
            <span>Fin</span>
            <input type="time" value={form.horaFin} aria-invalid={!!errores.horaFin}
              onChange={(e) => set("horaFin", e.target.value)} />
            {errores.horaFin && <small className="hz-err">{errores.horaFin}</small>}
          </label>
        </div>

        <label className="hz-campo">
          <span>Aula <em>(opcional)</em></span>
          <input value={form.aula ?? ""} maxLength={30} placeholder="Ej: Bloque B – 204"
            onChange={(e) => set("aula", e.target.value)} />
        </label>

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
      </form>
    </div>
  );
}

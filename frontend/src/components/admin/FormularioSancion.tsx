"use client";

import { useEffect, useId, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { motion, useReducedMotion } from "motion/react";
import { sancionarUsuario } from "@/lib/usuarios";
import {
  ETIQUETA_TIPO,
  LARGO_MAXIMO_MOTIVO,
  TipoSancion,
  ayudaFechaFin,
  confirmacionBajaValida,
  hastaParaApi,
  validarSancion,
} from "@/lib/reglasUsuarios";
import estilos from "./Usuarios.module.css";

const sinSuscripcion = () => () => {};

type Props = {
  usuario: { id: number; nombre: string };
  tipo: TipoSancion;
  /** Muestra un selector de tipo (al sancionar desde un reporte, donde todavía no se eligió). */
  elegirTipo?: boolean;
  /** Reporte que originó la sanción, si se abrió desde uno. */
  reporteId?: number | null;
  onCerrar: () => void;
  onAplicada: (mensaje: string) => void;
};

const TITULO: Record<TipoSancion, string> = {
  ADVERTENCIA: "Advertir a",
  SUSPENSION: "Suspender a",
  BAJA: "Dar de baja a",
};

/** Un valor para el atributo min de datetime-local (hora local, sin segundos). */
function ahoraLocal(): string {
  const d = new Date(Date.now() + 60_000);
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
}

/** Formulario de sanción. Motivo obligatorio; fecha de fin solo en la suspensión; la baja pide confirmación explícita. */
export default function FormularioSancion({ usuario, tipo: tipoInicial, elegirTipo = false, reporteId = null, onCerrar, onAplicada }: Props) {
  const reducir = useReducedMotion();
  const enCliente = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  const idTitulo = useId();
  const [tipo, setTipo] = useState<TipoSancion>(tipoInicial);
  const [motivo, setMotivo] = useState("");
  const [hasta, setHasta] = useState("");
  const [confirmacion, setConfirmacion] = useState("");
  const [intentado, setIntentado] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const alTeclear = (e: KeyboardEvent) => e.key === "Escape" && !enviando && onCerrar();
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [enviando, onCerrar]);

  const problema = validarSancion(tipo, motivo, hasta, new Date());
  const bajaConfirmada = tipo !== "BAJA" || confirmacionBajaValida(confirmacion, usuario.nombre);

  async function enviar(evento: React.FormEvent) {
    evento.preventDefault();
    setIntentado(true);
    setError("");
    if (problema || !bajaConfirmada) return;
    setEnviando(true);
    try {
      const r = await sancionarUsuario(usuario.id, tipo, motivo, tipo === "SUSPENSION" ? hastaParaApi(hasta) : null, reporteId);
      onAplicada(
        tipo === "BAJA"
          ? `${usuario.nombre} fue dado de baja.`
          : `${ETIQUETA_TIPO[tipo]} aplicada a ${usuario.nombre} (${r.advertencias} ${r.advertencias === 1 ? "advertencia vigente" : "advertencias vigentes"}).`,
      );
    } catch (e) {
      // El backend explica el rechazo (sancionarse a uno mismo, a otro admin, suspensión ya vigente…): se muestra tal cual.
      setError((e as Error).message);
      setEnviando(false);
    }
  }

  if (!enCliente) return null;

  const errorMotivo = intentado && problema;

  return createPortal(
    <motion.div
      className="hz-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && !enviando && onCerrar()}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.2 }}
    >
      <motion.form
        className={`hz-modal ${estilos.modalSancion}`}
        role={tipo === "BAJA" ? "alertdialog" : "dialog"}
        aria-modal="true"
        aria-labelledby={idTitulo}
        onSubmit={(e) => void enviar(e)}
        noValidate
        initial={reducir ? false : { opacity: 0, scale: 0.95, y: 24 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 380, damping: 32 }}
      >
        <h2 id={idTitulo}>
          {TITULO[tipo]} {usuario.nombre}
        </h2>

        {reporteId !== null && (
          <p className="hz-nota" style={{ margin: 0 }}>
            Se registra el reporte n.º {reporteId} como origen de la sanción.
          </p>
        )}

        {elegirTipo && (
          <fieldset className={estilos.tipos}>
            <legend>Tipo de sanción</legend>
            {(["ADVERTENCIA", "SUSPENSION", "BAJA"] as const).map((t) => (
              <label key={t} className={estilos.tipoOpcion}>
                <input type="radio" name={`${idTitulo}-tipo`} checked={tipo === t} onChange={() => setTipo(t)} />
                {ETIQUETA_TIPO[t]}
              </label>
            ))}
          </fieldset>
        )}

        {tipo === "BAJA" && (
          <div className={estilos.advertenciaBaja} role="note">
            <strong>La baja es definitiva y no se puede deshacer.</strong>
            <ul>
              <li>La cuenta queda anonimizada y bloqueada: no podrá volver a entrar ni registrarse de nuevo.</li>
              <li>Se borran sus eventos y sus participaciones. Sus comunidades se eliminan o pasan a otro participante.</li>
              <li>El motivo queda guardado en el historial de sanciones.</li>
            </ul>
          </div>
        )}

        <label className="hz-campo">
          <span>
            Motivo <em>(obligatorio)</em>
          </span>
          <textarea
            className={estilos.textoArea}
            rows={4}
            value={motivo}
            maxLength={LARGO_MAXIMO_MOTIVO}
            onChange={(e) => setMotivo(e.target.value)}
            aria-invalid={errorMotivo ? true : undefined}
            aria-describedby={`${idTitulo}-motivo`}
            autoFocus
          />
          <span className={estilos.contador}>
            {motivo.length}/{LARGO_MAXIMO_MOTIVO}
          </span>
        </label>
        {errorMotivo && (
          <p id={`${idTitulo}-motivo`} className="hz-err" role="alert">
            {problema}
          </p>
        )}

        {tipo === "SUSPENSION" && (
          <label className="hz-campo">
            <span>
              Fin de la suspensión <em>(opcional)</em>
            </span>
            <input type="datetime-local" value={hasta} min={ahoraLocal()} onChange={(e) => setHasta(e.target.value)} aria-describedby={`${idTitulo}-hasta`} />
            <span id={`${idTitulo}-hasta`} className="hz-nota">
              {ayudaFechaFin(hasta)}
            </span>
          </label>
        )}

        {tipo === "BAJA" && (
          <label className="hz-campo">
            <span>
              Para confirmar, escribe <strong>BAJA</strong> o el nombre del usuario
            </span>
            <input
              value={confirmacion}
              onChange={(e) => setConfirmacion(e.target.value)}
              autoComplete="off"
              aria-invalid={intentado && !bajaConfirmada ? true : undefined}
            />
            {intentado && !bajaConfirmada && (
              <span className="hz-err" role="alert">
                Escribe BAJA o «{usuario.nombre}» para confirmar.
              </span>
            )}
          </label>
        )}

        {error && (
          <motion.p key={error} className={estilos.error} role="alert" animate={reducir ? undefined : { x: [0, -8, 8, -5, 5, 0] }} transition={{ duration: 0.4 }}>
            {error}
          </motion.p>
        )}

        <div className="hz-acciones">
          <span className="hz-espacio" />
          <button type="button" className="hz-btn" disabled={enviando} onClick={onCerrar}>
            Cancelar
          </button>
          <button type="submit" className={tipo === "BAJA" ? "hz-btn hz-btn-peligro-fuerte" : "hz-btn hz-btn-primary"} disabled={enviando}>
            {enviando ? "Aplicando…" : tipo === "BAJA" ? "Dar de baja definitivamente" : tipo === "SUSPENSION" ? "Suspender" : "Advertir"}
          </button>
        </div>
      </motion.form>
    </motion.div>,
    document.body,
  );
}

"use client";

import { useEffect, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { motion, useReducedMotion } from "motion/react";
import { ErrorApi } from "@/lib/api";
import { LugarAdmin, eliminarLugarAdmin } from "@/lib/admin";
import estilos from "./FormularioLugar.module.css";

const sinSuscripcion = () => () => {};

type Props = {
  lugar: LugarAdmin;
  onCerrar: () => void;
  onEliminado: (mensaje: string) => void;
  /** Si ya se usó, no se puede eliminar: se ofrece desactivarlo. */
  onDesactivar: (lugar: LugarAdmin) => void;
};

/** Confirmación antes de eliminar un lugar. Un lugar que ya se usó no se elimina (rompería el historial): se desactiva. */
export default function EliminarLugar({ lugar, onCerrar, onEliminado, onDesactivar }: Props) {
  const reducir = useReducedMotion();
  const enCliente = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  const [eliminando, setEliminando] = useState(false);
  const [error, setError] = useState("");
  const [enUso, setEnUso] = useState(false);

  useEffect(() => {
    const alTeclear = (e: KeyboardEvent) => e.key === "Escape" && !eliminando && onCerrar();
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [eliminando, onCerrar]);

  async function eliminar() {
    setEliminando(true);
    setError("");
    try {
      await eliminarLugarAdmin(lugar.id);
      onEliminado(`${lugar.nombre} se eliminó del catálogo.`);
    } catch (e) {
      setError((e as Error).message);
      setEnUso(e instanceof ErrorApi && e.codigo === "EN_USO");
    } finally {
      setEliminando(false);
    }
  }

  if (!enCliente) return null;

  return createPortal(
    <motion.div
      className="hz-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && !eliminando && onCerrar()}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
    >
      <motion.div
        className="hz-modal"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="titulo-eliminar"
        aria-describedby="texto-eliminar"
        initial={reducir ? false : { opacity: 0, scale: 0.95, y: 24 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={reducir ? { opacity: 0 } : { opacity: 0, scale: 0.97, y: 12 }}
        transition={{ type: "spring", stiffness: 380, damping: 32 }}
      >
        <h2 id="titulo-eliminar">¿Eliminar «{lugar.nombre}»?</h2>
        <p id="texto-eliminar" className={estilos.ayuda}>
          Se borra del catálogo y no se puede deshacer (queda registrado en el historial). Si solo quieres que no se recomiende
          por un tiempo, desactívalo.
        </p>
        {error && (
          <motion.p key={error} className={estilos.error} role="alert" animate={reducir ? undefined : { x: [0, -8, 8, -5, 5, 0] }} transition={{ duration: 0.4 }}>
            {error}
          </motion.p>
        )}
        <div className="hz-acciones">
          <span className="hz-espacio" />
          <button type="button" className="hz-btn" disabled={eliminando} onClick={onCerrar}>
            Cancelar
          </button>
          {enUso ? (
            <button type="button" className="hz-btn hz-btn-primary" onClick={() => onDesactivar(lugar)} autoFocus>
              Desactivar en su lugar
            </button>
          ) : (
            <button type="button" className="hz-btn hz-btn-peligro-fuerte" disabled={eliminando} onClick={() => void eliminar()} autoFocus>
              {eliminando ? "Eliminando…" : "Eliminar"}
            </button>
          )}
        </div>
      </motion.div>
    </motion.div>,
    document.body,
  );
}

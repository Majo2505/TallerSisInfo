"use client";

import { useEffect, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { motion, useReducedMotion } from "motion/react";
import { LugarAdmin } from "@/lib/admin";
import { ETIQUETA_CONFIRMAR_DESVALIDAR, TEXTO_CONFIRMAR_DESVALIDAR, tituloConfirmarDesvalidar } from "@/lib/reglasAdmin";
import estilos from "./FormularioLugar.module.css";

const sinSuscripcion = () => () => {};

type Props = {
  lugar: LugarAdmin;
  onCerrar: () => void;
  /** Quita la validación; el padre cierra el diálogo cuando termina (bien o mal). */
  onConfirmar: (lugar: LugarAdmin) => Promise<void>;
};

/** HU16: confirmación antes de quitar la validación de un lugar. Validar no pide confirmación. */
export default function QuitarValidacion({ lugar, onCerrar, onConfirmar }: Props) {
  const reducir = useReducedMotion();
  const enCliente = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  const [procesando, setProcesando] = useState(false);

  useEffect(() => {
    const alTeclear = (e: KeyboardEvent) => e.key === "Escape" && !procesando && onCerrar();
    window.addEventListener("keydown", alTeclear);
    return () => window.removeEventListener("keydown", alTeclear);
  }, [procesando, onCerrar]);

  async function confirmar() {
    if (procesando) return;
    setProcesando(true);
    try {
      await onConfirmar(lugar);
    } finally {
      setProcesando(false);
    }
  }

  if (!enCliente) return null;

  return createPortal(
    <motion.div
      className="hz-overlay"
      onMouseDown={(e) => e.target === e.currentTarget && !procesando && onCerrar()}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
    >
      <motion.div
        className="hz-modal"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="titulo-quitar-validacion"
        aria-describedby="texto-quitar-validacion"
        initial={reducir ? false : { opacity: 0, scale: 0.95, y: 24 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={reducir ? { opacity: 0 } : { opacity: 0, scale: 0.97, y: 12 }}
        transition={{ type: "spring", stiffness: 380, damping: 32 }}
      >
        <h2 id="titulo-quitar-validacion">{tituloConfirmarDesvalidar(lugar.nombre)}</h2>
        <p id="texto-quitar-validacion" className={estilos.ayuda}>
          {TEXTO_CONFIRMAR_DESVALIDAR}
        </p>
        <div className="hz-acciones">
          <span className="hz-espacio" />
          <button type="button" className="hz-btn" disabled={procesando} onClick={onCerrar} autoFocus>
            Cancelar
          </button>
          <button type="button" className="hz-btn hz-btn-peligro-fuerte" disabled={procesando} onClick={() => void confirmar()}>
            {procesando ? "Quitando…" : ETIQUETA_CONFIRMAR_DESVALIDAR}
          </button>
        </div>
      </motion.div>
    </motion.div>,
    document.body,
  );
}

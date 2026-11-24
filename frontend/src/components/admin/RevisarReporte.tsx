"use client";

import { useId, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { SALIDA } from "@/components/app/Movimiento";
import { LARGO_MAXIMO_RESPUESTA } from "@/lib/admin";
import { validarMotivoModeracion } from "@/lib/reglasEventos";
import estilos from "./RevisarReporte.module.css";

type Decision = "RESUELTO" | "DESCARTADO";

type Props = {
  /** Para los aria-label: «el reporte de …». */
  objeto: string;
  ocupado: boolean;
  onRevisar: (decision: Decision, respuesta: string) => Promise<boolean>;
  claseResolver: string;
  claseDescartar: string;
  /**
   * HU35: solo en reportes de una comunidad. Quita la comunidad con el motivo (obligatorio) y deja el reporte resuelto.
   * No sanciona a quien la creó: «Sancionar» sigue aparte.
   */
  onQuitarComunidad?: (motivo: string) => Promise<boolean>;
};

/**
 * HU27: resolver o descartar un reporte pendiente con una respuesta opcional para el estudiante (hasta 500 caracteres).
 * La respuesta queda oculta tras «Responder al estudiante» para que la bandeja siga siendo fácil de recorrer.
 */
export default function RevisarReporte({ objeto, ocupado, onRevisar, claseResolver, claseDescartar, onQuitarComunidad }: Props) {
  const reducir = useReducedMotion();
  const idCampo = useId();
  const idMotivo = useId();
  const [conRespuesta, setConRespuesta] = useState(false);
  const [respuesta, setRespuesta] = useState("");
  const [quitando, setQuitando] = useState(false);
  const [motivo, setMotivo] = useState("");
  const [errorMotivo, setErrorMotivo] = useState("");
  const larga = respuesta.trim().length > LARGO_MAXIMO_RESPUESTA;

  async function quitar() {
    if (!onQuitarComunidad) return;
    const falta = validarMotivoModeracion(motivo, "quitar");
    if (falta) return setErrorMotivo(falta);
    setErrorMotivo("");
    if (await onQuitarComunidad(motivo.trim())) {
      setMotivo("");
      setQuitando(false);
    }
  }

  async function revisar(decision: Decision) {
    if (larga) return;
    const ok = await onRevisar(decision, conRespuesta ? respuesta : "");
    if (ok) setRespuesta("");
  }

  return (
    <div className={estilos.contenedor}>
      <AnimatePresence initial={false}>
        {conRespuesta && (
          <motion.label
            className={estilos.campo}
            htmlFor={idCampo}
            initial={reducir ? false : { opacity: 0, y: -6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={reducir ? { opacity: 0 } : { opacity: 0, y: -6 }}
            transition={{ duration: 0.2, ease: SALIDA }}
          >
            <span>
              Respuesta para el estudiante <em>(opcional)</em>
            </span>
            <textarea
              id={idCampo}
              rows={3}
              value={respuesta}
              maxLength={LARGO_MAXIMO_RESPUESTA}
              placeholder="Ej: Gracias por avisar, ya corregimos el horario."
              aria-invalid={larga}
              autoFocus
              onChange={(e) => setRespuesta(e.target.value)}
            />
            <span className={estilos.pie}>
              <small>La ve en Mis reportes y, resumida, en su campanita.</small>
              <small className={respuesta.length >= LARGO_MAXIMO_RESPUESTA * 0.9 ? estilos.limite : estilos.contador}>
                {respuesta.length}/{LARGO_MAXIMO_RESPUESTA}
              </small>
            </span>
          </motion.label>
        )}
      </AnimatePresence>
      <span className={estilos.acciones}>
        <button
          type="button"
          className={claseResolver}
          disabled={ocupado || larga}
          onClick={() => void revisar("RESUELTO")}
          aria-label={`Marcar como resuelto el reporte de ${objeto}`}
        >
          Resolver
        </button>
        <button
          type="button"
          className={claseDescartar}
          disabled={ocupado || larga}
          onClick={() => void revisar("DESCARTADO")}
          aria-label={`Descartar el reporte de ${objeto}`}
        >
          Descartar
        </button>
        {!conRespuesta && (
          <button type="button" className={estilos.responder} onClick={() => setConRespuesta(true)}>
            Responder al estudiante
          </button>
        )}
        {onQuitarComunidad && !quitando && (
          <button type="button" className={claseDescartar} disabled={ocupado} onClick={() => setQuitando(true)}>
            Quitar comunidad
          </button>
        )}
      </span>
      {onQuitarComunidad && quitando && (
        <div className={estilos.campo}>
          <label htmlFor={idMotivo}>
            Motivo para quitar la comunidad <em>(obligatorio; le llega a quien la creó y a quienes iban)</em>
          </label>
          <textarea
            id={idMotivo}
            rows={3}
            maxLength={500}
            value={motivo}
            autoFocus
            aria-invalid={!!errorMotivo}
            onChange={(e) => {
              setMotivo(e.target.value);
              setErrorMotivo("");
            }}
          />
          <small>Queda oculta sin borrarse, el reporte se marca como resuelto y no sanciona a quien la creó.</small>
          {errorMotivo && <small role="alert">{errorMotivo}</small>}
          <span className={estilos.acciones}>
            <button type="button" className={claseResolver} disabled={ocupado} onClick={() => setQuitando(false)}>
              Cancelar
            </button>
            <button type="button" className={claseDescartar} disabled={ocupado} onClick={() => void quitar()}>
              Confirmar y quitar
            </button>
          </span>
        </div>
      )}
    </div>
  );
}

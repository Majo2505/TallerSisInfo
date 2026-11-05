"use client";

import { useState } from "react";
import { motion, useReducedMotion } from "motion/react";
import { ErrorApi } from "@/lib/api";
import { ComunidadActiva, RespuestaAsistencia, responderAsistencia } from "@/lib/comunidades";
import estilos from "./AsistenciaComunidad.module.css";

type Props = {
  comunidad: ComunidadActiva;
  onRespondida: (respuesta: RespuestaAsistencia) => void;
  onDesactualizada: () => void;
  onVerQuienes: () => void;
};

const CODIGOS_DESACTUALIZADA = new Set(["COMPLETA", "NO_ENCONTRADO"]);

export default function AsistenciaComunidad({ comunidad: c, onRespondida, onDesactualizada, onVerQuienes }: Props) {
  const reducir = useReducedMotion();
  const [enviando, setEnviando] = useState<boolean | null>(null);
  const [error, setError] = useState("");

  const llena = c.confirmados >= c.maxParticipantes;
  const ocupado = enviando !== null;

  async function responder(asisto: boolean) {
    if (ocupado) return;
    setEnviando(asisto);
    setError("");
    try {
      onRespondida(await responderAsistencia(c.id, asisto));
    } catch (e) {
      setError(e instanceof Error ? e.message : "No pudimos guardar tu respuesta. Inténtalo de nuevo.");
      if (e instanceof ErrorApi && e.codigo && CODIGOS_DESACTUALIZADA.has(e.codigo)) onDesactualizada();
    } finally {
      setEnviando(null);
    }
  }

  const verQuienes = (
    <button type="button" className={`hz-btn ${estilos.boton}`} onClick={onVerQuienes}>
      Quiénes van
    </button>
  );

  const confirmar = (etiqueta: string) => (
    <button
      type="button"
      className={`hz-btn hz-btn-primary ${estilos.boton}`}
      onClick={() => void responder(true)}
      disabled={ocupado || llena}
      aria-busy={enviando === true}
    >
      {enviando === true ? "Guardando…" : llena ? "Sin lugares" : etiqueta}
    </button>
  );

  const rechazar = (etiqueta: string) => (
    <button
      type="button"
      className={`hz-btn ${estilos.boton}`}
      onClick={() => void responder(false)}
      disabled={ocupado}
      aria-busy={enviando === false}
    >
      {enviando === false ? "Guardando…" : etiqueta}
    </button>
  );

  let acciones: React.ReactNode;
  if (c.esMia) acciones = verQuienes;
  else if (c.miEstado === "PENDIENTE")
    acciones = (
      <>
        {rechazar("No voy")}
        {confirmar("Voy")}
      </>
    );
  else if (c.miEstado === "CONFIRMADO")
    acciones = (
      <>
        {verQuienes}
        {rechazar("Ya no voy")}
      </>
    );
  else if (c.miEstado === "RECHAZADO")
    acciones = (
      <>
        {verQuienes}
        {confirmar("Al final sí voy")}
      </>
    );
  else if (!llena) acciones = confirmar("Unirme");
  else return null;

  return (
    <div className={estilos.contenedor}>
      {c.miEstado === "PENDIENTE" && <p className={estilos.pregunta}>¿Vas a ir?</p>}
      <div className={estilos.acciones}>{acciones}</div>
      {error && (
        <motion.p
          key={error}
          className={estilos.error}
          role="alert"
          animate={reducir ? undefined : { x: [0, -8, 8, -5, 5, 0] }}
          transition={{ duration: 0.4 }}
        >
          {error}
        </motion.p>
      )}
    </div>
  );
}

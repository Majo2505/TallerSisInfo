"use client";

import Link from "next/link";
import { motion, useReducedMotion } from "motion/react";
import { useEffect, useState } from "react";
import { IconoFlechaDerecha, IconoPin } from "@/components/app/Iconos";
import { CLAVE_AVISO_UBICACION, debeMostrarAvisoUbicacion } from "@/lib/reglasUbicacion";
import { EstadoUbicacion, obtenerEstadoUbicacion } from "@/lib/ubicacion";
import estilos from "./AvisoUbicacionInicial.module.css";

export default function AvisoUbicacionInicial() {
  const reducir = useReducedMotion();
  const [estado, setEstado] = useState<EstadoUbicacion | null>(null);
  const [visto, setVisto] = useState(() => {
    if (typeof window === "undefined") return true;
    try {
      return window.localStorage.getItem(CLAVE_AVISO_UBICACION) === "1";
    } catch {
      return false;
    }
  });

  useEffect(() => {
    let activo = true;
    obtenerEstadoUbicacion()
      .then((nuevo) => {
        if (activo) setEstado(nuevo);
      })
      .catch(() => undefined);
    return () => {
      activo = false;
    };
  }, []);

  function descartar() {
    setVisto(true);
    try {
      window.localStorage.setItem(CLAVE_AVISO_UBICACION, "1");
    } catch {
      setVisto(true);
    }
  }

  if (!debeMostrarAvisoUbicacion(estado, visto)) return null;

  return (
    <motion.aside
      className={estilos.aviso}
      role="status"
      aria-label="Configurar la ubicación"
      initial={reducir ? false : { opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35 }}
    >
      <div className={estilos.arte} aria-hidden="true">
        <svg viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="34" cy="34" r="25" fill="currentColor" fillOpacity="0.1" />
          <path
            d="M34 10c-9.4 0-17 7.6-17 17 0 12 17 26 17 26s17-14 17-26c0-9.4-7.6-17-17-17Z"
            stroke="currentColor"
            strokeWidth="3"
            fill="white"
            fillOpacity="0.6"
          />
          <circle cx="34" cy="27" r="6" fill="currentColor" fillOpacity="0.35" />
          <circle cx="58" cy="55" r="15" fill="#fff" stroke="currentColor" strokeWidth="3" />
          <path d="m52 55 4 4 8-9" stroke="currentColor" strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>
      <div className={estilos.contenido}>
        <strong><IconoPin tamano={16} /> Recomendaciones más cerca de ti</strong>
        <p>
          Si nos dices tu campus, podemos sugerirte lugares y eventos que te quedan a mano. Solo guardamos la zona, nunca tus
          coordenadas, y puedes revocar el permiso cuando quieras desde tu perfil.
        </p>
        <div className={estilos.acciones}>
          <Link href="/configuracion/ubicacion" className={estilos.primario}>
            Elegir cómo compartir <IconoFlechaDerecha tamano={14} />
          </Link>
          <button type="button" className={estilos.omitir} onClick={descartar}>Ahora no</button>
        </div>
        <p className={estilos.ayuda}>También puedes elegir tu campus a mano si no quieres usar la ubicación del dispositivo.</p>
      </div>
    </motion.aside>
  );
}

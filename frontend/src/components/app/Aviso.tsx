"use client";

import { useCallback, useEffect, useState } from "react";
import estilos from "./Aviso.module.css";

export function useAviso() {
  const [texto, setTexto] = useState("");

  useEffect(() => {
    if (!texto) return;
    const t = setTimeout(() => setTexto(""), 3500);
    return () => clearTimeout(t);
  }, [texto]);

  const mostrar = useCallback((nuevo: string) => setTexto(nuevo), []);
  return { texto, mostrar };
}

export function AvisoFlotante({ texto }: { texto: string }) {
  if (!texto) return null;
  return (
    <div className={estilos.aviso} role="status">
      {texto}
    </div>
  );
}

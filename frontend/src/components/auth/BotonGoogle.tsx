"use client";

import { useEffect, useRef } from "react";
import { GOOGLE_CLIENT_ID } from "@/lib/config";

type RespuestaGoogle = { credential?: string };

type ApiGoogle = {
  accounts: {
    id: {
      initialize: (opciones: { client_id: string; callback: (r: RespuestaGoogle) => void }) => void;
      renderButton: (elemento: HTMLElement, opciones: Record<string, string | number>) => void;
    };
  };
};

declare global {
  interface Window {
    google?: ApiGoogle;
  }
}

const URL_SCRIPT = "https://accounts.google.com/gsi/client";

export const googleDisponible = GOOGLE_CLIENT_ID !== "";

function cargarScript(): Promise<void> {
  if (window.google) return Promise.resolve();
  return new Promise((resolver, rechazar) => {
    const existente = document.querySelector<HTMLScriptElement>(`script[src="${URL_SCRIPT}"]`);
    const script = existente ?? document.createElement("script");
    script.addEventListener("load", () => resolver(), { once: true });
    script.addEventListener("error", () => rechazar(new Error("No se pudo cargar Google")), { once: true });
    if (!existente) {
      script.src = URL_SCRIPT;
      script.async = true;
      document.head.appendChild(script);
    }
  });
}

type Props = {
  alRecibirCredencial: (idToken: string) => void;
  alFallar: () => void;
};

export default function BotonGoogle({ alRecibirCredencial, alFallar }: Props) {
  const contenedor = useRef<HTMLDivElement>(null);
  const callbacks = useRef({ alRecibirCredencial, alFallar });

  useEffect(() => {
    callbacks.current = { alRecibirCredencial, alFallar };
  }, [alRecibirCredencial, alFallar]);

  useEffect(() => {
    if (!googleDisponible) return;
    let activo = true;
    cargarScript()
      .then(() => {
        if (!activo || !window.google || !contenedor.current) return;
        window.google.accounts.id.initialize({
          client_id: GOOGLE_CLIENT_ID,
          callback: (respuesta) => {
            if (respuesta.credential) callbacks.current.alRecibirCredencial(respuesta.credential);
            else callbacks.current.alFallar();
          },
        });
        window.google.accounts.id.renderButton(contenedor.current, {
          type: "standard",
          theme: "outline",
          size: "large",
          text: "continue_with",
          shape: "pill",
          locale: "es",
          width: 320,
        });
      })
      .catch(() => {
        if (activo) callbacks.current.alFallar();
      });
    return () => {
      activo = false;
    };
  }, []);

  if (!googleDisponible) return null;
  return <div ref={contenedor} aria-label="Continuar con Google" />;
}

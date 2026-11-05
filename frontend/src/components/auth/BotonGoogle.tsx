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
  alNoConfigurado: () => void;
  claseBoton: string;
};

function IconoGoogle() {
  return (
    <svg width="18" height="18" viewBox="0 0 48 48" aria-hidden="true" focusable="false">
      <path fill="#EA4335" d="M24 9.5c3.5 0 6.6 1.2 9.1 3.6l6.8-6.8C35.8 2.4 30.3 0 24 0 14.6 0 6.5 5.4 2.6 13.2l7.9 6.1C12.4 13.6 17.7 9.5 24 9.5z" />
      <path fill="#4285F4" d="M46.5 24.5c0-1.6-.1-3.1-.4-4.5H24v9h12.7c-.6 3-2.3 5.5-4.8 7.2l7.6 5.9c4.4-4.1 7-10.1 7-17.6z" />
      <path fill="#FBBC05" d="M10.5 28.7A14.5 14.5 0 0 1 9.5 24c0-1.6.3-3.2.8-4.7l-7.9-6.1A24 24 0 0 0 0 24c0 3.9.9 7.5 2.6 10.8l7.9-6.1z" />
      <path fill="#34A853" d="M24 48c6.5 0 11.9-2.1 15.9-5.8l-7.6-5.9c-2.1 1.4-4.9 2.3-8.3 2.3-6.3 0-11.6-4.1-13.5-9.8l-7.9 6.1C6.5 42.6 14.6 48 24 48z" />
    </svg>
  );
}

export default function BotonGoogle({ alRecibirCredencial, alFallar, alNoConfigurado, claseBoton }: Props) {
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

  if (!googleDisponible)
    return (
      <button type="button" className={claseBoton} onClick={alNoConfigurado}>
        <IconoGoogle />
        Continuar con Google
      </button>
    );
  return <div ref={contenedor} aria-label="Continuar con Google" />;
}

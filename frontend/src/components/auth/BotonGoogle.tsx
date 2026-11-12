"use client";

import { useEffect, useRef, useState } from "react";
import styles from "./LoginForm.module.css";

/** El Client ID de Google (público). Sin él, el botón no aparece y todo sigue como antes. */
export const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID?.trim() ?? "";

type Gsi = {
  accounts: {
    id: {
      initialize: (opciones: { client_id: string; callback: (r: { credential?: string }) => void; ux_mode?: "popup"; locale?: string }) => void;
      renderButton: (contenedor: HTMLElement, opciones: Record<string, string | number>) => void;
    };
  };
};

const URL_SCRIPT = "https://accounts.google.com/gsi/client";
let cargando: Promise<Gsi> | null = null;

/** Carga Google Identity Services una sola vez. */
function cargarGsi(): Promise<Gsi> {
  const listo = (window as unknown as { google?: Gsi }).google;
  if (listo?.accounts?.id) return Promise.resolve(listo);
  cargando ??= new Promise<Gsi>((resolver, rechazar) => {
    const script = document.createElement("script");
    script.src = URL_SCRIPT;
    script.async = true;
    script.onload = () => resolver((window as unknown as { google: Gsi }).google);
    script.onerror = () => {
      cargando = null;
      rechazar(new Error("No se pudo cargar Google."));
    };
    document.head.appendChild(script);
  });
  return cargando;
}

type Props = {
  /** Recibe el ID token que da Google; el backend lo verifica en POST /api/auth/google. */
  alEntrar: (credencial: string) => void;
};

/** «Continuar con Google» con el botón oficial (Google exige su propio diseño). */
export default function BotonGoogle({ alEntrar }: Props) {
  const contenedor = useRef<HTMLDivElement>(null);
  const alEntrarActual = useRef(alEntrar);
  const [error, setError] = useState(false);

  useEffect(() => {
    alEntrarActual.current = alEntrar;
  }, [alEntrar]);

  useEffect(() => {
    if (!GOOGLE_CLIENT_ID) return;
    let vivo = true;
    cargarGsi()
      .then((google) => {
        if (!vivo || !contenedor.current) return;
        google.accounts.id.initialize({
          client_id: GOOGLE_CLIENT_ID,
          ux_mode: "popup",
          callback: (r) => r.credential && alEntrarActual.current(r.credential),
        });
        google.accounts.id.renderButton(contenedor.current, {
          type: "standard",
          theme: "outline",
          size: "large",
          text: "continue_with",
          shape: "pill",
          logo_alignment: "center",
          locale: "es",
          width: Math.min(400, Math.max(200, Math.round(contenedor.current.getBoundingClientRect().width))),
        });
      })
      .catch(() => vivo && setError(true));
    return () => {
      vivo = false;
    };
  }, []);

  if (!GOOGLE_CLIENT_ID) return null;
  return (
    <div className={styles.google}>
      <div className={styles.separador} aria-hidden="true">
        <span>o</span>
      </div>
      <div ref={contenedor} className={styles.botonGoogle} />
      {error && <p className={styles.errorCampo}>No pudimos cargar el acceso con Google. Revisa tu conexión.</p>}
    </div>
  );
}

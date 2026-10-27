"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from "react";
import { Flecha, Icono, Informacion } from "./Iconos";
import styles from "./Onboarding.module.css";
import { PASOS } from "./pasos";
import { useSesion } from "@/lib/useSesion";

// Se recuerda solo durante la pestaña: cada visita nueva sin sesión vuelve a mostrar el onboarding.
const CLAVE_VISTO = "breaku_onboarding_visto";
const UMBRAL_SWIPE = 50; // píxeles mínimos para contar como deslizamiento

function yaFueVisto(): boolean {
  try {
    return window.sessionStorage.getItem(CLAVE_VISTO) !== null;
  } catch {
    return false;
  }
}

const sinSuscripcion = () => () => {};

function marcarVisto() {
  try {
    window.sessionStorage.setItem(CLAVE_VISTO, "1");
  } catch {
    // Sin sessionStorage (modo privado estricto): se mostrará de nuevo al volver a "/".
  }
}

type Props = {
  /** Ignora la marca de "ya visto" (útil en desarrollo con ?onboarding=1). */
  forzar?: boolean;
};

export default function Onboarding({ forzar = false }: Props) {
  const router = useRouter();
  const [indice, setIndice] = useState(0);
  // null en el servidor y durante la hidratación: aún no se sabe si ya lo vio.
  const visto = useSyncExternalStore<boolean | null>(sinSuscripcion, yaFueVisto, () => null);
  const { sesion, listo } = useSesion();
  // Con la sesión abierta no hace falta el onboarding: se va directo a Inicio.
  const verificando = !listo || !!sesion || (!forzar && (visto === null || visto));
  const inicioToque = useRef<number | null>(null);

  const paso = PASOS[indice];
  const esUltimo = indice === PASOS.length - 1;

  useEffect(() => {
    if (!listo) return;
    if (sesion) router.replace("/inicio");
    else if (!forzar && visto) router.replace("/login");
  }, [listo, sesion, forzar, visto, router]);

  const terminar = useCallback(() => {
    marcarVisto();
    router.push("/login");
  }, [router]);

  const siguiente = useCallback(() => {
    if (esUltimo) {
      terminar();
    } else {
      setIndice((actual) => actual + 1);
    }
  }, [esUltimo, terminar]);

  const anterior = useCallback(() => {
    setIndice((actual) => Math.max(0, actual - 1));
  }, []);

  useEffect(() => {
    if (verificando) return;
    function alPresionarTecla(evento: KeyboardEvent) {
      if (evento.key === "ArrowRight") siguiente();
      if (evento.key === "ArrowLeft") anterior();
    }
    window.addEventListener("keydown", alPresionarTecla);
    return () => window.removeEventListener("keydown", alPresionarTecla);
  }, [verificando, siguiente, anterior]);

  function alTocar(evento: React.TouchEvent) {
    inicioToque.current = evento.touches[0].clientX;
  }

  function alSoltar(evento: React.TouchEvent) {
    if (inicioToque.current === null) return;
    const diferencia = evento.changedTouches[0].clientX - inicioToque.current;
    inicioToque.current = null;
    if (diferencia <= -UMBRAL_SWIPE) siguiente();
    if (diferencia >= UMBRAL_SWIPE) anterior();
  }

  const variables = {
    "--fondo": paso.fondo,
    "--acento": paso.acento,
    "--acento-texto": paso.acentoTexto,
  } as React.CSSProperties;

  return (
    <div
      className={styles.pantalla}
      style={variables}
      onTouchStart={alTocar}
      onTouchEnd={alSoltar}
    >
      {!verificando && (
        <main className={styles.contenedor}>
          <header className={styles.cabecera}>
            <button type="button" className={styles.saltar} onClick={terminar}>
              Saltar
            </button>
          </header>

          <section key={indice} className={styles.contenido} aria-live="polite">
            <div className={styles.icono}>
              <Icono nombre={paso.icono} />
            </div>
            <h1 className={styles.titulo}>{paso.titulo}</h1>
            <p className={styles.texto}>{paso.texto}</p>
            <p className={styles.nota}>
              <Informacion />
              <span>{paso.nota}</span>
            </p>
          </section>

          <footer className={styles.pie}>
            <div className={styles.puntos}>
              {PASOS.map((_, i) => (
                <button
                  key={i}
                  type="button"
                  className={i === indice ? styles.puntoActivo : styles.punto}
                  aria-label={`Paso ${i + 1} de ${PASOS.length}`}
                  aria-current={i === indice ? "step" : undefined}
                  onClick={() => setIndice(i)}
                />
              ))}
            </div>
            <button type="button" className={styles.boton} onClick={siguiente}>
              {paso.boton}
              <Flecha />
            </button>
          </footer>
        </main>
      )}
    </div>
  );
}

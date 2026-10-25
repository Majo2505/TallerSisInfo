"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  EstadoUbicacion,
  listarZonas,
  obtenerEstadoUbicacion,
  revocarUbicacion,
  seleccionarZona,
  compartirUbicacion,
  Zona,
} from "@/lib/ubicacion";
import styles from "./ConfiguracionUbicacion.module.css";

export default function ConfiguracionUbicacion() {
  const [estado, setEstado] = useState<EstadoUbicacion | null>(null);
  const [zonas, setZonas] = useState<Zona[]>([]);
  const [zonaSeleccionada, setZonaSeleccionada] = useState("");
  const [aceptaCompartir, setAceptaCompartir] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");
  const [mensaje, setMensaje] = useState("");

  useEffect(() => {
    let mounted = true;
    Promise.all([obtenerEstadoUbicacion(), listarZonas()])
      .then(([nuevoEstado, nuevasZonas]) => {
        if (!mounted) return;
        setEstado(nuevoEstado);
        setZonas(nuevasZonas);
        setZonaSeleccionada(nuevoEstado.zonaActual ? String(nuevoEstado.zonaActual.id) : "");
      })
      .catch((e: unknown) => {
        if (mounted) setError(e instanceof Error ? e.message : "No se pudo cargar la configuración.");
      })
      .finally(() => {
        if (mounted) setCargando(false);
      });
    return () => {
      mounted = false;
    };
  }, []);

  async function conGuardado(accion: () => Promise<EstadoUbicacion | void>, confirmacion: string) {
    setGuardando(true);
    setError("");
    setMensaje("");
    try {
      const nuevoEstado = await accion();
      if (nuevoEstado) {
        setEstado(nuevoEstado);
        setZonaSeleccionada(nuevoEstado.zonaActual ? String(nuevoEstado.zonaActual.id) : "");
      } else {
        setEstado({ consentimientoActivo: false, zonaActual: null });
        setZonaSeleccionada("");
      }
      setAceptaCompartir(nuevoEstado?.consentimientoActivo ?? false);
      setMensaje(confirmacion);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo guardar la configuración.");
    } finally {
      setGuardando(false);
    }
  }

  function solicitarUbicacion() {
    if (!aceptaCompartir) {
      setError("Confirma que aceptas compartir tu ubicación antes de continuar.");
      return;
    }
    if (!navigator.geolocation) {
      setError("Este navegador no permite obtener la ubicación. Puedes elegir tu campus manualmente.");
      return;
    }

    setGuardando(true);
    setError("");
    setMensaje("");
    navigator.geolocation.getCurrentPosition(
      ({ coords }) => {
        void conGuardado(
          () => compartirUbicacion(coords.latitude, coords.longitude),
          "Tu campus quedó actualizado. BREAKU guarda solo la zona, no tus coordenadas."
        );
      },
      (geolocationError) => {
        setGuardando(false);
        const texto = geolocationError.code === geolocationError.PERMISSION_DENIED
          ? "No se concedió el permiso del navegador. Puedes continuar eligiendo tu campus manualmente."
          : geolocationError.code === geolocationError.TIMEOUT
            ? "No pudimos obtener tu ubicación a tiempo. Inténtalo de nuevo o elige tu campus manualmente."
            : "No pudimos obtener tu ubicación. Puedes elegir tu campus manualmente.";
        setError(texto);
      },
      { enableHighAccuracy: false, timeout: 12_000, maximumAge: 0 }
    );
  }

  function guardarZonaManual() {
    if (!zonaSeleccionada) {
      setError("Selecciona un campus para continuar.");
      return;
    }
    void conGuardado(
      () => seleccionarZona(Number(zonaSeleccionada)),
      "Campus guardado manualmente. No se comparte la ubicación del dispositivo."
    );
  }

  if (cargando) {
    return <main className={styles.contenedor} aria-busy="true">Cargando configuración de ubicación…</main>;
  }

  return (
    <main className={styles.contenedor}>
      <Link className={styles.volver} href="/horario">← Volver a mi horario</Link>
      <section className={styles.tarjeta}>
        <p className={styles.etiqueta}>Privacidad y recomendaciones</p>
        <h1>Campus o zona actual</h1>
        <p>
          Tu ubicación sirve para encontrar un campus cercano y preparar recomendaciones relevantes.
          Solo se utiliza para recomendaciones activas; no compartimos tus coordenadas con terceros.
        </p>

        <div className={styles.estado} role="status">
          <strong>{estado?.consentimientoActivo ? "Consentimiento de ubicación activo" : "Ubicación del dispositivo desactivada"}</strong>
          <span>
            {estado?.zonaActual
              ? `Campus actual: ${estado.zonaActual.nombre}`
              : "Aún no hay un campus seleccionado."}
          </span>
        </div>

        {error && <p className={styles.error} role="alert">{error}</p>}
        {mensaje && <p className={styles.exito} role="status">{mensaje}</p>}

        <section className={styles.seccion} aria-labelledby="titulo-compartir">
          <h2 id="titulo-compartir">Compartir ubicación del dispositivo</h2>
          <p>
            Al aceptar, el navegador comparte tu ubicación una vez. El servidor la compara con el catálogo
            de campus y guarda únicamente el campus coincidente, nunca latitud ni longitud.
          </p>
          <label className={styles.consentimiento}>
            <input
              type="checkbox"
              checked={aceptaCompartir}
              onChange={(event) => setAceptaCompartir(event.target.checked)}
            />
            <span>Entiendo el uso y autorizo obtener mi ubicación para actualizar mi campus.</span>
          </label>
          <button
            type="button"
            className={styles.primario}
            disabled={guardando}
            onClick={solicitarUbicacion}
          >
            {guardando ? "Procesando…" : "Aceptar y compartir una vez"}
          </button>
        </section>

        <section className={styles.seccion} aria-labelledby="titulo-manual">
          <h2 id="titulo-manual">Elegir campus manualmente</h2>
          <p>Esta opción no solicita ni comparte la ubicación del dispositivo.</p>
          {zonas.length === 0 ? (
            <p className={styles.aviso}>
              Aún no hay campus disponibles en el catálogo. Contacta al administrador para que agregue zonas.
            </p>
          ) : (
            <div className={styles.formulario}>
              <label htmlFor="zona">Campus o zona</label>
              <select
                id="zona"
                value={zonaSeleccionada}
                onChange={(event) => setZonaSeleccionada(event.target.value)}
              >
                <option value="">Selecciona un campus</option>
                {zonas.map((zona) => <option key={zona.id} value={zona.id}>{zona.nombre}</option>)}
              </select>
              <button
                type="button"
                className={styles.secundario}
                disabled={guardando || !zonaSeleccionada}
                onClick={guardarZonaManual}
              >
                Guardar selección manual
              </button>
            </div>
          )}
        </section>

        {estado?.consentimientoActivo && (
          <section className={styles.seccion}>
            <h2>Revocar permiso</h2>
            <p>
              Al revocar, BREAKU desactiva el consentimiento y elimina el campus guardado.
              El permiso del navegador también puede administrarse desde la configuración del navegador.
            </p>
            <button
              type="button"
              className={styles.revocar}
              disabled={guardando}
              onClick={() => void conGuardado(revocarUbicacion, "Permiso revocado y campus eliminado.")}
            >
              Dejar de compartir y eliminar mi campus
            </button>
          </section>
        )}
      </section>
    </main>
  );
}

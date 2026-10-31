"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoCheck, IconoFlechaIzquierda, IconoObjetivo, IconoPin } from "@/components/app/Iconos";
import { Entrada, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import { Selector } from "@/components/app/Selectores";
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

/** HU5: elegir el campus a mano o compartir la ubicación una vez (con consentimiento), y revocarlo. */
export default function ConfiguracionUbicacion() {
  const router = useRouter();
  const reducir = useReducedMotion();
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

  function mensajeGeolocalizacion(codigo: number, e: GeolocationPositionError) {
    if (codigo === e.PERMISSION_DENIED) return "No se concedió el permiso del navegador. Puedes elegir tu campus manualmente.";
    if (codigo === e.TIMEOUT) return "No pudimos obtener tu ubicación a tiempo. Inténtalo de nuevo o elige tu campus manualmente.";
    return "No pudimos obtener tu ubicación. Puedes elegir tu campus manualmente.";
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
          "Tu campus quedó actualizado. BREAKU guarda solo el campus, no tus coordenadas.",
        );
      },
      (e) => {
        setGuardando(false);
        setError(mensajeGeolocalizacion(e.code, e));
      },
      { enableHighAccuracy: false, timeout: 12_000, maximumAge: 0 },
    );
  }

  function guardarZonaManual() {
    if (!zonaSeleccionada) {
      setError("Selecciona un campus para continuar.");
      return;
    }
    void conGuardado(() => seleccionarZona(Number(zonaSeleccionada)), "Campus guardado. No se compartió la ubicación del dispositivo.");
  }

  // Se llega desde Perfil, Campus o Comunidades: «Volver» regresa a donde estaba.
  function volver() {
    if (window.history.length > 1) router.back();
    else router.push("/perfil");
  }

  if (cargando) return <main className={styles.pagina} aria-busy="true" />;

  const opciones = zonas.map((z) => ({ valor: String(z.id), etiqueta: z.nombre }));
  const cambioPendiente = zonaSeleccionada !== "" && zonaSeleccionada !== String(estado?.zonaActual?.id ?? "");

  return (
    <Entrada as="main" className={styles.pagina}>
      <Pieza>
        <button type="button" className={styles.volver} onClick={volver}>
          <IconoFlechaIzquierda tamano={18} /> Volver
        </button>
      </Pieza>

      <Pieza as="header" className={styles.portada}>
        <div className={styles.portadaInterior} onPointerMove={seguirPuntero}>
          <FondoVivo />
          <span className={styles.chip}>
            <IconoPin tamano={14} /> Privacidad y recomendaciones
          </span>
          <h1 className={styles.titulo}>Tu campus</h1>
          <p className={styles.bajada}>
            Con tu campus buscamos lugares cercanos y compañeros con tiempo libre. Nunca guardamos tus coordenadas ni las compartimos.
          </p>
          <div className={styles.estado} role="status">
            <span className={estado?.zonaActual ? styles.estadoIconoOk : styles.estadoIcono} aria-hidden="true">
              {estado?.zonaActual ? <IconoCheck tamano={18} /> : <IconoPin tamano={18} />}
            </span>
            <span>
              <strong>{estado?.zonaActual ? estado.zonaActual.nombre : "Todavía no elegiste tu campus"}</strong>
              <small>{estado?.consentimientoActivo ? "Elegido con tu ubicación (consentimiento activo)" : "Ubicación del dispositivo desactivada"}</small>
            </span>
          </div>
        </div>
      </Pieza>

      <AnimatePresence mode="wait" initial={false}>
        {(error || mensaje) && (
          <motion.p
            key={error || mensaje}
            className={error ? styles.error : styles.exito}
            role={error ? "alert" : "status"}
            initial={reducir ? false : { opacity: 0, y: -6 }}
            animate={error && !reducir ? { opacity: 1, y: 0, x: [0, -8, 8, -5, 5, 0] } : { opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.3, ease: SALIDA }}
          >
            {error || mensaje}
          </motion.p>
        )}
      </AnimatePresence>

      <div className={styles.tarjetas}>
        <Pieza as="section" className={styles.tarjeta}>
          <span className={styles.recomendado}>Recomendado</span>
          <h2 className={styles.tarjetaTitulo}>Elegir mi campus</h2>
          <p className={styles.texto}>No se pide ni se comparte la ubicación del dispositivo.</p>
          {zonas.length === 0 ? (
            <p className={styles.aviso}>Todavía no hay campus disponibles. El administrador tiene que agregar al menos uno.</p>
          ) : (
            <>
              <div className={styles.campo}>
                <span id="etiqueta-campus">Campus</span>
                <Selector
                  valor={zonaSeleccionada}
                  opciones={opciones}
                  onCambio={setZonaSeleccionada}
                  etiqueta="Campus"
                  placeholder="Selecciona tu campus"
                />
              </div>
              <motion.button
                type="button"
                className={styles.primario}
                disabled={guardando || !cambioPendiente}
                onClick={guardarZonaManual}
                whileTap={{ scale: 0.97 }}
                transition={RESORTE_TOQUE}
              >
                {cambioPendiente || !estado?.zonaActual ? "Guardar campus" : "Campus guardado"}
              </motion.button>
            </>
          )}
        </Pieza>

        <Pieza as="section" className={styles.tarjeta}>
          <span className={styles.tarjetaIcono} aria-hidden="true">
            <IconoObjetivo tamano={20} />
          </span>
          <h2 className={styles.tarjetaTitulo}>Usar mi ubicación</h2>
          <p className={styles.texto}>
            El navegador comparte tu ubicación una sola vez. La comparamos con los campus y guardamos solo el que coincide, nunca la latitud ni la
            longitud.
          </p>
          <label className={styles.consentimiento}>
            <input type="checkbox" checked={aceptaCompartir} onChange={(e) => setAceptaCompartir(e.target.checked)} />
            <span className={styles.casilla} aria-hidden="true">
              <IconoCheck tamano={14} />
            </span>
            <span>Entiendo el uso y autorizo obtener mi ubicación para actualizar mi campus.</span>
          </label>
          <motion.button
            type="button"
            className={styles.secundario}
            disabled={guardando}
            onClick={solicitarUbicacion}
            whileTap={{ scale: 0.97 }}
            transition={RESORTE_TOQUE}
          >
            {guardando ? "Procesando…" : "Compartir una vez"}
          </motion.button>
        </Pieza>
      </div>

      {estado?.consentimientoActivo && (
        <Pieza as="section" className={styles.revocarTarjeta}>
          <div>
            <h2 className={styles.tarjetaTitulo}>Revocar permiso</h2>
            <p className={styles.texto}>
              Desactivamos el consentimiento y borramos el campus guardado. El permiso del navegador se administra desde su configuración.
            </p>
          </div>
          <button
            type="button"
            className={styles.revocar}
            disabled={guardando}
            onClick={() => void conGuardado(revocarUbicacion, "Permiso revocado y campus eliminado.")}
          >
            Dejar de compartir
          </button>
        </Pieza>
      )}
    </Entrada>
  );
}

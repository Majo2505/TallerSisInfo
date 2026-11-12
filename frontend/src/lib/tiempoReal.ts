"use client";

import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel, RetryContext } from "@microsoft/signalr";
import { useEffect, useRef } from "react";
import { API_URL } from "./config";
import { obtenerToken } from "@/services/authService";

/**
 * HU18: una sola conexión de tiempo real para toda la app (campanita, Descubrir, «Quiénes van»).
 * - Se abre cuando algo la usa y se cierra cuando ya nadie la usa (por ejemplo, al cerrar sesión).
 * - Reintenta sin rendirse: rápido al principio y después cada 15 s (antes se rendía a los ~40 s y la página
 *   dejaba de actualizarse sin avisar).
 * - Al reconectar, vuelve a seguir lo que seguía y avisa a la página para que pida todo de nuevo (lo que pasó
 *   mientras estuvo desconectada).
 */

type Oyente = (...args: never[]) => void;

const ESPERAS_MS = [0, 2_000, 5_000, 10_000];
const ESPERA_LARGA_MS = 15_000;

let conexion: HubConnection | null = null;
let usos = 0;
let reintento: ReturnType<typeof setTimeout> | null = null;
let sincronizacion: ReturnType<typeof setTimeout> | null = null;

const seguidas = new Map<number, number>();
const enServidor = new Set<number>();
let siguiendoDescubrir = 0;
let descubrirEnServidor = false;
const alReconectar = new Set<() => void>();

const conectada = () => conexion?.state === HubConnectionState.Connected;

function sincronizar() {
  sincronizacion = null;
  const c = conexion;
  if (!c || !conectada()) return;
  for (const id of seguidas.keys()) {
    if (enServidor.has(id)) continue;
    enServidor.add(id);
    c.invoke("Suscribirse", id).catch(() => undefined);
  }
  for (const id of [...enServidor]) {
    if (seguidas.has(id)) continue;
    enServidor.delete(id);
    c.invoke("Desuscribirse", id).catch(() => undefined);
  }
  if (siguiendoDescubrir > 0 && !descubrirEnServidor) {
    descubrirEnServidor = true;
    c.invoke("SeguirDescubrir").catch(() => undefined);
  }
}

// Se agrupan los cambios: si una lista cambia, no se desuscribe y vuelve a suscribir las que siguen igual.
function programarSincronizacion() {
  if (sincronizacion === null) sincronizacion = setTimeout(sincronizar, 0);
}

function alConectar() {
  enServidor.clear();
  descubrirEnServidor = false;
  sincronizar();
  alReconectar.forEach((f) => f());
}

function iniciar() {
  reintento = null;
  const c = conexion;
  if (!c || usos === 0 || c.state !== HubConnectionState.Disconnected || !obtenerToken()) return;
  c.start()
    .then(alConectar)
    .catch(() => {
      if (conexion === c && usos > 0) reintento = setTimeout(iniciar, ESPERA_LARGA_MS);
    });
}

function crear(): HubConnection {
  const c = new HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/comunidades`, { accessTokenFactory: () => obtenerToken() ?? "" })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (r: RetryContext) => ESPERAS_MS[r.previousRetryCount] ?? ESPERA_LARGA_MS,
    })
    .configureLogging(LogLevel.None)
    .build();
  c.onreconnected(alConectar);
  // Si se cerró del todo (no por nosotros), se vuelve a intentar.
  c.onclose(() => {
    if (conexion === c && usos > 0 && reintento === null) reintento = setTimeout(iniciar, ESPERA_LARGA_MS);
  });
  return c;
}

function usar(): () => void {
  usos++;
  if (!conexion) conexion = crear();
  if (reintento === null) iniciar();
  let soltada = false;
  return () => {
    if (soltada) return;
    soltada = true;
    usos--;
    if (usos > 0) return;
    const c = conexion;
    conexion = null;
    if (reintento !== null) clearTimeout(reintento);
    reintento = null;
    enServidor.clear();
    descubrirEnServidor = false;
    void c?.stop();
  };
}

/** Escucha un evento del servidor. Devuelve la función para dejar de escucharlo. */
export function escuchar(evento: string, oyente: Oyente): () => void {
  const soltar = usar();
  const c = conexion!;
  c.on(evento, oyente as (...args: unknown[]) => void);
  return () => {
    c.off(evento, oyente as (...args: unknown[]) => void);
    soltar();
  };
}

/** Recibe `comunidadActualizada` de esta comunidad. */
export function seguirComunidad(id: number): () => void {
  const soltar = usar();
  seguidas.set(id, (seguidas.get(id) ?? 0) + 1);
  programarSincronizacion();
  return () => {
    const quedan = (seguidas.get(id) ?? 1) - 1;
    if (quedan > 0) seguidas.set(id, quedan);
    else seguidas.delete(id);
    programarSincronizacion();
    soltar();
  };
}

/** Recibe `comunidadesCambiaron` cuando alguien de tu sede crea una comunidad. */
export function seguirDescubrir(): () => void {
  const soltar = usar();
  siguiendoDescubrir++;
  programarSincronizacion();
  return () => {
    siguiendoDescubrir--;
    soltar();
  };
}

/** Después de cambiar algo que define qué sede ves (aceptar el matching), el servidor lo vuelve a calcular. */
export function volverASeguirDescubrir() {
  descubrirEnServidor = false;
  programarSincronizacion();
}

// ---------- hooks

/** Llama a `oyente` cada vez que llega el evento (no hace falta memorizarlo). */
export function useEventoEnVivo<T extends unknown[]>(evento: string, oyente: (...args: T) => void) {
  const actual = useRef(oyente);
  useEffect(() => {
    actual.current = oyente;
  }, [oyente]);
  useEffect(() => escuchar(evento, ((...args: T) => actual.current(...args)) as unknown as Oyente), [evento]);
}

/** Llama a `alVolver` cada vez que la conexión se (re)establece: es el momento de pedir todo de nuevo. */
export function useAlReconectar(alVolver: () => void) {
  const actual = useRef(alVolver);
  useEffect(() => {
    actual.current = alVolver;
  }, [alVolver]);
  useEffect(() => {
    const f = () => actual.current();
    alReconectar.add(f);
    const soltar = usar();
    return () => {
      alReconectar.delete(f);
      soltar();
    };
  }, []);
}

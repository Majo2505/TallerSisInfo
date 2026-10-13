"use client";

import { useSyncExternalStore } from "react";
import { obtenerSesion, suscribirSesion } from "@/services/authService";

const sinSuscripcion = () => () => {};

/**
 * Estado de la sesión para componentes de cliente.
 * `listo` es false en el servidor y durante la hidratación (aún no se sabe si hay sesión guardada);
 * solo cuando es true se puede decidir con seguridad redirigir al login.
 */
export function useSesion() {
  const sesion = useSyncExternalStore(suscribirSesion, obtenerSesion, () => null);
  const listo = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  return { sesion, listo };
}

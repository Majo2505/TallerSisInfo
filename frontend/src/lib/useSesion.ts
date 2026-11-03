"use client";

import { useSyncExternalStore } from "react";
import { obtenerSesion, suscribirSesion } from "@/services/authService";

const sinSuscripcion = () => () => {};

export function useSesion() {
  const sesion = useSyncExternalStore(suscribirSesion, obtenerSesion, () => null);
  const listo = useSyncExternalStore(sinSuscripcion, () => true, () => false);
  return { sesion, listo };
}

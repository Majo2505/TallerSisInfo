"use client";

import { useEffect } from "react";
import { seguirComunidad, useEventoEnVivo } from "./tiempoReal";

export type EventoComunidad = {
  comunidadId: number;
  estado: string;
  confirmados: number;
};

/**
 * HU18: sigue estas comunidades y llama a `onActualizada` cuando cambia su cupo o se archivan. Usa la conexión
 * compartida (`lib/tiempoReal.ts`): si la lista cambia, solo se suscribe a las nuevas y deja las que ya no están.
 */
export function useComunidadesRealtime(comunidadIds: readonly number[], onActualizada: (evento: EventoComunidad) => void) {
  const ids = comunidadIds.join(",");

  useEventoEnVivo("comunidadActualizada", onActualizada);

  useEffect(() => {
    const soltar = ids ? ids.split(",").map((id) => seguirComunidad(Number(id))) : [];
    return () => soltar.forEach((f) => f());
  }, [ids]);
}

"use client";

import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useEffect, useRef } from "react";
import { API_URL } from "./config";
import { obtenerToken } from "@/services/authService";

export type EventoComunidad = {
  comunidadId: number;
  estado: string;
  confirmados: number;
};

export function useComunidadesRealtime(
  comunidadIds: readonly number[],
  onActualizada: (evento: EventoComunidad) => void,
) {
  const ids = comunidadIds.join(",");
  const alActualizar = useRef(onActualizada);

  useEffect(() => {
    alActualizar.current = onActualizada;
  }, [onActualizada]);

  useEffect(() => {
    const token = obtenerToken();
    const suscritas = ids ? ids.split(",").map(Number) : [];
    if (!token || suscritas.length === 0) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/comunidades`, { accessTokenFactory: () => obtenerToken() ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const suscribir = async () => {
      if (connection.state !== HubConnectionState.Connected) return;
      await Promise.allSettled(suscritas.map((id) => connection.invoke("Suscribirse", id)));
    };

    connection.on("comunidadActualizada", (evento: EventoComunidad) => alActualizar.current(evento));
    connection.onreconnected(() => void suscribir());
    void connection
      .start()
      .then(() => suscribir())
      .catch(() => undefined);

    return () => {
      void connection.stop();
    };
  }, [ids]);
}

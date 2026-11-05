"use client";

import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useEffect } from "react";
import { API_URL } from "./config";
import { obtenerToken } from "@/services/authService";

type EventoComunidad = {
  comunidadId: number;
  estado: string;
  confirmados: number;
};

export function useComunidadesRealtime(
  comunidadIds: readonly number[],
  onActualizada: (evento: EventoComunidad) => void,
) {
  const ids = comunidadIds.join(",");
  const idsToSubscribe = ids ? ids.split(",").map(Number) : [];

  useEffect(() => {
    const token = obtenerToken();
    if (!token || idsToSubscribe.length === 0) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/comunidades`, { accessTokenFactory: () => obtenerToken() ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const suscribir = async () => {
      if (connection.state !== HubConnectionState.Connected) return;
      await Promise.all(idsToSubscribe.map((id) => connection.invoke("Suscribirse", id)));
    };

    connection.on("comunidadActualizada", onActualizada);
    connection.onreconnected(() => void suscribir());
    void connection
      .start()
      .then(() => suscribir())
      .catch(() => undefined);

    return () => {
      void connection.stop();
    };
  }, [ids, onActualizada]);
}

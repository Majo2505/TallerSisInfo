"use client";

import { useCallback, useEffect, useState } from "react";
import { presentacionNotificacion } from "@/lib/reglasNotificaciones";
import { useRefrescoAutomatico } from "@/lib/useRefrescoAutomatico";
import { AlertaEstudiante, descartarAlertas, listarAlertas } from "@/lib/usuarios";
import estilos from "./Usuarios.module.css";

const fecha = (iso: string) =>
  new Date(iso).toLocaleString("es-BO", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });

type Props = {
  /** Avisos breves del panel (el mismo toast de las demás pestañas). */
  onAviso: (texto: string) => void;
  /** Abre el detalle del estudiante en la pestaña Usuarios, donde se decide (advertir, suspender, dar de baja). */
  onRevisar: (usuarioId: number) => void;
  /** Cuántas alertas hay, para la cifra de la pestaña. */
  onCantidad?: (cantidad: number) => void;
};

/**
 * HU32: estudiantes con alertas activas (intentos bloqueados por seguridad o reportes acumulados), para revisarlos y
 * decidir. El sistema nunca sanciona solo: «Revisar» abre su detalle y «Descartar» saca la alerta sin sancionar.
 */
export default function Alertas({ onAviso, onRevisar, onCantidad }: Props) {
  const [lista, setLista] = useState<AlertaEstudiante[] | null>(null);
  const [error, setError] = useState("");
  const [descartando, setDescartando] = useState<number | null>(null);

  const cargar = useCallback(() => {
    void listarAlertas()
      .then((datos) => {
        setLista(datos);
        setError("");
        onCantidad?.(datos.length);
      })
      .catch((e: Error) => setError(e.message));
  }, [onCantidad]);

  useEffect(cargar, [cargar]);
  // Una alerta nueva (aviso en vivo) o volver a la pestaña trae la lista al día.
  useRefrescoAutomatico(cargar);

  async function descartar(a: AlertaEstudiante) {
    setDescartando(a.usuarioId);
    try {
      await descartarAlertas(a.usuarioId);
      setLista((l) => {
        const nueva = l?.filter((x) => x.usuarioId !== a.usuarioId) ?? null;
        onCantidad?.(nueva?.length ?? 0);
        return nueva;
      });
      onAviso(`Alertas de ${a.nombre} descartadas. Si vuelve a pasar, aparecen de nuevo.`);
    } catch (e) {
      onAviso((e as Error).message);
    } finally {
      setDescartando(null);
    }
  }

  if (error) return <p className={estilos.error} role="alert">{error}</p>;
  if (!lista) return <div className="hz-skeleton" style={{ height: 160 }} aria-busy="true" aria-label="Cargando alertas" />;

  return (
    <div className={estilos.contenedor}>
      <p className={estilos.conteo}>
        Estudiantes con alertas activas ({lista.length})
      </p>
      {lista.length === 0 ? (
        <div className={estilos.vacio}>
          <strong>Sin alertas</strong>
          <span>
            Te avisamos cuando un estudiante acumule intentos bloqueados por seguridad o reportes de varias personas.
          </span>
        </div>
      ) : (
        <ul className={estilos.lista}>
          {lista.map((a) => (
            <li key={a.usuarioId} className={estilos.tarjeta}>
              <div className={estilos.cabecera}>
                <div>
                  <h3>{a.nombre}</h3>
                  <p>{a.correo}</p>
                </div>
                {!a.activo && <span className={`${estilos.estado} ${estilos.estado_BAJA}`}>Inactivo</span>}
              </div>
              <ul className={estilos.alertas}>
                {a.alertas.map((al) => (
                  <li key={al.tipo} className={estilos.alerta}>
                    <strong>{presentacionNotificacion(al.tipo).titulo}</strong>
                    <span>{al.mensaje}</span>
                    <small>{fecha(al.fecha)}</small>
                  </li>
                ))}
              </ul>
              <div className={estilos.acciones}>
                <button type="button" className="hz-btn hz-btn-primary" onClick={() => onRevisar(a.usuarioId)}>
                  Revisar y decidir
                </button>
                <button
                  type="button"
                  className="hz-btn"
                  disabled={descartando !== null}
                  onClick={() => void descartar(a)}
                  aria-label={`Descartar las alertas de ${a.nombre} sin sancionar`}
                >
                  {descartando === a.usuarioId ? "Descartando…" : "Descartar"}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

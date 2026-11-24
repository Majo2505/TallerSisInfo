"use client";

import { useCallback, useEffect, useState } from "react";
import { ComunidadAdmin, listarComunidadesAdmin, quitarComunidad, restaurarComunidad } from "@/lib/comunidadesAdmin";
import { puedeRestaurarComunidad, TEXTO_COMUNIDAD_TERMINADA } from "@/lib/reglasComunidadesAdmin";
import { validarMotivoModeracion } from "@/lib/reglasEventos";
import { useRefrescoAutomatico } from "@/lib/useRefrescoAutomatico";
import estilos from "./Usuarios.module.css";

type Vista = "ACTIVA" | "BAJADA";

const hora = (iso: string) => iso.slice(11, 16);
const dia = (iso: string) => `${iso.slice(8, 10)}/${iso.slice(5, 7)}`;
const fecha = (iso: string) => `${dia(iso)}/${iso.slice(0, 4)} ${hora(iso)}`;

type Props = {
  /** Avisos breves del panel (el mismo toast de las demás pestañas). */
  onAviso: (texto: string) => void;
};

/**
 * HU35: las comunidades activas (para quitar una inapropiada) y las quitadas (para restaurarla si fue un error). Quitar no
 * borra nada ni sanciona a quien la creó: sancionar sigue siendo aparte, en Usuarios.
 */
export default function ComunidadesAdmin({ onAviso }: Props) {
  const [vista, setVista] = useState<Vista>("ACTIVA");
  const [lista, setLista] = useState<ComunidadAdmin[] | null>(null);
  const [error, setError] = useState("");
  const [motivos, setMotivos] = useState<Record<number, string>>({});
  const [quitando, setQuitando] = useState<number | null>(null);
  const [ocupada, setOcupada] = useState<number | null>(null);

  const cargar = useCallback(() => {
    void listarComunidadesAdmin(vista)
      .then((datos) => {
        setLista(datos);
        setError("");
      })
      .catch((e: Error) => setError(e.message));
  }, [vista]);

  useEffect(cargar, [cargar]);
  useRefrescoAutomatico(cargar);

  async function quitar(c: ComunidadAdmin) {
    const motivo = motivos[c.id] ?? "";
    const falta = validarMotivoModeracion(motivo, "quitar");
    if (falta) return onAviso(falta);
    setOcupada(c.id);
    try {
      await quitarComunidad(c.id, motivo);
      setLista((l) => l?.filter((x) => x.id !== c.id) ?? null);
      setQuitando(null);
      onAviso(`«${c.actividad}» quedó quitada. Le avisamos a quien la creó y a quienes iban.`);
    } catch (e) {
      onAviso((e as Error).message);
      cargar();
    } finally {
      setOcupada(null);
    }
  }

  async function restaurar(c: ComunidadAdmin) {
    setOcupada(c.id);
    try {
      await restaurarComunidad(c.id);
      setLista((l) => l?.filter((x) => x.id !== c.id) ?? null);
      onAviso(`«${c.actividad}» volvió a estar activa. Le avisamos a quien la creó y a quienes iban.`);
    } catch (e) {
      onAviso((e as Error).message);
      cargar();
    } finally {
      setOcupada(null);
    }
  }

  return (
    <div className={estilos.contenedor}>
      <div className={estilos.acciones} role="tablist" aria-label="Comunidades por estado">
        {(["ACTIVA", "BAJADA"] as const).map((v) => (
          <button
            key={v}
            type="button"
            role="tab"
            aria-selected={vista === v}
            className={vista === v ? "hz-btn hz-btn-primary" : "hz-btn"}
            onClick={() => {
              setVista(v);
              setLista(null);
              setQuitando(null);
            }}
          >
            {v === "ACTIVA" ? "Activas" : "Quitadas"}
          </button>
        ))}
      </div>

      {error ? (
        <p className={estilos.error} role="alert">{error}</p>
      ) : !lista ? (
        <div className="hz-skeleton" style={{ height: 160 }} aria-busy="true" aria-label="Cargando comunidades" />
      ) : lista.length === 0 ? (
        <div className={estilos.vacio}>
          <strong>{vista === "ACTIVA" ? "No hay comunidades activas" : "No hay comunidades quitadas"}</strong>
          <span>
            {vista === "ACTIVA"
              ? "Cuando los estudiantes armen comunidades, aparecen aquí hasta que terminen."
              : "Las que quites aparecen aquí con su motivo, por si necesitas restaurarlas."}
          </span>
        </div>
      ) : (
        <ul className={estilos.lista}>
          {lista.map((c) => (
            <li key={c.id} className={estilos.tarjeta}>
              <div className={estilos.cabecera}>
                <div>
                  <h3>{c.actividad}</h3>
                  <p>
                    {dia(c.inicio)}, {hora(c.inicio)}–{hora(c.fin)} · {c.confirmados}/{c.maxParticipantes} van · creada por {c.creador}
                    {c.privada ? " · privada" : ""}
                  </p>
                </div>
                {c.reportesPendientes > 0 && (
                  <span className={`${estilos.estado} ${estilos.estado_SUSPENDIDO}`}>
                    {c.reportesPendientes} {c.reportesPendientes === 1 ? "reporte pendiente" : "reportes pendientes"}
                  </span>
                )}
              </div>

              {c.estado === "BAJADA" ? (
                <>
                  <div className={estilos.alerta}>
                    <strong>Quitada{c.fechaBaja ? ` el ${fecha(c.fechaBaja)}` : ""}</strong>
                    <span>Motivo: {c.motivoBaja}</span>
                  </div>
                  <div className={estilos.acciones}>
                    {puedeRestaurarComunidad(c.fin, new Date()) ? (
                      <button type="button" className="hz-btn" disabled={ocupada !== null} onClick={() => void restaurar(c)}>
                        {ocupada === c.id ? "Restaurando…" : "Restaurar"}
                      </button>
                    ) : (
                      <small>{TEXTO_COMUNIDAD_TERMINADA}</small>
                    )}
                  </div>
                </>
              ) : quitando === c.id ? (
                <>
                  <label className={estilos.buscador}>
                    Motivo para quitarla (obligatorio; se lo mandamos a quien la creó y a quienes iban)
                    <textarea
                      rows={3}
                      maxLength={500}
                      value={motivos[c.id] ?? ""}
                      autoFocus
                      onChange={(e) => setMotivos((m) => ({ ...m, [c.id]: e.target.value }))}
                    />
                  </label>
                  <p className={estilos.filaDato}>No se borra nada y no sanciona a quien la creó: eso es aparte, en Usuarios.</p>
                  <div className={estilos.acciones}>
                    <button type="button" className="hz-btn" disabled={ocupada !== null} onClick={() => setQuitando(null)}>
                      Cancelar
                    </button>
                    <button type="button" className="hz-btn hz-btn-peligro" disabled={ocupada !== null} onClick={() => void quitar(c)}>
                      {ocupada === c.id ? "Quitando…" : "Quitar comunidad"}
                    </button>
                  </div>
                </>
              ) : (
                <div className={estilos.acciones}>
                  <button type="button" className="hz-btn hz-btn-peligro" onClick={() => setQuitando(c.id)}>
                    Quitar comunidad
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

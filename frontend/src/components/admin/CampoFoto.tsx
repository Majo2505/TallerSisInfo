"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";
import { IconoCamara } from "@/components/app/Iconos";
import ArteCategoria from "@/components/inicio/ArteCategoria";
import { achicarFoto, urlDeFoto } from "@/lib/fotos";
import estilos from "./CampoFoto.module.css";

/** Qué hacer con la foto al guardar el lugar. */
export type CambioFoto = { tipo: "igual" } | { tipo: "nueva"; foto: Blob } | { tipo: "quitar" };

type Props = {
  /** Ruta de la foto guardada (`fotoUrl` del detalle), o null. */
  fotoActual: string | null;
  /** Para la ilustración que se muestra mientras no hay foto. */
  categoria: string;
  cambio: CambioFoto;
  onCambio: (cambio: CambioFoto) => void;
  deshabilitado?: boolean;
};

export default function CampoFoto({ fotoActual, categoria, cambio, onCambio, deshabilitado }: Props) {
  const id = useId();
  const entrada = useRef<HTMLInputElement>(null);
  const [preparando, setPreparando] = useState(false);
  const [error, setError] = useState("");
  // La vista previa de una foto nueva es una URL local; se libera al cambiarla o al cerrar.
  const vistaPrevia = useMemo(() => (cambio.tipo === "nueva" ? URL.createObjectURL(cambio.foto) : null), [cambio]);
  useEffect(() => () => {
    if (vistaPrevia) URL.revokeObjectURL(vistaPrevia);
  }, [vistaPrevia]);

  const mostrada = cambio.tipo === "nueva" ? vistaPrevia : cambio.tipo === "quitar" ? null : urlDeFoto(fotoActual);
  const hayFoto = !!mostrada;

  async function elegir(e: React.ChangeEvent<HTMLInputElement>) {
    const archivo = e.target.files?.[0];
    e.target.value = "";
    if (!archivo) return;
    setError("");
    setPreparando(true);
    try {
      onCambio({ tipo: "nueva", foto: await achicarFoto(archivo) });
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setPreparando(false);
    }
  }

  function quitar() {
    setError("");
    onCambio(fotoActual ? { tipo: "quitar" } : { tipo: "igual" });
  }

  return (
    <div className="hz-campo">
      <span id={`${id}-titulo`}>
        Foto <em>(opcional)</em>
      </span>
      <div className={estilos.marco} aria-busy={preparando || undefined}>
        {/* Sin foto se ve la ilustración de la categoría, igual que en las tarjetas del estudiante. */}
        {hayFoto ? (
          // eslint-disable-next-line @next/next/no-img-element -- foto servida por la API, no por Next
          <img className={estilos.foto} src={mostrada!} alt="Vista previa de la foto del lugar" />
        ) : (
          <ArteCategoria categoria={categoria || "Descanso"} />
        )}
        {preparando && <span className={estilos.preparando}>Preparando la foto…</span>}
      </div>

      <div className={estilos.acciones}>
        <input
          ref={entrada}
          id={`${id}-archivo`}
          className={estilos.oculto}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          onChange={elegir}
          disabled={deshabilitado || preparando}
          aria-describedby={`${id}-ayuda`}
        />
        <button type="button" className={estilos.elegir} onClick={() => entrada.current?.click()} disabled={deshabilitado || preparando}>
          <IconoCamara tamano={16} /> {hayFoto ? "Cambiar foto" : "Elegir foto"}
        </button>
        {hayFoto && (
          <button type="button" className={estilos.quitar} onClick={quitar} disabled={deshabilitado || preparando}>
            Quitar foto
          </button>
        )}
      </div>

      {error ? (
        <small className="hz-err" role="alert">
          {error}
        </small>
      ) : (
        <small id={`${id}-ayuda`} className={estilos.ayuda}>
          JPG, PNG o WebP. La achicamos antes de subirla. Sin foto, se muestra la ilustración de la categoría.
        </small>
      )}
    </div>
  );
}

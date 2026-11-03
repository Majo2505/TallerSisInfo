"use client";

import { useRef, useState } from "react";
import { IconoLapiz } from "@/components/app/Iconos";
import estilos from "./DatoEditable.module.css";

export type TipoDato = "texto" | "hora" | "numero";

type Props = {
  etiqueta: string;
  mostrar: string | null;
  inicial: string;
  tipo: TipoDato;
  sufijo?: string;
  largoMaximo?: number;
  editando: boolean;
  deshabilitado: boolean;
  onEditar: () => void;
  onGuardar: (valor: string) => void;
  onCancelar: () => void;
};

const FORMATO_HORA = /^([01]\d|2[0-3]):[0-5]\d$/;

function formatearHora(escrito: string): string {
  const digitos = escrito.replace(/\D/g, "").slice(0, 4);
  return digitos.length > 2 ? `${digitos.slice(0, 2)}:${digitos.slice(2)}` : digitos;
}

function completarHora(escrito: string): string | null {
  const limpio = escrito.trim();
  const conPuntos = limpio.includes(":") ? limpio : limpio.length === 3 ? `0${limpio[0]}:${limpio.slice(1)}` : limpio;
  const [h, m = ""] = conPuntos.split(":");
  const hora = `${h.padStart(2, "0")}:${m.padEnd(2, "0")}`;
  return FORMATO_HORA.test(hora) ? hora : null;
}

export default function DatoEditable(props: Props) {
  const { etiqueta, mostrar, inicial, tipo, sufijo, largoMaximo, editando, deshabilitado, onEditar, onGuardar, onCancelar } = props;
  const [valor, setValor] = useState(inicial);
  const [aviso, setAviso] = useState("");
  const terminado = useRef(false);

  function empezar() {
    if (deshabilitado) return;
    setValor(inicial);
    setAviso("");
    terminado.current = false;
    onEditar();
  }

  function guardar() {
    if (terminado.current) return;
    const escrito = valor.trim();
    let final = escrito;
    if (tipo === "hora") {
      final = completarHora(escrito) ?? "";
      if (!final) return setAviso("Escribe la hora así: 16:30");
    } else if (tipo === "numero") {
      if (!/^\d+$/.test(escrito)) return setAviso("Escribe un número");
      final = String(Number(escrito));
    } else if (!escrito) {
      return setAviso("No puede quedar vacío");
    }
    terminado.current = true;
    onGuardar(final);
  }

  function cancelar() {
    terminado.current = true;
    onCancelar();
  }

  if (editando) {
    return (
      <li className={estilos.editando}>
        <label htmlFor={`dato-${etiqueta}`}>{etiqueta}</label>
        <span className={estilos.fila}>
          <input
            id={`dato-${etiqueta}`}
            className={estilos.campo}
            value={valor}
            onChange={(e) => {
              setValor(tipo === "hora" ? formatearHora(e.target.value) : tipo === "numero" ? e.target.value.replace(/\D/g, "") : e.target.value);
              setAviso("");
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                guardar();
              } else if (e.key === "Escape") {
                e.preventDefault();
                cancelar();
              }
            }}
            onFocus={(e) => e.target.select()}
            onBlur={guardar}
            inputMode={tipo === "texto" ? "text" : "numeric"}
            placeholder={tipo === "hora" ? "HH:MM" : undefined}
            maxLength={tipo === "hora" ? 5 : tipo === "numero" ? 3 : largoMaximo}
            aria-invalid={aviso ? true : undefined}
            aria-describedby={aviso ? `aviso-${etiqueta}` : undefined}
            autoComplete="off"
            autoFocus
          />
          {sufijo && <span className={estilos.sufijo}>{sufijo}</span>}
        </span>
        {aviso ? (
          <small id={`aviso-${etiqueta}`} className={estilos.aviso}>
            {aviso}
          </small>
        ) : (
          <small className={estilos.ayuda}>Enter para guardar · Esc para cancelar</small>
        )}
      </li>
    );
  }

  return (
    <li>
      <button
        type="button"
        className={mostrar ? estilos.dato : estilos.datoFalta}
        onClick={empezar}
        disabled={deshabilitado}
        aria-label={`${etiqueta}: ${mostrar ?? "falta"}. Toca para editar`}
      >
        <span className={estilos.etiqueta}>{etiqueta}</span>
        <span className={estilos.lapiz} aria-hidden="true">
          <IconoLapiz tamano={14} />
        </span>
        <strong>{mostrar ?? "Falta"}</strong>
      </button>
    </li>
  );
}

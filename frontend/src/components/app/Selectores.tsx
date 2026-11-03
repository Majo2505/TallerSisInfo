"use client";

import { useCallback, useEffect, useId, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { IconoReloj } from "./Iconos";
import { SALIDA } from "./Movimiento";
import estilos from "./Selectores.module.css";

/*
 * Selectores con el estilo de BREAKU, en lugar de los <select> y <input type="time"> del navegador
 * (que se ven distintos en cada sistema). La lista se abre en un portal con position: fixed para que
 * ningún contenedor con overflow (por ejemplo, un modal) la recorte.
 */

type Posicion = { top: number; left: number; width: number; arriba: boolean };

function usePopover() {
  const [abierto, setAbierto] = useState(false);
  const [pos, setPos] = useState<Posicion | null>(null);
  const disparador = useRef<HTMLButtonElement>(null);
  const panel = useRef<HTMLDivElement>(null);

  const medir = useCallback(() => {
    const r = disparador.current?.getBoundingClientRect();
    if (!r) return;
    const alto = 280;
    const arriba = window.innerHeight - r.bottom < alto && r.top > alto;
    setPos({ top: arriba ? r.top - 6 : r.bottom + 6, left: r.left, width: r.width, arriba });
  }, []);

  useLayoutEffect(() => {
    if (abierto) medir();
  }, [abierto, medir]);

  useEffect(() => {
    if (!abierto) return;
    const fuera = (e: PointerEvent) => {
      const t = e.target as Node;
      if (!panel.current?.contains(t) && !disparador.current?.contains(t)) setAbierto(false);
    };
    // Si se desplaza la página (no la lista), se cierra: la lista quedaría lejos de su campo.
    const desplazar = (e: Event) => {
      if (!panel.current?.contains(e.target as Node)) setAbierto(false);
    };
    document.addEventListener("pointerdown", fuera);
    window.addEventListener("scroll", desplazar, true);
    window.addEventListener("resize", medir);
    return () => {
      document.removeEventListener("pointerdown", fuera);
      window.removeEventListener("scroll", desplazar, true);
      window.removeEventListener("resize", medir);
    };
  }, [abierto, medir]);

  const cerrar = useCallback((devolverFoco = true) => {
    setAbierto(false);
    if (devolverFoco) disparador.current?.focus();
  }, []);

  return { abierto, setAbierto, cerrar, pos, disparador, panel };
}

function Panel({
  pos,
  abierto,
  refPanel,
  ancho,
  children,
}: {
  pos: Posicion | null;
  abierto: boolean;
  refPanel: React.RefObject<HTMLDivElement | null>;
  ancho?: number;
  children: React.ReactNode;
}) {
  const reducir = useReducedMotion();
  if (typeof document === "undefined") return null;
  return createPortal(
    <AnimatePresence>
      {abierto && pos && (
        <motion.div
          ref={refPanel}
          className={estilos.panel}
          style={{
            top: pos.top,
            left: Math.min(pos.left, window.innerWidth - (ancho ?? pos.width) - 8),
            width: ancho ?? pos.width,
            transformOrigin: pos.arriba ? "bottom center" : "top center",
            translate: pos.arriba ? "0 -100%" : undefined,
          }}
          initial={reducir ? { opacity: 0 } : { opacity: 0, scale: 0.97, y: pos.arriba ? 4 : -4 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.98, transition: { duration: 0.1 } }}
          transition={{ duration: 0.16, ease: SALIDA }}
        >
          {children}
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  );
}

const Flecha = () => (
  <svg className={estilos.flecha} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="m6 9 6 6 6-6" />
  </svg>
);

/* ------------------------------------------------------------------ */

export type Opcion = { valor: string; etiqueta: string };

type PropsSelector = {
  valor: string;
  opciones: Opcion[];
  onCambio: (valor: string) => void;
  /** Nombre accesible si no hay un <label> que lo envuelva. */
  etiqueta?: string;
  placeholder?: string;
  invalido?: boolean;
  deshabilitado?: boolean;
  id?: string;
  /** Versión de 44 px para filas densas (por ejemplo, la revisión de la foto del horario). */
  compacto?: boolean;
  /** Clase extra para ubicar el campo en una grilla. */
  clase?: string;
};

/** Lista desplegable (reemplaza a <select>). Flechas para moverse, Enter para elegir, Escape para cerrar. */
export function Selector({ valor, opciones, onCambio, etiqueta, placeholder = "Elige una opción", invalido, deshabilitado, id, compacto, clase }: PropsSelector) {
  const { abierto, setAbierto, cerrar, pos, disparador, panel } = usePopover();
  const idLista = useId();
  const [activo, setActivo] = useState(0);
  const elegida = opciones.find((o) => o.valor === valor);

  useEffect(() => {
    if (!abierto) return;
    panel.current?.querySelector<HTMLElement>(`[data-indice="${activo}"]`)?.scrollIntoView({ block: "nearest" });
  }, [abierto, activo, panel]);

  function abrir() {
    setActivo(Math.max(0, opciones.findIndex((o) => o.valor === valor)));
    setAbierto(true);
  }

  function elegir(i: number) {
    onCambio(opciones[i].valor);
    cerrar();
  }

  function teclado(e: React.KeyboardEvent) {
    if (!abierto) {
      if (["ArrowDown", "ArrowUp", "Enter", " "].includes(e.key)) {
        e.preventDefault();
        abrir();
      }
      return;
    }
    if (e.key === "ArrowDown") setActivo((i) => Math.min(i + 1, opciones.length - 1));
    else if (e.key === "ArrowUp") setActivo((i) => Math.max(i - 1, 0));
    else if (e.key === "Home") setActivo(0);
    else if (e.key === "End") setActivo(opciones.length - 1);
    else if (e.key === "Enter" || e.key === " ") elegir(activo);
    else if (e.key === "Escape") cerrar();
    else if (e.key === "Tab") return cerrar(false);
    else return;
    e.preventDefault();
  }

  return (
    <>
      <button
        ref={disparador}
        id={id}
        type="button"
        className={[estilos.disparador, compacto ? estilos.compacto : "", clase ?? ""].join(" ")}
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={abierto}
        aria-controls={idLista}
        aria-label={etiqueta}
        aria-invalid={invalido || undefined}
        aria-activedescendant={abierto ? `${idLista}-${activo}` : undefined}
        disabled={deshabilitado}
        onClick={() => (abierto ? cerrar() : abrir())}
        onKeyDown={teclado}
      >
        <span className={elegida ? estilos.valor : estilos.placeholder}>{elegida?.etiqueta ?? placeholder}</span>
        <Flecha />
      </button>
      <Panel pos={pos} abierto={abierto} refPanel={panel}>
        <ul id={idLista} role="listbox" className={estilos.lista} aria-label={etiqueta}>
          {opciones.map((o, i) => (
            <li
              key={o.valor}
              id={`${idLista}-${i}`}
              data-indice={i}
              role="option"
              aria-selected={o.valor === valor}
              className={[estilos.opcion, i === activo ? estilos.opcionActiva : "", o.valor === valor ? estilos.opcionElegida : ""].join(" ")}
              onPointerEnter={() => setActivo(i)}
              onClick={() => elegir(i)}
            >
              {o.etiqueta}
              {o.valor === valor && <Check />}
            </li>
          ))}
        </ul>
      </Panel>
    </>
  );
}

const Check = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="m5 12.5 4.5 4.5L19 7.5" />
  </svg>
);

/* ------------------------------------------------------------------ */

const aMin = (hhmm: string) => {
  const [h, m] = hhmm.split(":").map(Number);
  return h * 60 + m;
};
const dos = (n: number) => String(n).padStart(2, "0");
const MINUTOS = Array.from({ length: 12 }, (_, i) => i * 5);

type PropsHora = {
  valor: string; // "HH:mm" o ""
  onCambio: (valor: string) => void;
  /** Hora mínima permitida, "HH:mm" (incluida). */
  min: string;
  /** Hora máxima permitida, "HH:mm" (incluida). */
  max: string;
  etiqueta?: string;
  invalido?: boolean;
  id?: string;
  compacto?: boolean;
  clase?: string;
};

/**
 * Selector de hora (reemplaza a <input type="time">): una columna de horas y otra de minutos de 5 en 5.
 * Lo que queda fuera de [min, max] aparece desactivado, así que no se puede elegir.
 */
export function SelectorHora({ valor, onCambio, min, max, etiqueta, invalido, id, compacto, clase }: PropsHora) {
  const { abierto, setAbierto, cerrar, pos, disparador, panel } = usePopover();
  const desde = aMin(min);
  const hasta = aMin(max);
  const [hSel, mSel] = valor ? valor.split(":").map(Number) : [NaN, NaN];
  const horas = Array.from({ length: Math.floor(hasta / 60) - Math.floor(desde / 60) + 1 }, (_, i) => Math.floor(desde / 60) + i);
  const valido = (h: number, m: number) => h * 60 + m >= desde && h * 60 + m <= hasta;

  useEffect(() => {
    if (!abierto) return;
    for (const lado of ["h", "m"])
      panel.current?.querySelector<HTMLElement>(`[data-col="${lado}"] [aria-selected="true"]`)?.scrollIntoView({ block: "center" });
  }, [abierto, panel]);

  function elegirHora(h: number) {
    // Se conserva el minuto si sigue siendo válido; si no, el primero válido de esa hora.
    const m = !Number.isNaN(mSel) && valido(h, mSel) ? mSel : MINUTOS.find((x) => valido(h, x))!;
    onCambio(`${dos(h)}:${dos(m)}`);
  }

  function elegirMinuto(m: number) {
    const h = !Number.isNaN(hSel) && valido(hSel, m) ? hSel : horas.find((x) => valido(x, m))!;
    onCambio(`${dos(h)}:${dos(m)}`);
    cerrar();
  }

  return (
    <>
      <button
        ref={disparador}
        id={id}
        type="button"
        className={[estilos.disparador, compacto ? estilos.compacto : "", clase ?? ""].join(" ")}
        aria-haspopup="dialog"
        aria-expanded={abierto}
        aria-label={etiqueta ? `${etiqueta}: ${valor || "sin elegir"}` : undefined}
        data-invalido={invalido || undefined}
        onClick={() => setAbierto(!abierto)}
        onKeyDown={(e) => e.key === "Escape" && cerrar()}
      >
        <span className={valor ? estilos.valorHora : estilos.placeholder}>{valor || "--:--"}</span>
        <IconoReloj tamano={18} />
      </button>
      <Panel pos={pos} abierto={abierto} refPanel={panel} ancho={Math.max(pos?.width ?? 0, 200)}>
        <div
          className={estilos.reloj}
          role="dialog"
          aria-label={etiqueta ?? "Elegir hora"}
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              e.preventDefault();
              cerrar();
            }
          }}
        >
          <div className={estilos.columnaTitulos} aria-hidden="true">
            <span>Hora</span>
            <span>Min</span>
          </div>
          <div className={estilos.columnas}>
            <ul className={estilos.columna} data-col="h" role="listbox" aria-label="Hora">
              {horas.map((h) => {
                const posible = MINUTOS.some((m) => valido(h, m));
                return (
                  <li key={h} role="option" aria-selected={h === hSel} aria-disabled={!posible || undefined}>
                    <button type="button" className={estilos.celda} disabled={!posible} onClick={() => elegirHora(h)}>
                      {dos(h)}
                    </button>
                  </li>
                );
              })}
            </ul>
            <ul className={estilos.columna} data-col="m" role="listbox" aria-label="Minutos">
              {MINUTOS.map((m) => {
                const posible = Number.isNaN(hSel) ? horas.some((h) => valido(h, m)) : valido(hSel, m);
                return (
                  <li key={m} role="option" aria-selected={m === mSel} aria-disabled={!posible || undefined}>
                    <button type="button" className={estilos.celda} disabled={!posible} onClick={() => elegirMinuto(m)}>
                      {dos(m)}
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>
          <p className={estilos.rango}>
            De {min} a {max}
          </p>
        </div>
      </Panel>
    </>
  );
}

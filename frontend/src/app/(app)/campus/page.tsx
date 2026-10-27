"use client";

import { useMemo, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoBuscar, IconoCheck } from "@/components/app/Iconos";
import { Entrada, Odometro, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import TarjetaLugar from "@/components/app/TarjetaLugar";
import { CATEGORIAS, Categoria, LUGARES } from "@/lib/maqueta";
import estilos from "./campus.module.css";

export default function Campus() {
  const reducir = useReducedMotion();
  const [categoria, setCategoria] = useState<Categoria | "Todos">("Todos");
  const [busqueda, setBusqueda] = useState("");

  const lugares = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return LUGARES.filter(
      (l) => (categoria === "Todos" || l.categoria === categoria) && (!texto || l.nombre.toLowerCase().includes(texto)),
    );
  }, [categoria, busqueda]);

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header" className={estilos.cabecera}>
        <div onPointerMove={seguirPuntero} className={estilos.portada}>
          <FondoVivo />
          <span className={estilos.etiqueta}>
            <IconoCheck tamano={14} /> Lugares verificados
          </span>
          <h1 className={estilos.titulo}>Tu campus, a pocos pasos</h1>
          <p className={estilos.bajada}>Rincones para comer, estudiar, descansar o moverte entre clases.</p>
          <p className={estilos.cuenta} aria-live="polite">
            <span className={estilos.cuentaNumero}>
              <Odometro valor={lugares.length} />
            </span>
            {lugares.length === 1 ? "lugar a la vista" : "lugares a la vista"}
          </p>
        </div>
      </Pieza>

      <Pieza className={estilos.controles}>
        <label className={estilos.buscador}>
          <IconoBuscar tamano={20} />
          <input
            type="search"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            placeholder="Buscar un lugar"
            aria-label="Buscar un lugar"
            enterKeyHint="search"
          />
        </label>

        <div className={estilos.filtros} role="group" aria-label="Categorías">
          {(["Todos", ...CATEGORIAS] as const).map((c) => (
            <motion.button
              key={c}
              type="button"
              className={categoria === c ? estilos.filtroActivo : estilos.filtro}
              aria-pressed={categoria === c}
              onClick={() => setCategoria(c)}
              whileTap={{ scale: 0.95 }}
              transition={RESORTE_TOQUE}
            >
              {categoria === c && (
                <motion.span
                  layoutId="filtro-campus"
                  className={estilos.pastilla}
                  transition={reducir ? { duration: 0 } : { type: "spring", stiffness: 420, damping: 34 }}
                />
              )}
              <span className={estilos.textoFiltro}>{c}</span>
            </motion.button>
          ))}
        </div>
      </Pieza>

      <Pieza>
        {lugares.length === 0 ? (
          <motion.div
            className={estilos.vacio}
            initial={reducir ? false : { opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.35, ease: SALIDA }}
          >
            <span className={estilos.vacioIcono} aria-hidden="true">
              <IconoBuscar tamano={24} />
            </span>
            <p>No encontramos lugares con ese filtro.</p>
            <button
              type="button"
              className={estilos.limpiar}
              onClick={() => {
                setBusqueda("");
                setCategoria("Todos");
              }}
            >
              Ver todos
            </button>
          </motion.div>
        ) : (
          <motion.ul className={estilos.lista} layout={!reducir}>
            <AnimatePresence mode="popLayout" initial={false}>
              {lugares.map((l) => (
                <motion.li
                  key={l.id}
                  layout={!reducir}
                  initial={{ opacity: 0, scale: 0.94, filter: "blur(4px)" }}
                  animate={{ opacity: 1, scale: 1, filter: "blur(0px)" }}
                  exit={{ opacity: 0, scale: 0.94, filter: "blur(4px)" }}
                  transition={{ duration: 0.35, ease: SALIDA }}
                  whileHover={reducir ? undefined : { y: -4 }}
                >
                  <TarjetaLugar lugar={l} />
                </motion.li>
              ))}
            </AnimatePresence>
          </motion.ul>
        )}
      </Pieza>

      <Pieza>
        <p className={estilos.nota}>Vista previa con lugares de ejemplo; el catálogo de verdad llega con el backend.</p>
      </Pieza>
    </Entrada>
  );
}

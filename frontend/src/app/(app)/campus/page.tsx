"use client";

import { useMemo, useState } from "react";
import comun from "@/components/app/Comun.module.css";
import { IconoBuscar } from "@/components/app/Iconos";
import TarjetaLugar from "@/components/app/TarjetaLugar";
import { CATEGORIAS, Categoria, LUGARES } from "@/lib/maqueta";
import estilos from "./campus.module.css";

export default function Campus() {
  const [categoria, setCategoria] = useState<Categoria | "Todos">("Todos");
  const [busqueda, setBusqueda] = useState("");

  const lugares = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return LUGARES.filter(
      (l) => (categoria === "Todos" || l.categoria === categoria) && (!texto || l.nombre.toLowerCase().includes(texto)),
    );
  }, [categoria, busqueda]);

  return (
    <main className={comun.pagina}>
      <header>
        <h1 className={comun.titulo}>Campus</h1>
        <p className={comun.subtitulo}>Lugares verificados cerca de ti para aprovechar tu tiempo libre.</p>
      </header>

      <div className={estilos.buscador}>
        <IconoBuscar tamano={20} />
        <input
          type="search"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar un lugar"
          aria-label="Buscar un lugar"
        />
      </div>

      <div className={estilos.filtros} role="group" aria-label="Categorías">
        {(["Todos", ...CATEGORIAS] as const).map((c) => (
          <button
            key={c}
            type="button"
            className={categoria === c ? estilos.filtroActivo : estilos.filtro}
            aria-pressed={categoria === c}
            onClick={() => setCategoria(c)}
          >
            {c}
          </button>
        ))}
      </div>

      {lugares.length === 0 ? (
        <p className={estilos.vacio}>No encontramos lugares con ese filtro.</p>
      ) : (
        <div className={estilos.lista}>
          {lugares.map((l) => (
            <TarjetaLugar key={l.id} lugar={l} variante="compacta" />
          ))}
        </div>
      )}

      <p className={comun.nota}>Vista previa con lugares de ejemplo; el catálogo real llegará con el backend.</p>
    </main>
  );
}

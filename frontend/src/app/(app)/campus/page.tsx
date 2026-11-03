"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import { IconoBuscar, IconoCheck, IconoPin } from "@/components/app/Iconos";
import { Entrada, Odometro, Pieza, RESORTE_TOQUE, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import { Selector } from "@/components/app/Selectores";
import TarjetaLugarViable from "@/components/campus/TarjetaLugarViable";
import { buscarLugaresViables, ResultadoLugares } from "@/lib/lugares";
import { Puente, duracionMin, hhmm, listarPuentes } from "@/lib/puentes";
import { useSesion } from "@/lib/useSesion";
import estilos from "./campus.module.css";

const DIAS = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

/** Elige el puente más útil ahora: el que está activo o el próximo (hoy primero, luego los siguientes días). */
function puenteSugerido(puentes: Puente[]): Puente | undefined {
  const ahora = new Date();
  const hoy = ahora.getDay();
  const minutosAhora = ahora.getHours() * 60 + ahora.getMinutes();
  const clave = (p: Puente) => {
    const dias = (p.diaSemana - hoy + 7) % 7;
    const yaTermino = dias === 0 && aMin(p.horaFin) <= minutosAhora;
    return (yaTermino ? 7 : dias) * 1440 + aMin(p.horaInicio);
  };
  return [...puentes].sort((a, b) => clave(a) - clave(b))[0];
}

const etiquetaPuente = (p: Puente) =>
  `${DIAS[p.diaSemana]} · ${hhmm(p.horaInicio)}–${hhmm(p.horaFin)} · ${duracionMin(p)} min`;

type RespuestaPara = { puenteId: number; resultado: ResultadoLugares };
type ErrorPara = { puenteId: number; mensaje: string };

export default function Campus() {
  const reducir = useReducedMotion();
  const { sesion } = useSesion();

  const [puentes, setPuentes] = useState<Puente[] | null>(null);
  const [errorPuentes, setErrorPuentes] = useState("");
  const [puenteId, setPuenteId] = useState<number | null>(null);
  const [respuesta, setRespuesta] = useState<RespuestaPara | null>(null);
  const [errorLugares, setErrorLugares] = useState<ErrorPara | null>(null);
  const [intento, setIntento] = useState(0);
  const [categoria, setCategoria] = useState("Todos");
  const [busqueda, setBusqueda] = useState("");

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    listarPuentes()
      .then((lista) => {
        if (!activo) return;
        setPuentes(lista);
        setPuenteId(puenteSugerido(lista)?.id ?? null);
      })
      .catch((e: Error) => {
        if (activo) setErrorPuentes(e.message);
      });
    return () => {
      activo = false;
    };
  }, [sesion]);

  useEffect(() => {
    if (puenteId === null) return;
    let activo = true;
    buscarLugaresViables(puenteId)
      .then((resultado) => {
        if (activo) setRespuesta({ puenteId, resultado });
      })
      .catch((e: Error) => {
        if (activo) setErrorLugares({ puenteId, mensaje: e.message });
      });
    return () => {
      activo = false;
    };
  }, [puenteId, intento]);

  const resultado = respuesta?.puenteId === puenteId ? respuesta.resultado : null;
  const error = errorLugares?.puenteId === puenteId ? errorLugares.mensaje : "";
  const buscando = puenteId !== null && !resultado && !error;
  const cargandoPuentes = !puentes && !errorPuentes;

  const categorias = useMemo(
    () => ["Todos", ...Array.from(new Set(resultado?.lugares.map((l) => l.categoria) ?? []))],
    [resultado],
  );

  const lugares = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return (resultado?.lugares ?? []).filter(
      (l) => (categoria === "Todos" || l.categoria === categoria) && (!texto || l.nombre.toLowerCase().includes(texto)),
    );
  }, [resultado, categoria, busqueda]);

  const elegirPuente = (id: number) => {
    setPuenteId(id);
    setCategoria("Todos");
    setBusqueda("");
  };

  const reintentar = () => {
    setErrorLugares(null);
    setIntento((n) => n + 1);
  };

  const hayLista = resultado?.estado === "OK";

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header" className={estilos.cabecera}>
        <div onPointerMove={seguirPuntero} className={estilos.portada}>
          <FondoVivo />
          <span className={estilos.etiqueta}>
            <IconoCheck tamano={14} /> Lugares verificados
          </span>
          <h1 className={estilos.titulo}>Tu campus, a pocos pasos</h1>
          <p className={estilos.bajada}>
            {resultado?.zona && resultado.radioAplicadoM
              ? `A menos de ${resultado.radioAplicadoM} m de ${resultado.zona.nombre}, para un puente de ${resultado.minutosDisponibles} min.`
              : "Rincones para comer, estudiar, descansar o moverte entre clases."}
          </p>
          {hayLista && (
            <p className={estilos.cuenta} aria-live="polite">
              <span className={estilos.cuentaNumero}>
                <Odometro valor={lugares.length} />
              </span>
              {lugares.length === 1 ? "lugar a la vista" : "lugares a la vista"}
            </p>
          )}
        </div>
      </Pieza>

      {puentes && puentes.length > 0 && (
        <Pieza>
          <div className={estilos.selector}>
            <span>¿Para qué puente buscas?</span>
            <Selector
              valor={puenteId ? String(puenteId) : ""}
              opciones={puentes.map((p) => ({ valor: String(p.id), etiqueta: etiquetaPuente(p) }))}
              onCambio={(v) => elegirPuente(Number(v))}
              etiqueta="¿Para qué puente buscas?"
            />
          </div>
        </Pieza>
      )}

      {hayLista && (
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
            {categorias.map((c) => (
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
      )}

      <Pieza>
        {(cargandoPuentes || buscando) && (
          <p className={estilos.estado} role="status" aria-busy="true">
            Buscando lugares para tu puente…
          </p>
        )}

        {errorPuentes && (
          <div className={estilos.aviso} role="alert">
            <p>{errorPuentes}</p>
          </div>
        )}

        {error && (
          <div className={estilos.aviso} role="alert">
            <p>{error}</p>
            <button type="button" className={estilos.accion} onClick={reintentar}>
              Intentar de nuevo
            </button>
          </div>
        )}

        {puentes && puentes.length === 0 && (
          <div className={estilos.aviso}>
            <p>
              <strong>Todavía no tienes puentes.</strong> Un puente es un hueco libre de 45 minutos o más entre dos clases.
              Registra tu horario y aquí verás los lugares que caben en cada uno.
            </p>
            <Link href="/horario" className={estilos.accion}>
              Registrar mi horario
            </Link>
          </div>
        )}

        {resultado?.estado === "SIN_ZONA" && (
          <div className={estilos.aviso}>
            <span className={estilos.avisoIcono} aria-hidden="true">
              <IconoPin tamano={22} />
            </span>
            <p>
              <strong>Primero elige tu campus.</strong> Para mostrarte lugares cercanos necesitamos saber dónde estás. Puedes compartir tu
              ubicación una sola vez (guardamos solo el campus, nunca tus coordenadas) o elegir tu campus a mano. Tú decides, y puedes
              retirar el permiso cuando quieras.
            </p>
            <Link href="/configuracion/ubicacion" className={estilos.accion}>
              Elegir mi campus
            </Link>
          </div>
        )}

        {resultado?.estado === "SIN_RESULTADOS" && (
          <motion.div
            className={estilos.vacio}
            initial={reducir ? false : { opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.35, ease: SALIDA }}
          >
            <span className={estilos.vacioIcono} aria-hidden="true">
              <IconoBuscar tamano={24} />
            </span>
            <p>{resultado.mensaje}</p>
            <p>Prueba con otro puente o cambia tu campus en tu perfil.</p>
          </motion.div>
        )}

        {hayLista && lugares.length === 0 && (
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
        )}

        {hayLista && lugares.length > 0 && resultado && (
          <motion.ul className={estilos.lista} layout={!reducir}>
            <AnimatePresence mode="popLayout" initial={false}>
              {lugares.map((l) => (
                <motion.li
                  key={l.lugarId}
                  layout={!reducir}
                  initial={{ opacity: 0, scale: 0.94, filter: "blur(4px)" }}
                  animate={{ opacity: 1, scale: 1, filter: "blur(0px)" }}
                  exit={{ opacity: 0, scale: 0.94, filter: "blur(4px)" }}
                  transition={{ duration: 0.35, ease: SALIDA }}
                  whileHover={reducir ? undefined : { y: -4 }}
                >
                  <TarjetaLugarViable lugar={l} minutosDisponibles={resultado.minutosDisponibles} />
                </motion.li>
              ))}
            </AnimatePresence>
          </motion.ul>
        )}
      </Pieza>

      <Pieza>
        <p className={estilos.nota}>Solo mostramos lugares aprobados por el administrador. Los que dicen «Por validar» todavía los está revisando el equipo.</p>
      </Pieza>
    </Entrada>
  );
}

"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import comun from "@/components/app/Comun.module.css";
import {
  IconoBrujula,
  IconoCalendario,
  IconoDestello,
  IconoFlechaDerecha,
  IconoLuna,
  IconoObjetivo,
  IconoPersonas,
  IconoRayo,
} from "@/components/app/Iconos";
import TarjetaLugar from "@/components/app/TarjetaLugar";
import { Bloque, listarHorario } from "@/lib/horario";
import { ESTADOS_ANIMO, LUGARES } from "@/lib/maqueta";
import { Puente, listarPuentes, duracionMin, hhmm } from "@/lib/puentes";
import { useSesion } from "@/lib/useSesion";
import estilos from "./inicio.module.css";

const ICONO_ESTADO = {
  cansado: IconoLuna,
  enfocado: IconoObjetivo,
  energia: IconoRayo,
  social: IconoPersonas,
};

const aMin = (h: string) => {
  const [a, b] = h.split(":").map(Number);
  return a * 60 + b;
};

const RADIO = 34;
const CIRCUNFERENCIA = 2 * Math.PI * RADIO;

export default function Inicio() {
  const router = useRouter();
  const { sesion } = useSesion();
  const aviso = useAviso();
  const [ahora, setAhora] = useState(() => new Date());
  const [puentes, setPuentes] = useState<Puente[]>([]);
  const [bloques, setBloques] = useState<Bloque[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const t = setInterval(() => setAhora(new Date()), 30_000);
    return () => clearInterval(t);
  }, []);

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    Promise.allSettled([listarPuentes(), listarHorario()]).then(([p, h]) => {
      if (!activo) return;
      if (p.status === "fulfilled") setPuentes(p.value);
      else setError((p.reason as Error).message);
      if (h.status === "fulfilled") setBloques(h.value);
      setCargando(false);
    });
    return () => {
      activo = false;
    };
  }, [sesion]);

  const minutosAhora = ahora.getHours() * 60 + ahora.getMinutes();
  const diaHoy = ahora.getDay();

  const { activo, proximo } = useMemo(() => {
    const deHoy = puentes
      .filter((p) => p.diaSemana === diaHoy)
      .sort((a, b) => aMin(a.horaInicio) - aMin(b.horaInicio));
    return {
      activo: deHoy.find((p) => aMin(p.horaInicio) <= minutosAhora && minutosAhora < aMin(p.horaFin)),
      proximo: deHoy.find((p) => aMin(p.horaInicio) > minutosAhora),
    };
  }, [puentes, diaHoy, minutosAhora]);

  const foco = activo ?? proximo;

  const siguienteClase = useMemo(() => {
    if (!foco) return undefined;
    return bloques
      .filter((b) => b.diaSemana === diaHoy && aMin(b.horaInicio) >= aMin(foco.horaFin))
      .sort((a, b) => aMin(a.horaInicio) - aMin(b.horaInicio))[0];
  }, [bloques, foco, diaHoy]);

  const nombre = sesion?.usuario.nombre.split(" ")[0] ?? "";
  const fechaTexto = ahora.toLocaleDateString("es-BO", {
    weekday: "short",
    day: "numeric",
    month: "long",
    year: "numeric",
  });
  const fecha = fechaTexto.charAt(0).toUpperCase() + fechaTexto.slice(1);

  const minutosRestantes = activo ? aMin(activo.horaFin) - minutosAhora : 0;
  const usado = activo ? Math.round(((minutosAhora - aMin(activo.horaInicio)) / duracionMin(activo)) * 100) : 0;
  const minutosParaIa = foco ? (activo ? minutosRestantes : duracionMin(foco)) : 0;
  const rutaIa = minutosParaIa > 0 ? `/ia?min=${minutosParaIa}` : "/ia";

  return (
    <main className={comun.pagina}>
      <header>
        <p className={estilos.fecha}>{fecha}</p>
        <h1 className={estilos.saludo}>
          Hola, <span className={estilos.nombre}>{nombre}</span>
        </h1>
      </header>

      <section className={estilos.puente} aria-label="Tu puente">
        {foco ? (
          <>
            <div className={estilos.puenteCuerpo}>
              <div className={estilos.puenteDatos}>
                <span className={estilos.insignia}>
                  <span className={estilos.punto} aria-hidden="true" />
                  {activo ? "Puente activo" : "Próximo puente"}
                </span>
                <p className={estilos.minutos}>
                  {activo ? minutosRestantes : duracionMin(foco)}
                  <span> min</span>
                </p>
                <p className={estilos.rango}>
                  {hhmm(foco.horaInicio)} – {hhmm(foco.horaFin)}
                  {!activo && ` · empieza en ${aMin(foco.horaInicio) - minutosAhora} min`}
                </p>
                {siguienteClase && (
                  <p className={estilos.proxima}>
                    <IconoCalendario tamano={16} />
                    <span>
                      Próxima: {siguienteClase.materia}
                      <small>
                        {hhmm(siguienteClase.horaInicio)}
                        {siguienteClase.aula ? ` · ${siguienteClase.aula}` : ""}
                      </small>
                    </span>
                  </p>
                )}
              </div>
              {activo && (
                <div className={estilos.anillo} role="img" aria-label={`${usado}% del puente usado`}>
                  <svg viewBox="0 0 80 80" width="96" height="96" aria-hidden="true">
                    <circle cx="40" cy="40" r={RADIO} className={estilos.pista} />
                    <circle
                      cx="40"
                      cy="40"
                      r={RADIO}
                      className={estilos.progreso}
                      strokeDasharray={CIRCUNFERENCIA}
                      strokeDashoffset={CIRCUNFERENCIA * (1 - usado / 100)}
                      transform="rotate(-90 40 40)"
                    />
                  </svg>
                  <span className={estilos.anilloTexto}>
                    <small>usado</small>
                    {usado}%
                  </span>
                </div>
              )}
            </div>
            <Link href={rutaIa} className={estilos.accion}>
              ¿Qué hago con mi tiempo? <IconoFlechaDerecha tamano={18} />
            </Link>
          </>
        ) : (
          <div className={estilos.sinPuente}>
            <p className={estilos.sinPuenteTitulo}>
              {cargando ? "Buscando tus puentes…" : "No tienes puentes por ahora"}
            </p>
            <p className={estilos.sinPuenteTexto}>
              {error
                ? error
                : "Un puente es un hueco libre de 45 minutos o más entre dos clases del mismo día. Registra tu horario y aparecerán aquí."}
            </p>
            <Link href="/horario" className={estilos.accionClara}>
              Ver mi horario
            </Link>
          </div>
        )}
      </section>

      <section aria-labelledby="como-te-sientes">
        <h2 id="como-te-sientes" className={comun.etiqueta}>
          ¿Cómo te sientes?
        </h2>
        <div className={estilos.estados}>
          {ESTADOS_ANIMO.map((e) => {
            const Icono = ICONO_ESTADO[e.id];
            return (
              <button
                key={e.id}
                type="button"
                className={`${estilos.estado} ${estilos[`estado_${e.id}`]}`}
                onClick={() => router.push(`${rutaIa}${rutaIa.includes("?") ? "&" : "?"}estado=${e.id}`)}
              >
                <Icono tamano={22} />
                {e.nombre}
              </button>
            );
          })}
        </div>
      </section>

      <section className={estilos.atajos} aria-label="Atajos">
        <button type="button" className={estilos.atajo} onClick={() => aviso.mostrar("Los grupos rápidos estarán disponibles pronto.")}>
          <span className={estilos.atajoIcono}>
            <IconoPersonas tamano={22} />
          </span>
          <span>
            <strong>Grupo rápido</strong>
            <small>Escríbelo</small>
          </span>
        </button>
        <Link href="/campus" className={estilos.atajo}>
          <span className={estilos.atajoIconoAlt}>
            <IconoBrujula tamano={22} />
          </span>
          <span>
            <strong>Descubrir</strong>
            <small>Campus</small>
          </span>
        </Link>
      </section>

      <section aria-labelledby="sugerencias">
        <div className={estilos.encabezado}>
          <h2 id="sugerencias" className={comun.etiqueta}>
            Sugerencias rápidas
          </h2>
          <Link href="/campus" className={estilos.verTodas}>
            Ver todas <IconoFlechaDerecha tamano={16} />
          </Link>
        </div>
        <div className={estilos.lista}>
          {LUGARES.slice(0, 2).map((l) => (
            <TarjetaLugar key={l.id} lugar={l} variante="compacta" />
          ))}
        </div>
        <p className={comun.nota}>
          <IconoDestello tamano={12} /> Vista previa con lugares de ejemplo; las sugerencias reales llegarán con el catálogo.
        </p>
      </section>

      <AvisoFlotante texto={aviso.texto} />
    </main>
  );
}

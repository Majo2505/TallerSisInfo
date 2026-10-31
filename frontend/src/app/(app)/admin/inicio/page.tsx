"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { motion, useReducedMotion } from "motion/react";
import FondoVivo from "@/components/app/FondoVivo";
import {
  IconoBandera,
  IconoBuscar,
  IconoCheck,
  IconoDestello,
  IconoFlechaDerecha,
  IconoPersonas,
  IconoPin,
} from "@/components/app/Iconos";
import { Entrada, Odometro, Pieza, SALIDA, seguirPuntero } from "@/components/app/Movimiento";
import { ResumenAdmin, obtenerResumen } from "@/lib/admin";
import { useSesion } from "@/lib/useSesion";
import { esAdmin } from "@/services/authService";
import estilos from "./inicio.module.css";

function saludoPara(hora: number) {
  if (hora < 12) return "Buenos días";
  if (hora < 19) return "Buenas tardes";
  return "Buenas noches";
}

const plural = (n: number, uno: string, varios: string) => `${n} ${n === 1 ? uno : varios}`;

/** Inicio del administrador: lo que hay que atender hoy, las cifras del campus y accesos directos al panel. */
export default function InicioAdmin() {
  const router = useRouter();
  const { sesion, listo } = useSesion();
  const reducir = useReducedMotion();
  const [resumen, setResumen] = useState<ResumenAdmin | null>(null);
  const [error, setError] = useState("");
  const [ahora] = useState(() => new Date());

  // Solo administradores: el resto vuelve a su inicio (el backend igual responde 403).
  useEffect(() => {
    if (listo && sesion && !esAdmin(sesion)) router.replace("/inicio");
  }, [listo, sesion, router]);

  useEffect(() => {
    if (!esAdmin(sesion)) return;
    let activo = true;
    obtenerResumen()
      .then((r) => activo && setResumen(r))
      .catch((e: Error) => activo && setError(e.message));
    return () => {
      activo = false;
    };
  }, [sesion]);

  if (!esAdmin(sesion)) return <main className={estilos.pagina} aria-busy="true" />;

  const nombre = sesion?.usuario.nombre.split(" ")[0] ?? "";
  const fechaTexto = ahora.toLocaleDateString("es-BO", { weekday: "long", day: "numeric", month: "long" });
  const fecha = fechaTexto.charAt(0).toUpperCase() + fechaTexto.slice(1);
  const saludo = saludoPara(ahora.getHours());

  const enRevision = resumen?.lugaresEnRevision ?? 0;
  const reportes = resumen?.reportesPendientes ?? 0;

  const cifras = [
    { valor: resumen?.lugaresActivos, etiqueta: "Lugares activos", clase: estilos.tonoLugares, Icono: IconoPin },
    { valor: resumen?.estudiantes, etiqueta: "Estudiantes", clase: estilos.tonoEstudiantes, Icono: IconoPersonas },
    { valor: resumen?.consultasRagSemana, etiqueta: "Consultas a la IA (7 días)", clase: estilos.tonoRag, Icono: IconoDestello },
    { valor: resumen?.comunidadesHoy, etiqueta: "Comunidades hoy", clase: estilos.tonoComunidades, Icono: IconoPersonas },
  ];

  const accesos = [
    {
      pestana: "catalogo",
      titulo: "Catálogo de lugares",
      texto: "Aprueba, desactiva o reactiva los lugares que se recomiendan.",
      insignia: enRevision ? `${enRevision} en revisión` : undefined,
      Icono: IconoPin,
    },
    {
      pestana: "reportes",
      titulo: "Reportes",
      texto: "Lo que los estudiantes avisaron sobre lugares o la app.",
      insignia: reportes ? plural(reportes, "pendiente", "pendientes") : undefined,
      Icono: IconoBandera,
    },
    {
      pestana: "metricas",
      titulo: "Métricas de la IA",
      texto: "Consultas por resultado, latencia y lugares más recomendados.",
      Icono: IconoDestello,
    },
    {
      pestana: "auditoria",
      titulo: "Auditoría RAG",
      texto: "Cada consulta al asistente con los lugares que la respaldan.",
      Icono: IconoBuscar,
    },
  ];

  return (
    <Entrada as="main" className={estilos.pagina}>
      <Pieza as="header" className={estilos.cabecera}>
        <p className={estilos.fecha}>{fecha}</p>
        <h1 className={estilos.saludo}>
          {saludo},{" "}
          <span className={estilos.nombre}>
            <motion.span
              className={estilos.resaltador}
              aria-hidden="true"
              initial={reducir ? false : { scaleX: 0 }}
              animate={{ scaleX: 1 }}
              transition={{ duration: 0.7, ease: SALIDA, delay: 0.35 }}
            />
            <span className={estilos.nombreTexto}>{nombre}</span>
          </span>
        </h1>
        <p className={estilos.bajada}>Así está el campus hoy.</p>
      </Pieza>

      {error && (
        <Pieza>
          <p className={estilos.error} role="alert">
            {error}
          </p>
        </Pieza>
      )}

      <Pieza as="section" className={estilos.marcoHeroe}>
        <div className={estilos.heroe} onPointerMove={seguirPuntero} aria-live="polite">
          <FondoVivo />
          <span className={estilos.chip}>
            <span className={estilos.punto} aria-hidden="true" /> Para atender
          </span>
          {resumen === null ? (
            <h2 className={estilos.heroeTitulo}>Revisando el campus…</h2>
          ) : (
            <Pendientes enRevision={enRevision} reportes={reportes} />
          )}
        </div>
      </Pieza>

      <Pieza as="section" className={estilos.bloque}>
        <h2 className={estilos.subtitulo}>El campus en cifras</h2>
        <div className={estilos.cifras}>
          {cifras.map(({ valor, etiqueta, clase, Icono }) => (
            <div key={etiqueta} className={`${estilos.cifra} ${clase}`}>
              <span className={estilos.cifraIcono} aria-hidden="true">
                <Icono tamano={18} />
              </span>
              <span className={estilos.cifraValor}>{valor === undefined ? "—" : <Odometro valor={valor} />}</span>
              <span className={estilos.cifraEtiqueta}>{etiqueta}</span>
            </div>
          ))}
        </div>
      </Pieza>

      <Pieza as="section" className={estilos.bloque}>
        <h2 className={estilos.subtitulo}>Ir al panel</h2>
        <ul className={estilos.accesos}>
          {accesos.map(({ pestana, titulo, texto, insignia, Icono }) => (
            <li key={pestana}>
              <Link href={`/admin?pestana=${pestana}`} className={estilos.acceso}>
                <span className={estilos.accesoIcono} aria-hidden="true">
                  <Icono tamano={20} />
                </span>
                <span className={estilos.accesoTexto}>
                  <strong>
                    {titulo}
                    {insignia && <span className={estilos.insignia}>{insignia}</span>}
                  </strong>
                  <small>{texto}</small>
                </span>
                <span className={estilos.flecha} aria-hidden="true">
                  <IconoFlechaDerecha tamano={18} />
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </Pieza>
    </Entrada>
  );
}

/** Lo que hay que atender. El botón lleva a lo más urgente: primero los lugares por revisar, luego los reportes. */
function Pendientes({ enRevision, reportes }: Readonly<{ enRevision: number; reportes: number }>) {
  const pendientes = enRevision + reportes;

  if (pendientes === 0)
    return (
      <>
        <h2 className={estilos.heroeTitulo}>
          <span className={estilos.check} aria-hidden="true">
            <IconoCheck tamano={26} />
          </span>
          Todo al día
        </h2>
        <p className={estilos.heroeTexto}>No hay lugares por revisar ni reportes pendientes.</p>
        <Link href="/admin?pestana=metricas" className={estilos.botonHeroe}>
          Ver métricas de la IA <IconoFlechaDerecha tamano={18} />
        </Link>
      </>
    );

  const partes = [
    enRevision ? plural(enRevision, "lugar espera tu revisión", "lugares esperan tu revisión") : null,
    reportes ? plural(reportes, "reporte sin resolver", "reportes sin resolver") : null,
  ].filter(Boolean);
  const aviso = enRevision ? " Los lugares en revisión no se recomiendan hasta que los apruebes." : "";

  return (
    <>
      <h2 className={estilos.heroeTitulo}>
        Tienes <Odometro valor={pendientes} /> {pendientes === 1 ? "pendiente" : "pendientes"}
      </h2>
      <p className={estilos.heroeTexto}>{`${partes.join(" y ")}.${aviso}`}</p>
      <Link href={`/admin?pestana=${enRevision ? "catalogo" : "reportes"}`} className={estilos.botonHeroe}>
        {enRevision ? "Revisar lugares" : "Ver reportes"} <IconoFlechaDerecha tamano={18} />
      </Link>
    </>
  );
}

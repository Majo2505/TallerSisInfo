"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { motion } from "motion/react";
import { useEffect } from "react";
import { Logo } from "@/components/auth/Iconos";
import Notificaciones from "@/components/notificaciones/Notificaciones";
import { iniciales } from "@/lib/iniciales";
import { useSesion } from "@/lib/useSesion";
import {
  IconoBrujula,
  IconoCalendario,
  IconoDestello,
  IconoInicio,
  IconoPersona,
} from "./Iconos";
import estilos from "./ShellApp.module.css";

// Resorte del indicador de sección: llega rápido y se asienta con un rebote mínimo.
const RESORTE = { type: "spring", bounce: 0.18, duration: 0.45 } as const;

// `corto` es la etiqueta de la barra inferior del celular, donde no entran nombres largos.
const SECCIONES = [
  { ruta: "/inicio", nombre: "Inicio", corto: "Inicio", Icono: IconoInicio },
  { ruta: "/horario", nombre: "Horario", corto: "Horario", Icono: IconoCalendario },
  { ruta: "/recomendaciones", nombre: "Recomendaciones", corto: "Para ti", Icono: IconoDestello },
  { ruta: "/campus", nombre: "Campus", corto: "Campus", Icono: IconoBrujula },
  { ruta: "/perfil", nombre: "Perfil", corto: "Perfil", Icono: IconoPersona },
];

export default function ShellApp({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { sesion, listo } = useSesion();

  useEffect(() => {
    if (listo && !sesion) router.replace("/login");
  }, [listo, sesion, router]);

  if (!listo || !sesion) {
    return <div className={estilos.app} aria-busy="true" />;
  }

  const activa = (ruta: string) => pathname === ruta || pathname.startsWith(`${ruta}/`);

  return (
    <div className={estilos.app}>
      <header className={estilos.barra}>
        <Link href="/inicio" className={estilos.marca} aria-label="BreakU, ir al inicio">
          <span className={estilos.logo}>
            <Logo tamano={20} />
          </span>
          <span className={estilos.nombre}>
            Break<span className={estilos.u}>U</span>
          </span>
        </Link>

        <nav className={estilos.navEscritorio} aria-label="Secciones">
          {SECCIONES.map(({ ruta, nombre }) => (
            <Link
              key={ruta}
              href={ruta}
              className={activa(ruta) ? estilos.enlaceActivo : estilos.enlace}
              aria-current={activa(ruta) ? "page" : undefined}
            >
              {activa(ruta) && <motion.span layoutId="indicador-escritorio" className={estilos.indicador} transition={RESORTE} />}
              <span className={estilos.textoEnlace}>{nombre}</span>
            </Link>
          ))}
        </nav>

        <div className={estilos.acciones}>
          <Notificaciones recarga={0} />
          <Link href="/perfil" className={estilos.avatar} aria-label={`Mi perfil, ${sesion.usuario.nombre}`}>
            {iniciales(sesion.usuario.nombre)}
          </Link>
        </div>
      </header>

      <div className={estilos.contenido}>{children}</div>

      <nav className={estilos.navInferior} aria-label="Secciones">
        {SECCIONES.map(({ ruta, corto, Icono }) => (
          <Link
            key={ruta}
            href={ruta}
            className={activa(ruta) ? estilos.itemActivo : estilos.item}
            aria-current={activa(ruta) ? "page" : undefined}
          >
            <span className={estilos.icono}>
              {activa(ruta) && <motion.span layoutId="indicador-movil" className={estilos.pastilla} transition={RESORTE} />}
              <Icono tamano={22} />
            </span>
            {corto}
          </Link>
        ))}
      </nav>
    </div>
  );
}

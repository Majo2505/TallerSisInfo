"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { Logo } from "@/components/auth/Iconos";
import Notificaciones from "@/components/notificaciones/Notificaciones";
import { useSesion } from "@/lib/useSesion";
import {
  IconoBrujula,
  IconoCalendario,
  IconoDestello,
  IconoInicio,
  IconoPersona,
} from "./Iconos";
import estilos from "./ShellApp.module.css";

const SECCIONES = [
  { ruta: "/inicio", nombre: "Inicio", Icono: IconoInicio },
  { ruta: "/horario", nombre: "Horario", Icono: IconoCalendario },
  { ruta: "/ia", nombre: "IA", Icono: IconoDestello },
  { ruta: "/campus", nombre: "Campus", Icono: IconoBrujula },
  { ruta: "/perfil", nombre: "Perfil", Icono: IconoPersona },
];

function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/).filter(Boolean);
  if (partes.length === 0) return "?";
  const letras = partes.length === 1 ? partes[0].slice(0, 2) : partes[0][0] + partes[1][0];
  return letras.toUpperCase();
}

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
              {nombre}
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
        {SECCIONES.map(({ ruta, nombre, Icono }) => (
          <Link
            key={ruta}
            href={ruta}
            className={activa(ruta) ? estilos.itemActivo : estilos.item}
            aria-current={activa(ruta) ? "page" : undefined}
          >
            <span className={estilos.icono}>
              <Icono tamano={22} />
            </span>
            {nombre}
          </Link>
        ))}
      </nav>
    </div>
  );
}

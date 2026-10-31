"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { motion } from "motion/react";
import { useEffect } from "react";
import { Logo } from "@/components/auth/Iconos";
import Notificaciones from "@/components/notificaciones/Notificaciones";
import { iniciales } from "@/lib/iniciales";
import { Sesion, cerrarSesion } from "@/services/authService";
import { IconoAjustes, IconoInicio, IconoSalir } from "./Iconos";
import estilos from "./ShellAdmin.module.css";

const RESORTE = { type: "spring", bounce: 0.18, duration: 0.45 } as const;

const SECCIONES = [
  { ruta: "/admin/inicio", nombre: "Inicio", Icono: IconoInicio },
  { ruta: "/admin", nombre: "Panel Admin", Icono: IconoAjustes },
];

/** Estructura del administrador: barra lateral en laptop y barra superior + inferior en celular. */
export default function ShellAdmin({ sesion, children }: { sesion: Sesion; children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  // "/admin" (el panel) se marca solo en su ruta exacta; si no, también se marcaría en "/admin/inicio".
  const activa = (ruta: string) => pathname === ruta || (ruta !== "/admin" && pathname.startsWith(`${ruta}/`));
  // Las pantallas del estudiante (horario, puentes, recomendaciones…) no aplican al administrador.
  const fueraDeAdmin = !pathname.startsWith("/admin");

  useEffect(() => {
    if (fueraDeAdmin) router.replace("/admin/inicio");
  }, [fueraDeAdmin, router]);

  function salir() {
    cerrarSesion();
    router.replace("/login");
  }

  return (
    <div className={estilos.app}>
      <aside className={estilos.lateral} aria-label="Administración">
        <Link href="/admin/inicio" className={estilos.marca} aria-label="BreakU, ir al inicio">
          <span className={estilos.logo}>
            <Logo tamano={20} />
          </span>
          <span className={estilos.nombre}>
            Break<span className={estilos.u}>U</span>
          </span>
        </Link>

        <nav className={estilos.nav}>
          {SECCIONES.map(({ ruta, nombre, Icono }) => (
            <Link
              key={ruta}
              href={ruta}
              className={activa(ruta) ? estilos.enlaceActivo : estilos.enlace}
              aria-current={activa(ruta) ? "page" : undefined}
            >
              {activa(ruta) && <motion.span layoutId="indicador-admin" className={estilos.indicador} transition={RESORTE} />}
              <span className={estilos.textoEnlace}>
                <Icono tamano={20} /> {nombre}
              </span>
            </Link>
          ))}
        </nav>

        <div className={estilos.pie}>
          <div className={estilos.filaAvisos}>
            <Notificaciones recarga={0} />
            <span>Notificaciones</span>
          </div>
          <div className={estilos.usuario}>
            <span className={estilos.avatar} aria-hidden="true">
              {iniciales(sesion.usuario.nombre)}
            </span>
            <span className={estilos.usuarioTexto}>
              <strong>{sesion.usuario.nombre}</strong>
              <small>Administrador</small>
            </span>
            <button type="button" className={estilos.salir} onClick={salir} aria-label="Cerrar sesión" title="Cerrar sesión">
              <IconoSalir tamano={18} />
            </button>
          </div>
        </div>
      </aside>

      {/* Celular: barra superior con marca, avisos y salir. */}
      <header className={estilos.barra}>
        <Link href="/admin/inicio" className={estilos.marca} aria-label="BreakU, ir al inicio">
          <span className={estilos.logo}>
            <Logo tamano={20} />
          </span>
          <span className={estilos.nombre}>
            Break<span className={estilos.u}>U</span>
          </span>
          <span className={estilos.etiqueta}>Admin</span>
        </Link>
        <div className={estilos.acciones}>
          <Notificaciones recarga={0} />
          <button type="button" className={estilos.salirMovil} onClick={salir} aria-label="Cerrar sesión">
            <IconoSalir tamano={18} />
          </button>
        </div>
      </header>

      <div className={estilos.contenido} aria-busy={fueraDeAdmin || undefined}>
        {fueraDeAdmin ? null : children}
      </div>

      <nav className={estilos.navInferior} aria-label="Administración">
        {SECCIONES.map(({ ruta, nombre, Icono }) => (
          <Link
            key={ruta}
            href={ruta}
            className={activa(ruta) ? estilos.itemActivo : estilos.item}
            aria-current={activa(ruta) ? "page" : undefined}
          >
            <span className={estilos.icono}>
              {activa(ruta) && <motion.span layoutId="indicador-admin-movil" className={estilos.pastilla} transition={RESORTE} />}
              <Icono tamano={22} />
            </span>
            {nombre}
          </Link>
        ))}
      </nav>
    </div>
  );
}

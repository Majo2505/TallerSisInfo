"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { IconoFlechaDerecha } from "@/components/app/Iconos";
import styles from "./Navegacion.module.css";

const ENLACES = [
  { href: "/#como-funciona", texto: "Cómo funciona" },
  { href: "/#funciones", texto: "Qué puedes hacer" },
  { href: "/#opiniones", texto: "Opiniones" },
];

type Props = {
  /** Sobre la foto de la portada empieza transparente; en las páginas legales, siempre sólida. */
  sobreFoto?: boolean;
};

export default function Navegacion({ sobreFoto = false }: Props) {
  const [flotante, setFlotante] = useState(!sobreFoto);

  useEffect(() => {
    if (!sobreFoto) return;
    const revisar = () => setFlotante(window.scrollY > 24);
    revisar();
    window.addEventListener("scroll", revisar, { passive: true });
    return () => window.removeEventListener("scroll", revisar);
  }, [sobreFoto]);

  return (
    <header className={styles.barra} data-flotante={flotante || undefined}>
      <nav className={styles.pildora} aria-label="Principal">
        <Link href="/" className={styles.marca} aria-label="BreakU, inicio">
          Break<span>U</span>
        </Link>

        <ul className={styles.enlaces}>
          {ENLACES.map((e) => (
            <li key={e.href}>
              <Link href={e.href} className={styles.enlace}>
                {e.texto}
              </Link>
            </li>
          ))}
        </ul>

        <div className={styles.acciones}>
          <Link href="/login" className={styles.entrar} aria-label="Iniciar sesión">
            <span className={styles.largo}>Iniciar sesión</span>
            <span className={styles.corto} aria-hidden="true">
              Entrar
            </span>
          </Link>
          <Link href="/login?modo=registro" className={styles.registro}>
            <span>Crear cuenta</span>
            <IconoFlechaDerecha tamano={16} />
          </Link>
        </div>
      </nav>
    </header>
  );
}

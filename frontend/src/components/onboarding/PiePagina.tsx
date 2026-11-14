import Link from "next/link";
import styles from "./PiePagina.module.css";

const COLUMNAS = [
  {
    titulo: "BreakU",
    enlaces: [
      { href: "/#como-funciona", texto: "Cómo funciona" },
      { href: "/#funciones", texto: "Qué puedes hacer" },
      { href: "/#opiniones", texto: "Opiniones" },
    ],
  },
  {
    titulo: "Tu cuenta",
    enlaces: [
      { href: "/login", texto: "Iniciar sesión" },
      { href: "/login?modo=registro", texto: "Crear cuenta" },
    ],
  },
  {
    titulo: "Legal",
    enlaces: [
      { href: "/normas", texto: "Normas de la comunidad" },
      { href: "/privacidad", texto: "Política de privacidad" },
      { href: "/terminos", texto: "Términos y condiciones" },
    ],
  },
];

export default function PiePagina() {
  return (
    <footer className={styles.pie}>
      <div className={styles.marco}>
        <div className={styles.presentacion}>
          <Link href="/" className={styles.marca}>
            Break<span>U</span>
          </Link>
          <p>Tu tiempo libre, bien aprovechado. Hecho por estudiantes de la UCB para estudiantes de la UCB.</p>
        </div>

        {COLUMNAS.map((c) => (
          <nav key={c.titulo} className={styles.columna} aria-label={c.titulo}>
            <h2>{c.titulo}</h2>
            <ul>
              {c.enlaces.map((e) => (
                <li key={e.href}>
                  <Link href={e.href}>{e.texto}</Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </div>

      <div className={styles.base}>
        <p>© 2026 BreakU · Universidad Católica Boliviana «San Pablo»</p>
        <p>Proyecto del Taller de Sistemas de Información (SIS-227)</p>
      </div>
    </footer>
  );
}

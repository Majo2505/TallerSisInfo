import Link from "next/link";
import Navegacion from "@/components/onboarding/Navegacion";
import PiePagina from "@/components/onboarding/PiePagina";
import styles from "./DocumentoLegal.module.css";

const DOCUMENTOS = [
  { href: "/normas", texto: "Normas de la comunidad" },
  { href: "/privacidad", texto: "Política de privacidad" },
  { href: "/terminos", texto: "Términos y condiciones" },
];

type Props = {
  ruta: string;
  titulo: string;
  resumen: string;
  actualizado: string;
  children: React.ReactNode;
};

/** Página de un documento legal: portada marina, texto en una hoja blanca y enlaces a los otros documentos. */
export default function DocumentoLegal({ ruta, titulo, resumen, actualizado, children }: Props) {
  return (
    <div className={styles.pagina}>
      <Navegacion />
      <main className={styles.principal}>
        <header className={styles.portada}>
          <div className={styles.portadaMarco}>
            <h1>{titulo}</h1>
            <p>{resumen}</p>
            <small>Última actualización: {actualizado}</small>
          </div>
        </header>

        <div className={styles.cuerpo}>
          <article className={styles.hoja}>{children}</article>

          <aside className={styles.otros} aria-label="Otros documentos">
            <h2>Otros documentos</h2>
            <ul>
              {DOCUMENTOS.filter((d) => d.href !== ruta).map((d) => (
                <li key={d.href}>
                  <Link href={d.href}>{d.texto}</Link>
                </li>
              ))}
            </ul>
            <p>¿Dudas o algo que corregir? Escríbenos desde Perfil → Reportar un problema.</p>
          </aside>
        </div>
      </main>
      <PiePagina />
    </div>
  );
}

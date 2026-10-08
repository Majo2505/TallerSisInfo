import { Candado, Logo, Personas, Rayo } from "./Iconos";
import styles from "./PanelMarca.module.css";

const BENEFICIOS = [
  {
    icono: <Rayo />,
    titulo: "Detecta tus puentes",
    texto: "Identifica bloques de tiempo libre entre tus clases automáticamente.",
  },
  {
    icono: <Candado />,
    titulo: "Lugares verificados · Sin alucinaciones",
    texto: "Motor RAG con catálogo curado. 100% respaldado por datos reales.",
  },
  {
    icono: <Personas />,
    titulo: "Comunidades efímeras",
    texto: "Conecta con compañeros que tienen el mismo tiempo libre.",
  },
];

export default function PanelMarca() {
  return (
    <aside className={styles.panel}>
      <div className={styles.circulo1} aria-hidden="true" />
      <div className={styles.circulo2} aria-hidden="true" />

      <div className={styles.marca}>
        <div className={styles.logo}>
          <Logo tamano={22} />
        </div>
        <span className={styles.nombre}>BreakU</span>
      </div>
      <p className={styles.lema}>Tu tiempo libre, bien aprovechado.</p>

      {/* En móvil los beneficios se ocultan para dejar el formulario a la vista. */}
      <ul className={styles.beneficios}>
        {BENEFICIOS.map((b) => (
          <li key={b.titulo} className={styles.beneficio}>
            <span className={styles.icono}>{b.icono}</span>
            <div>
              <strong>{b.titulo}</strong>
              <p>{b.texto}</p>
            </div>
          </li>
        ))}
      </ul>

      <p className={styles.pie}>© 2026 BreakU · Solo correos institucionales verificados</p>
    </aside>
  );
}

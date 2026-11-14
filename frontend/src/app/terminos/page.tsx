import type { Metadata } from "next";
import Link from "next/link";
import DocumentoLegal from "@/components/legal/DocumentoLegal";

export const metadata: Metadata = {
  title: "Términos y condiciones · BreakU",
  description: "Las reglas de uso de BreakU para estudiantes de la UCB.",
};

export default function Terminos() {
  return (
    <DocumentoLegal
      ruta="/terminos"
      titulo="Términos y condiciones"
      resumen="Las reglas para usar BreakU. Al crear tu cuenta, las aceptas."
      actualizado="14 de noviembre de 2026"
    >
      <aside>
        <strong>En corto:</strong> BreakU es un proyecto académico para estudiantes de la UCB. Las recomendaciones son
        sugerencias: la decisión final siempre es tuya.
      </aside>

      <h2>1. Qué es BreakU</h2>
      <p>
        BreakU es una aplicación web que detecta los ratos libres entre tus clases y te sugiere qué hacer con ellos en el
        campus. Es un proyecto académico desarrollado en la Universidad Católica Boliviana «San Pablo» y no reemplaza los
        canales oficiales de la universidad para horarios, avisos ni trámites.
      </p>

      <h2>2. Quién puede usarla</h2>
      <ul>
        <li>Estudiantes con un correo institucional @ucb.edu.bo activo.</li>
        <li>Una cuenta por persona. No la compartas ni uses la de otra persona.</li>
        <li>Eres responsable de cuidar tu contraseña y de lo que se haga con tu cuenta.</li>
      </ul>

      <h2>3. Tu horario</h2>
      <p>
        Los puentes se calculan con las clases que registras. Si tu horario no está completo o tiene errores, los avisos
        y las sugerencias también los tendrán. Revisa siempre lo que lee la función de foto antes de guardarlo.
      </p>

      <h2>4. Las recomendaciones</h2>
      <ul>
        <li>
          Las genera un asistente con inteligencia artificial, pero solo a partir de lugares de un catálogo revisado por
          el equipo de administración.
        </li>
        <li>
          Las distancias y los tiempos de ida y vuelta son estimados. Considera tu propio ritmo y los horarios de cada
          lugar.
        </li>
        <li>
          Si el asistente tarda demasiado, puedes recibir una sugerencia alternativa con los lugares más cercanos, sin
          explicación personalizada.
        </li>
        <li>Respeta las reglas de cada espacio del campus. La decisión de ir o no siempre es tuya.</li>
      </ul>

      <h2>5. Lo que publicas</h2>
      <p>
        Eres responsable de los grupos, eventos y reportes que creas. Todo debe cumplir las{" "}
        <Link href="/normas">Normas de la comunidad</Link>. Los eventos se revisan antes de publicarse y el equipo puede
        rechazarlos o retirarlos si no las cumplen.
      </p>

      <h2>6. Sanciones</h2>
      <p>
        Si no se cumplen estos términos o las normas, el equipo de administración puede aplicar una advertencia, una
        suspensión temporal o la baja definitiva de la cuenta, siempre con un motivo. Una cuenta suspendida o dada de
        baja no puede iniciar sesión mientras dure la sanción.
      </p>

      <h2>7. Disponibilidad</h2>
      <p>
        Como proyecto académico, BreakU puede tener interrupciones, cambios o funciones en prueba. Hacemos lo posible
        para que funcione bien, pero no podemos garantizar que esté disponible todo el tiempo ni libre de errores.
      </p>

      <h2>8. Tus datos</h2>
      <p>
        Cómo usamos y protegemos tu información está en la <Link href="/privacidad">Política de privacidad</Link>.
      </p>

      <h2>9. Cambios en estos términos</h2>
      <p>
        Podemos actualizar estos términos. Si el cambio es importante, te avisaremos dentro de la app. Seguir usando
        BreakU después del aviso significa que aceptas la nueva versión.
      </p>
    </DocumentoLegal>
  );
}

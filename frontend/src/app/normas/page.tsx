import type { Metadata } from "next";
import Link from "next/link";
import DocumentoLegal from "@/components/legal/DocumentoLegal";

export const metadata: Metadata = {
  title: "Normas de la comunidad · BreakU",
  description: "Cómo convivimos en los grupos, eventos y espacios compartidos de BreakU.",
};

export default function Normas() {
  return (
    <DocumentoLegal
      ruta="/normas"
      titulo="Normas de la comunidad"
      resumen="BreakU funciona porque la gente aparece cuando dice que va a ir y trata bien a los demás. Estas son las reglas para que siga así."
      actualizado="14 de noviembre de 2026"
    >
      <aside>
        <strong>En corto:</strong> respeta a los demás, publica cosas reales, cumple lo que confirmas y reporta lo que no
        está bien.
      </aside>

      <h2>1. Respeto ante todo</h2>
      <p>
        Los grupos y eventos juntan a personas que quizá no se conocen. No se permiten insultos, burlas, acoso,
        amenazas ni comentarios discriminatorios por origen, género, orientación, religión, discapacidad, apariencia o
        cualquier otra razón.
      </p>

      <h2>2. Actividades y eventos reales</h2>
      <ul>
        <li>Crea grupos para cosas que de verdad vas a hacer, en el lugar y la hora que indicas.</li>
        <li>No publiques eventos falsos, cadenas, ventas, rifas ni publicidad.</li>
        <li>
          Los eventos se revisan antes de salir. Si el equipo rechaza uno, te explicamos el motivo para que puedas
          corregirlo.
        </li>
      </ul>

      <h2>3. Dentro del reglamento de la UCB</h2>
      <p>
        Todo lo que se organiza con BreakU pasa en el campus o alrededor de la vida universitaria, así que sigue el
        reglamento de la universidad. No se permiten actividades con alcohol, sustancias, apuestas ni nada que ponga en
        riesgo a alguien. El asistente tampoco va a ayudarte a organizarlas.
      </p>

      <h2>4. Si confirmas, ve</h2>
      <p>
        Cada grupo tiene cupos. Cuando dices «Voy», ocupas un lugar que otra persona podría usar. Si cambias de planes,
        toca «Ya no voy» lo antes posible para liberar tu cupo.
      </p>

      <h2>5. Cuida la privacidad de los demás</h2>
      <ul>
        <li>No compartas fuera de la app los nombres, carreras u horarios de otras personas.</li>
        <li>No uses «Libres contigo» para seguir o incomodar a alguien.</li>
        <li>No hagas capturas para exponer o burlarte de otros.</li>
      </ul>

      <h2>6. Usa el asistente para lo que es</h2>
      <p>
        El asistente te ayuda con tu tiempo libre y tus actividades en el campus. Intentar engañarlo para que ignore sus
        reglas, revele información interna o recomiende algo fuera del catálogo se bloquea y queda registrado.
      </p>

      <h2>7. Reporta lo que no está bien</h2>
      <p>
        Si un grupo, un evento o un lugar no cumple estas normas, usa la opción de reportar. Cada reporte lo revisa una
        persona del equipo de administración, que puede responderte con lo que decidió.
      </p>

      <h2>8. Qué pasa si no se cumplen</h2>
      <p>Según la gravedad y si se repite, el equipo puede aplicar:</p>
      <ul>
        <li>
          <strong>Advertencia:</strong> un aviso con el motivo, sin perder acceso.
        </li>
        <li>
          <strong>Suspensión:</strong> no puedes entrar hasta una fecha determinada.
        </li>
        <li>
          <strong>Baja:</strong> la cuenta queda bloqueada de forma definitiva.
        </li>
      </ul>
      <p>
        Toda sanción indica el motivo. Si crees que hubo un error, escríbenos desde Perfil → Reportar un problema. Más
        detalles en los <Link href="/terminos">Términos y condiciones</Link>.
      </p>
    </DocumentoLegal>
  );
}

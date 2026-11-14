import type { Metadata } from "next";
import Link from "next/link";
import DocumentoLegal from "@/components/legal/DocumentoLegal";

export const metadata: Metadata = {
  title: "Política de privacidad · BreakU",
  description: "Qué datos usa BreakU, para qué, con quién se comparten y cómo puedes controlarlos.",
};

export default function Privacidad() {
  return (
    <DocumentoLegal
      ruta="/privacidad"
      titulo="Política de privacidad"
      resumen="Qué datos usamos, para qué, quién los ve y cómo los controlas. Sin letra chica."
      actualizado="14 de noviembre de 2026"
    >
      <aside>
        <strong>En corto:</strong> usamos tu horario para encontrar tus ratos libres. Tu ubicación solo con permiso, y
        guardamos el campus, nunca tus coordenadas. No vendemos tus datos.
      </aside>

      <h2>1. Qué datos guardamos</h2>
      <h3>Tu cuenta</h3>
      <ul>
        <li>Nombre, correo institucional (@ucb.edu.bo), carrera y semestre.</li>
        <li>Tu contraseña, siempre cifrada: nadie del equipo puede leerla.</li>
        <li>
          Si entras con Google, un identificador de tu cuenta de Google para reconocerte. Nunca recibimos tu contraseña de
          Google.
        </li>
      </ul>
      <h3>Tu horario</h3>
      <ul>
        <li>Tus clases: materia, día, hora, aula y si asistes o no.</li>
        <li>
          Si cargas tu horario con una foto, la imagen se usa solo para leer las clases y <strong>no se guarda</strong>.
        </li>
        <li>Los puentes (ratos libres) que calculamos a partir de tus clases.</li>
      </ul>
      <h3>Tu ubicación</h3>
      <ul>
        <li>Solo si das permiso. La usamos para saber en qué campus estás y sugerirte lugares cercanos.</li>
        <li>Guardamos el campus y la fecha en que diste el permiso, no tus coordenadas.</li>
        <li>También puedes elegir tu campus a mano, sin compartir tu ubicación.</li>
      </ul>
      <h3>Lo que haces en BreakU</h3>
      <ul>
        <li>
          Lo que le pides al asistente, el estado de ánimo que eliges, los lugares que te sugirió y si te sirvieron o no.
        </li>
        <li>Los grupos que creas o a los que te unes, los eventos que publicas y los reportes que envías.</li>
      </ul>

      <h2>2. Para qué los usamos</h2>
      <ul>
        <li>Detectar tus puentes y avisarte cuando empiezan.</li>
        <li>Recomendarte lugares del catálogo que entren en tu tiempo libre.</li>
        <li>Invitarte a grupos de compañeros de tu sede que estén libres a la misma hora.</li>
        <li>
          Auditar al asistente: cada recomendación queda registrada con los lugares que la respaldan, para comprobar que
          la IA solo sugiere sitios reales.
        </li>
        <li>Revisar reportes y mantener la convivencia según las <Link href="/normas">Normas de la comunidad</Link>.</li>
      </ul>

      <h2>3. Quién ve tus datos</h2>
      <ul>
        <li>
          <strong>Otros estudiantes</strong> ven tu nombre y carrera solo cuando participas en un grupo, y tu nombre,
          carrera, semestre y el tiempo libre que comparten en «Libres contigo», únicamente si activaste esa opción.
        </li>
        <li>
          <strong>El equipo de administración</strong> ve los reportes, los registros del asistente y las sanciones, para
          moderar y mejorar el servicio.
        </li>
        <li>
          <strong>El proveedor de inteligencia artificial</strong> recibe el texto de tus pedidos al asistente y, si la
          usas, la foto de tu horario, solo para responderte. No le enviamos tu correo ni tu contraseña.
        </li>
      </ul>
      <p>No vendemos ni alquilamos tus datos a nadie.</p>

      <h2>4. Tú tienes el control</h2>
      <ul>
        <li>Corrige tu nombre, carrera y semestre desde tu perfil.</li>
        <li>Revoca el permiso de ubicación cuando quieras.</li>
        <li>Activa o desactiva «Libres contigo» y los avisos.</li>
        <li>Cambia o crea tu contraseña desde Perfil → Seguridad.</li>
        <li>Para pedir que eliminemos tu cuenta y tus datos, escríbenos desde Perfil → Reportar un problema.</li>
      </ul>

      <h2>5. Cómo los protegemos</h2>
      <p>
        Las contraseñas se guardan cifradas, la sesión usa un token que vence y cada persona solo accede a lo que le
        corresponde según su rol. Los datos se guardan mientras tu cuenta exista.
      </p>

      <h2>6. Cambios en esta política</h2>
      <p>
        Si cambiamos algo importante, te avisaremos dentro de la app. La fecha de arriba indica la última versión.
        Consulta también los <Link href="/terminos">Términos y condiciones</Link>.
      </p>
    </DocumentoLegal>
  );
}

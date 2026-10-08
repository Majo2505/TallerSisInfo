import Link from "next/link";

// Pendiente: el inicio de sesión real es de HU1-FE (Amira). Esta página es solo un placeholder.
export default function Login() {
  return (
    <main style={{ padding: "2rem", textAlign: "center" }}>
      <h1>Inicio de sesión (pendiente)</h1>
      <p>
        <Link href="/?onboarding=1">Volver a ver la bienvenida</Link>
      </p>
    </main>
  );
}

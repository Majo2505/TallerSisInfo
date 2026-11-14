import LoginForm from "@/components/auth/LoginForm";
import PanelMarca from "@/components/auth/PanelMarca";
import styles from "./login.module.css";

type Props = {
  searchParams: Promise<{ modo?: string }>;
};

export default async function Login({ searchParams }: Props) {
  const { modo } = await searchParams;
  return (
    <div className={styles.pagina}>
      <PanelMarca />
      <main className={styles.principal}>
        <LoginForm modoInicial={modo === "registro" ? "registro" : "login"} />
      </main>
    </div>
  );
}

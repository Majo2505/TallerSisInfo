import LoginForm from "@/components/auth/LoginForm";
import PanelMarca from "@/components/auth/PanelMarca";
import styles from "./login.module.css";

export default function Login() {
  return (
    <div className={styles.pagina}>
      <PanelMarca />
      <main className={styles.principal}>
        <LoginForm />
      </main>
    </div>
  );
}

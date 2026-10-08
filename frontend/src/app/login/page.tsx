<<<<<<< HEAD
import React from 'react';
import { AuthPage } from '@/components/AuthPage/AuthPage';

export default function LoginPage() {
  return <AuthPage />;
}
=======
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
>>>>>>> 000ed75a925299e9c9cbfd531f5a9bc966e8f0c0

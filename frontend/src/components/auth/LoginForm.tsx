"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState, useSyncExternalStore } from "react";
import { ErrorAuth, iniciarSesion, obtenerSesion, suscribirSesion } from "@/services/authService";
import { Ojo, OjoTachado } from "./Iconos";
import styles from "./LoginForm.module.css";

const RUTA_DESTINO = "/horario";
const FORMATO_CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

type Errores = { correo?: string; password?: string };

function validar(correo: string, password: string): Errores {
  const errores: Errores = {};
  if (!correo.trim()) errores.correo = "Ingresa tu correo institucional.";
  else if (!FORMATO_CORREO.test(correo.trim())) errores.correo = "Escribe un correo con formato válido.";
  // Pendiente: validar el dominio @ucb.edu.bo cuando el contrato y el backend lo definan (HU1).
  if (!password) errores.password = "Ingresa tu contraseña.";
  return errores;
}

export default function LoginForm() {
  const router = useRouter();
  const sesion = useSyncExternalStore(suscribirSesion, obtenerSesion, () => null);
  const [correo, setCorreo] = useState("");
  const [password, setPassword] = useState("");
  const [verPassword, setVerPassword] = useState(false);
  const [errores, setErrores] = useState<Errores>({});
  const [errorServidor, setErrorServidor] = useState("");
  const [enviando, setEnviando] = useState(false);

  // Si ya hay sesión, se va directo al horario.
  useEffect(() => {
    if (sesion) router.replace(RUTA_DESTINO);
  }, [sesion, router]);

  async function alEnviar(evento: React.FormEvent) {
    evento.preventDefault();
    setErrorServidor("");
    const encontrados = validar(correo, password);
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) return;

    setEnviando(true);
    try {
      await iniciarSesion(correo.trim(), password);
      router.replace(RUTA_DESTINO);
    } catch (e) {
      setErrorServidor(e instanceof ErrorAuth ? e.message : "Ocurrió un error inesperado. Inténtalo de nuevo.");
      setEnviando(false);
    }
  }

  return (
    <div className={styles.contenedor}>
      <div className={styles.pestanas} role="tablist" aria-label="Acceso">
        <button type="button" role="tab" aria-selected="true" className={styles.pestanaActiva}>
          Iniciar sesión
        </button>
        {/* Pendiente: el registro de usuarios no está definido para el sprint 1 (docs/api-contract.md). */}
        <button
          type="button"
          role="tab"
          aria-selected="false"
          aria-disabled="true"
          disabled
          title="Próximamente"
          className={styles.pestana}
        >
          Registrarse
        </button>
      </div>

      <form className={styles.formulario} onSubmit={alEnviar} noValidate>
        {errorServidor && (
          <p className={styles.aviso} role="alert">
            {errorServidor}
          </p>
        )}

        <div className={styles.campo}>
          <label htmlFor="correo">Correo institucional</label>
          <input
            id="correo"
            type="email"
            autoComplete="username"
            placeholder="usuario@universidad.edu"
            value={correo}
            onChange={(e) => setCorreo(e.target.value)}
            aria-invalid={errores.correo ? true : undefined}
            aria-describedby={errores.correo ? "error-correo" : undefined}
          />
          {errores.correo && (
            <span id="error-correo" className={styles.errorCampo}>
              {errores.correo}
            </span>
          )}
        </div>

        <div className={styles.campo}>
          <label htmlFor="password">Contraseña</label>
          <div className={styles.conBoton}>
            <input
              id="password"
              type={verPassword ? "text" : "password"}
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-invalid={errores.password ? true : undefined}
              aria-describedby={errores.password ? "error-password" : undefined}
            />
            <button
              type="button"
              className={styles.ojo}
              onClick={() => setVerPassword((v) => !v)}
              aria-label={verPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
              aria-pressed={verPassword}
            >
              {verPassword ? <OjoTachado /> : <Ojo />}
            </button>
          </div>
          {errores.password && (
            <span id="error-password" className={styles.errorCampo}>
              {errores.password}
            </span>
          )}
        </div>

        {/* Pendiente (fuera de alcance, sin backend): "¿Olvidaste tu contraseña?" y el acceso rápido Estudiante / Admin del prototipo. */}

        <button type="submit" className={styles.boton} disabled={enviando}>
          {enviando ? "Ingresando…" : "Iniciar sesión"}
        </button>

        <p className={styles.pie}>Acceso restringido a correos institucionales verificados · JWT</p>
      </form>
    </div>
  );
}

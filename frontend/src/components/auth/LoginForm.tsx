"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState, useSyncExternalStore } from "react";
import { ErrorAuth, iniciarSesion, obtenerSesion, registrarse, suscribirSesion } from "@/services/authService";
import { Ojo, OjoTachado } from "./Iconos";
import styles from "./LoginForm.module.css";

const RUTA_DESTINO = "/horario";
const FORMATO_CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const DOMINIO_INSTITUCIONAL = "@ucb.edu.bo";
// Pendiente: el backend no define reglas de contraseña; este mínimo es solo del formulario.
const LARGO_MINIMO_PASSWORD = 8;

type Modo = "login" | "registro";
type Errores = {
  nombre?: string;
  correo?: string;
  password?: string;
  confirmar?: string;
  terminos?: string;
};

function validar(
  modo: Modo,
  datos: { nombre: string; correo: string; password: string; confirmar: string; terminos: boolean },
): Errores {
  const errores: Errores = {};
  const correo = datos.correo.trim();

  if (modo === "registro" && !datos.nombre.trim()) errores.nombre = "Ingresa tu nombre completo.";

  if (!correo) errores.correo = "Ingresa tu correo institucional.";
  else if (!FORMATO_CORREO.test(correo)) errores.correo = "Escribe un correo con formato válido.";
  else if (!correo.toLowerCase().endsWith(DOMINIO_INSTITUCIONAL))
    errores.correo = "Solo se permite el acceso con un correo institucional (@ucb.edu.bo)";

  if (!datos.password) errores.password = "Ingresa tu contraseña.";
  else if (modo === "registro" && datos.password.length < LARGO_MINIMO_PASSWORD)
    errores.password = `Tu contraseña debe tener al menos ${LARGO_MINIMO_PASSWORD} caracteres.`;

  if (modo === "registro") {
    if (!datos.confirmar) errores.confirmar = "Confirma tu contraseña.";
    else if (datos.confirmar !== datos.password) errores.confirmar = "Las contraseñas no coinciden.";
    if (!datos.terminos) errores.terminos = "Debes aceptar los términos de uso.";
  }
  return errores;
}

type PropsCampoPassword = {
  id: string;
  etiqueta: string;
  autoComplete: string;
  valor: string;
  alCambiar: (valor: string) => void;
  error?: string;
};

function CampoPassword({ id, etiqueta, autoComplete, valor, alCambiar, error }: PropsCampoPassword) {
  const [ver, setVer] = useState(false);
  return (
    <div className={styles.campo}>
      <label htmlFor={id}>{etiqueta}</label>
      <div className={styles.conBoton}>
        <input
          id={id}
          type={ver ? "text" : "password"}
          autoComplete={autoComplete}
          value={valor}
          onChange={(e) => alCambiar(e.target.value)}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `error-${id}` : undefined}
        />
        <button
          type="button"
          className={styles.ojo}
          onClick={() => setVer((v) => !v)}
          aria-label={ver ? "Ocultar contraseña" : "Mostrar contraseña"}
          aria-pressed={ver}
        >
          {ver ? <OjoTachado /> : <Ojo />}
        </button>
      </div>
      {error && (
        <span id={`error-${id}`} className={styles.errorCampo}>
          {error}
        </span>
      )}
    </div>
  );
}

export default function LoginForm() {
  const router = useRouter();
  const sesion = useSyncExternalStore(suscribirSesion, obtenerSesion, () => null);
  const [modo, setModo] = useState<Modo>("login");
  const [nombre, setNombre] = useState("");
  const [correo, setCorreo] = useState("");
  const [password, setPassword] = useState("");
  const [confirmar, setConfirmar] = useState("");
  const [terminos, setTerminos] = useState(false);
  const [errores, setErrores] = useState<Errores>({});
  const [errorServidor, setErrorServidor] = useState("");
  const [enviando, setEnviando] = useState(false);

  const esRegistro = modo === "registro";

  // Si ya hay sesión, se va directo al horario.
  useEffect(() => {
    if (sesion) router.replace(RUTA_DESTINO);
  }, [sesion, router]);

  function cambiarModo(nuevo: Modo) {
    setModo(nuevo);
    setErrores({});
    setErrorServidor("");
  }

  async function alEnviar(evento: React.FormEvent) {
    evento.preventDefault();
    setErrorServidor("");
    const encontrados = validar(modo, { nombre, correo, password, confirmar, terminos });
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) return;

    setEnviando(true);
    try {
      if (esRegistro) await registrarse(nombre.trim(), correo.trim(), password);
      else await iniciarSesion(correo.trim(), password);
      router.replace(RUTA_DESTINO);
    } catch (e) {
      setErrorServidor(e instanceof ErrorAuth ? e.message : "Ocurrió un error inesperado. Inténtalo de nuevo.");
      setEnviando(false);
    }
  }

  return (
    <div className={styles.contenedor}>
      <div className={styles.pestanas} role="tablist" aria-label="Acceso">
        <button
          type="button"
          role="tab"
          aria-selected={!esRegistro}
          className={!esRegistro ? styles.pestanaActiva : styles.pestana}
          onClick={() => cambiarModo("login")}
        >
          Iniciar sesión
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={esRegistro}
          className={esRegistro ? styles.pestanaActiva : styles.pestana}
          onClick={() => cambiarModo("registro")}
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

        {esRegistro && (
          <div className={styles.campo}>
            <label htmlFor="nombre">Nombre completo</label>
            <input
              id="nombre"
              type="text"
              autoComplete="name"
              placeholder="María Fernández"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              aria-invalid={errores.nombre ? true : undefined}
              aria-describedby={errores.nombre ? "error-nombre" : undefined}
            />
            {errores.nombre && (
              <span id="error-nombre" className={styles.errorCampo}>
                {errores.nombre}
              </span>
            )}
          </div>
        )}

        <div className={styles.campo}>
          <label htmlFor="correo">Correo institucional</label>
          <input
            id="correo"
            type="email"
            autoComplete="username"
            placeholder="usuario@ucb.edu.bo"
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

        <CampoPassword
          id="password"
          etiqueta="Contraseña"
          autoComplete={esRegistro ? "new-password" : "current-password"}
          valor={password}
          alCambiar={setPassword}
          error={errores.password}
        />

        {esRegistro && (
          <>
            <CampoPassword
              id="confirmar"
              etiqueta="Confirmar contraseña"
              autoComplete="new-password"
              valor={confirmar}
              alCambiar={setConfirmar}
              error={errores.confirmar}
            />

            <div className={styles.campo}>
              <div className={styles.terminos}>
                <input
                  id="terminos"
                  type="checkbox"
                  checked={terminos}
                  onChange={(e) => setTerminos(e.target.checked)}
                  aria-invalid={errores.terminos ? true : undefined}
                  aria-describedby={errores.terminos ? "error-terminos" : undefined}
                />
                {/* Pendiente: aún no existe una página de términos de uso a la que enlazar. */}
                <label htmlFor="terminos">
                  Acepto los <strong>términos de uso</strong> y entiendo que BREAKU solo utiliza mi ubicación para
                  generar recomendaciones activas, nunca la almacena ni la comparte.
                </label>
              </div>
              {errores.terminos && (
                <span id="error-terminos" className={styles.errorCampo}>
                  {errores.terminos}
                </span>
              )}
            </div>
          </>
        )}

        {/* Pendiente (fuera de alcance, sin backend): "¿Olvidaste tu contraseña?" y el acceso rápido Estudiante / Admin del prototipo. */}

        <button type="submit" className={styles.boton} disabled={enviando}>
          {enviando ? "Ingresando…" : esRegistro ? "Crear cuenta" : "Iniciar sesión"}
        </button>

        <p className={styles.pie}>Acceso restringido a correos institucionales verificados · JWT</p>
      </form>
    </div>
  );
}

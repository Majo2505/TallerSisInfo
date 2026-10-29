"use client";

import { useRouter } from "next/navigation";
import { motion, useReducedMotion } from "motion/react";
import { useEffect, useState, useSyncExternalStore } from "react";
import { RESORTE_TOQUE } from "@/components/app/Movimiento";
import {
  ErrorAuth,
  iniciarSesion,
  iniciarSesionAdmin,
  obtenerSesion,
  registrarse,
  rutaDeEntrada,
  suscribirSesion,
} from "@/services/authService";
import { Ojo, OjoTachado } from "./Iconos";
import styles from "./LoginForm.module.css";

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
    errores.password = `Tu contraseña tiene que tener al menos ${LARGO_MINIMO_PASSWORD} caracteres.`;

  if (modo === "registro") {
    if (!datos.confirmar) errores.confirmar = "Confirma tu contraseña.";
    else if (datos.confirmar !== datos.password) errores.confirmar = "Las contraseñas no coinciden.";
    if (!datos.terminos) errores.terminos = "Tienes que aceptar los términos de uso.";
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

function IconoEscudo() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M12 3 5 6v5c0 4.5 3 8.3 7 10 4-1.7 7-5.5 7-10V6l-7-3Z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
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
  const [aviso, setAviso] = useState("");
  const [comoAdmin, setComoAdmin] = useState(false);

  const esRegistro = modo === "registro";

  // Si ya hay sesión, se va directo a la entrada de su rol (estudiante: Inicio; administrador: el panel).
  useEffect(() => {
    if (sesion) router.replace(rutaDeEntrada(sesion));
  }, [sesion, router]);

  function cambiarModo(nuevo: Modo) {
    setModo(nuevo);
    setErrores({});
    setErrorServidor("");
    setAviso("");
  }


  async function alEnviar(evento: React.FormEvent) {
    evento.preventDefault();
    setErrorServidor("");
    const encontrados = validar(modo, { nombre, correo, password, confirmar, terminos });
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) return;

    setEnviando(true);
    try {
      let nueva;
      if (esRegistro) nueva = await registrarse(nombre.trim(), correo.trim(), password);
      else if (comoAdmin) nueva = await iniciarSesionAdmin(correo.trim(), password);
      else nueva = await iniciarSesion(correo.trim(), password);
      router.replace(rutaDeEntrada(nueva));
    } catch (e) {
      setErrorServidor(e instanceof ErrorAuth ? e.message : "Hubo un error inesperado. Inténtalo de nuevo.");
      setEnviando(false);
    }
  }

  const reducir = useReducedMotion();
  let textoBoton = esRegistro ? "Crear cuenta" : "Iniciar sesión";
  if (comoAdmin) textoBoton = "Ingresar al panel";
  if (enviando) textoBoton = "Ingresando…";

  return (
    <div className={styles.contenedor}>
      {comoAdmin ? (
        <motion.div
          className={styles.accesoAdmin}
          initial={reducir ? false : { opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
        >
          <span className={styles.escudo} aria-hidden="true">
            <IconoEscudo />
          </span>
          <div>
            <strong>Acceso de administración</strong>
            <small>Solo para cuentas con rol de administrador del campus.</small>
          </div>
        </motion.div>
      ) : (
      <div className={styles.pestanas} role="tablist" aria-label="Acceso">
        <button
          type="button"
          role="tab"
          aria-selected={!esRegistro}
          className={!esRegistro ? styles.pestanaActiva : styles.pestana}
          onClick={() => cambiarModo("login")}
        >
          {!esRegistro && <motion.span layoutId="pestana-acceso" className={styles.pastilla} transition={RESORTE_TOQUE} />}
          <span className={styles.textoPestana}>Iniciar sesión</span>
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={esRegistro}
          className={esRegistro ? styles.pestanaActiva : styles.pestana}
          onClick={() => cambiarModo("registro")}
        >
          {esRegistro && <motion.span layoutId="pestana-acceso" className={styles.pastilla} transition={RESORTE_TOQUE} />}
          <span className={styles.textoPestana}>Registrarse</span>
        </button>
      </div>
      )}

      <form className={styles.formulario} onSubmit={alEnviar} noValidate>
        {errorServidor && (
          // El error "tiembla" al aparecer: se nota sin tener que leerlo primero.
          <motion.p
            key={errorServidor}
            className={styles.aviso}
            role="alert"
            initial={reducir ? false : { x: 0 }}
            animate={{ x: [0, -8, 8, -5, 5, 0] }}
            transition={{ duration: 0.42 }}
          >
            {errorServidor}
          </motion.p>
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

        {!esRegistro && (
          <button
            type="button"
            className={styles.olvido}
            onClick={() => setAviso("Recuperar la contraseña va a estar disponible pronto.")}
          >
            ¿Olvidaste tu contraseña?
          </button>
        )}

        {aviso && (
          <p className={styles.aviso} role="status">
            {aviso}
          </p>
        )}

        <motion.button
          type="submit"
          className={styles.boton}
          disabled={enviando}
          whileTap={enviando ? undefined : { scale: 0.97 }}
          transition={RESORTE_TOQUE}
        >
          {enviando && <span className={styles.girando} aria-hidden="true" />}
          {textoBoton}
        </motion.button>

        <p className={styles.pie}>Solo con tu correo institucional @ucb.edu.bo</p>
      </form>

      {!esRegistro && (
        <button
          type="button"
          className={styles.cambiarAcceso}
          onClick={() => {
            setComoAdmin((v) => !v);
            setErrorServidor("");
            setErrores({});
          }}
        >
          {comoAdmin ? "← Volver al acceso de estudiantes" : "¿Administras el campus? Ingresa como administrador"}
        </button>
      )}
    </div>
  );
}

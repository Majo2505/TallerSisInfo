"use client";

import Image from "next/image";
import Link from "next/link";
import { motion, useReducedMotion } from "motion/react";
import { useState } from "react";
import { IconoFlechaDerecha } from "@/components/app/Iconos";
import { SALIDA } from "@/components/app/Movimiento";
import fotoCafeteria from "./fotos/cafeteria.webp";
import { Estrellas, Icono, type NombreIcono } from "./Iconos";
import styles from "./Secciones.module.css";

/** Aparece al entrar en pantalla, una sola vez. */
function useAparecer() {
  const reducir = useReducedMotion();
  return (retraso = 0) =>
    reducir
      ? {}
      : {
          initial: { opacity: 0, y: 24 },
          whileInView: { opacity: 1, y: 0 },
          viewport: { once: true, margin: "-80px" },
          transition: { duration: 0.7, ease: SALIDA, delay: retraso },
        };
}

/* ---------- Promesas: la tarjeta blanca que se monta sobre la foto ---------- */

const PROMESAS: { icono: NombreIcono; titulo: string; texto: string }[] = [
  {
    icono: "ubicacion",
    titulo: "Tu ubicación, solo con permiso",
    texto: "Guardamos en qué campus estás, nunca tus coordenadas. Lo puedes revocar cuando quieras.",
  },
  {
    icono: "escudo",
    titulo: "Lugares que existen de verdad",
    texto: "La IA solo sugiere sitios del catálogo que revisa la universidad. Si no está ahí, no aparece.",
  },
  {
    icono: "campana",
    titulo: "Avisos que valen la pena",
    texto: "Te escribimos cuando arranca tu tiempo libre o cambia algo en tus grupos. Nada más.",
  },
];

export function Promesas() {
  const aparecer = useAparecer();
  return (
    <section className={styles.promesas} aria-label="Lo que te prometemos">
      <motion.ul className={styles.promesasTarjeta} {...aparecer(0.1)}>
        {PROMESAS.map((p) => (
          <li key={p.titulo} className={styles.promesa}>
            <span className={styles.promesaIcono}>
              <Icono nombre={p.icono} tamano={22} />
            </span>
            <h2>{p.titulo}</h2>
            <p>{p.texto}</p>
          </li>
        ))}
      </motion.ul>
    </section>
  );
}

/* ---------- Cómo funciona: tres tarjetas que se abren en abanico ---------- */

function MaquetaHorario() {
  return (
    <div className={styles.maquetaHorario} aria-hidden="true">
      <span className={styles.chipFoto}>
        <Icono nombre="camara" tamano={14} /> Leído desde tu foto
      </span>
      {[
        ["07:15", "Cálculo II", "B-204"],
        ["09:30", "Programación III", "Lab. 3"],
        ["14:00", "Física General", "A-101"],
      ].map(([hora, materia, aula]) => (
        <span key={hora} className={styles.clase}>
          <b>{hora}</b>
          <span>{materia}</span>
          <small>{aula}</small>
        </span>
      ))}
    </div>
  );
}

function MaquetaPuente() {
  return (
    <div className={styles.maquetaPuente} aria-hidden="true">
      <div className={styles.linea}>
        <span className={styles.bloque} style={{ flex: 2 }}>
          Clase
        </span>
        <span className={styles.hueco} style={{ flex: 2.4 }}>
          1 h 15 min
        </span>
        <span className={styles.bloque} style={{ flex: 1.6 }}>
          Clase
        </span>
      </div>
      <div className={styles.aviso}>
        <span className={styles.avisoIcono}>
          <Icono nombre="rayo" tamano={16} />
        </span>
        <span>
          <b>¡Empezó tu puente!</b>
          <small>Libre hasta las 11:45</small>
        </span>
      </div>
    </div>
  );
}

function MaquetaAnimo() {
  return (
    <div className={styles.maquetaAnimo} aria-hidden="true">
      <div className={styles.animos}>
        {["Cansado", "Enfocado", "Energía", "Social"].map((a) => (
          <span key={a} className={styles.animo} data-activo={a === "Enfocado" || undefined}>
            {a}
          </span>
        ))}
      </div>
      <div className={styles.lugar}>
        <span className={styles.lugarIcono}>
          <Icono nombre="ubicacion" tamano={16} />
        </span>
        <span>
          <b>Biblioteca, sala silenciosa</b>
          <small>3 min a pie · cabe en tu puente</small>
        </span>
      </div>
    </div>
  );
}

const PASOS = [
  {
    titulo: "Sube tu horario",
    texto: "Escríbelo materia por materia o tómale una foto al impreso: la IA lo lee y tú solo confirmas.",
    maqueta: <MaquetaHorario />,
    giro: -5,
  },
  {
    titulo: "Descubre tus puentes",
    texto: "Cada hueco de 45 minutos o más queda marcado en tu semana, con cuánto dura y a qué hora termina.",
    maqueta: <MaquetaPuente />,
    giro: 0,
  },
  {
    titulo: "Elige qué hacer",
    texto: "Cuéntanos cómo llegas y te sugerimos lugares que entran en tu tiempo, con lo que tardas en ir y volver.",
    maqueta: <MaquetaAnimo />,
    giro: 5,
  },
];

export function ComoFunciona() {
  const reducir = useReducedMotion();
  const aparecer = useAparecer();
  // Las tarjetas entran juntas y se abren en abanico (la inclinación es CSS, más suave en el celular).
  const [abierto, setAbierto] = useState(false);
  return (
    <section id="como-funciona" className={styles.seccion} aria-labelledby="titulo-como">
      <motion.header className={styles.cabecera} {...aparecer()}>
        <h2 id="titulo-como" className={styles.tituloSeccion}>
          Cómo funciona
        </h2>
        <p className={styles.bajadaSeccion}>De tu horario a un plan, en menos de lo que dura el recreo.</p>
      </motion.header>

      <motion.ol
        className={styles.abanico}
        data-abierto={abierto || reducir || undefined}
        onViewportEnter={() => setAbierto(true)}
        viewport={{ once: true, margin: "-160px" }}
      >
        {PASOS.map((p, i) => (
          <motion.li
            key={p.titulo}
            className={styles.paso}
            data-destacado={i === 1 || undefined}
            style={{ "--giro": `${p.giro}deg` } as React.CSSProperties}
            initial={reducir ? false : { opacity: 0, x: (1 - i) * 110, y: 40 }}
            whileInView={{ opacity: 1, x: 0, y: 0 }}
            viewport={{ once: true, margin: "-120px" }}
            transition={{ type: "spring", duration: 0.9, bounce: 0.18, delay: 0.06 * i }}
          >
            <h3>{p.titulo}</h3>
            <p>{p.texto}</p>
            {p.maqueta}
          </motion.li>
        ))}
      </motion.ol>
    </section>
  );
}

/* ---------- Qué puedes hacer: lista y celular ---------- */

const FUNCIONES: { icono: NombreIcono; titulo: string; texto: string }[] = [
  {
    icono: "grupo",
    titulo: "Grupos que se arman solos",
    texto: "Propón «repaso de Física a las 10» y BreakU invita a compañeros de tu sede que estén libres a esa hora.",
  },
  {
    icono: "coincidencia",
    titulo: "Libres contigo",
    texto: "Mira quién comparte tu hueco para almorzar o estudiar juntos. Solo apareces si tú lo activas.",
  },
  {
    icono: "evento",
    titulo: "Eventos del campus",
    texto: "Charlas, torneos y ferias publicados por la comunidad y revisados antes de salir.",
  },
  {
    icono: "enVivo",
    titulo: "Todo al instante",
    texto: "Cupos, invitaciones y cambios se actualizan en vivo, sin recargar la página.",
  },
];

function Celular() {
  return (
    <div className={styles.celular} aria-hidden="true">
      <div className={styles.pantallaCelular}>
        <div className={styles.isla} />
        <p className={styles.saludo}>Hola, Andrés</p>
        <p className={styles.saludoTitulo}>Hoy tienes 2 puentes</p>

        <div className={styles.tarjetaPuente}>
          <small>Empieza en 12 min</small>
          <b>10:30 – 11:45</b>
          <span className={styles.barraPuente}>
            <span />
          </span>
        </div>

        <p className={styles.subtituloCelular}>Grupos en tu sede</p>
        <div className={styles.grupo}>
          <span className={styles.grupoIcono}>
            <Icono nombre="rayo" tamano={16} />
          </span>
          <span>
            <b>Fulbito en la cancha</b>
            <small>Hoy 10:40 · 6 de 10 van</small>
          </span>
          <span className={styles.unirme}>Unirme</span>
        </div>
        <div className={styles.grupo}>
          <span className={styles.grupoIcono} data-tono="celeste">
            <Icono nombre="calendario" tamano={16} />
          </span>
          <span>
            <b>Repaso de Física</b>
            <small>Hoy 11:00 · 3 de 6 van</small>
          </span>
          <span className={styles.unirme}>Unirme</span>
        </div>

        <div className={styles.libres}>
          <span className={styles.caritas}>
            <span>CM</span>
            <span>JR</span>
            <span>LP</span>
          </span>
          <small>3 compañeros libres contigo</small>
        </div>
      </div>
    </div>
  );
}

export function Funciones() {
  const aparecer = useAparecer();
  return (
    <section id="funciones" className={styles.seccionFunciones} aria-labelledby="titulo-funciones">
      <div className={styles.funcionesMarco}>
        <div className={styles.funcionesTexto}>
          <motion.h2 id="titulo-funciones" className={styles.tituloSeccion} {...aparecer()}>
            Tu tiempo libre, en buena compañía
          </motion.h2>
          <ul className={styles.listaFunciones}>
            {FUNCIONES.map((f, i) => (
              <motion.li key={f.titulo} className={styles.funcion} {...aparecer(0.08 * i)}>
                <span className={styles.funcionIcono}>
                  <Icono nombre={f.icono} tamano={22} />
                </span>
                <span>
                  <h3>{f.titulo}</h3>
                  <p>{f.texto}</p>
                </span>
              </motion.li>
            ))}
          </ul>
        </div>
        <motion.div className={styles.funcionesCelular} {...aparecer(0.15)}>
          <Celular />
        </motion.div>
      </div>
    </section>
  );
}

/* ---------- Opiniones ---------- */

const OPINIONES = [
  {
    nombre: "Andrea Choque",
    carrera: "Psicología · 3.er semestre",
    iniciales: "AC",
    tono: "amarillo",
    texto:
      "Pensé que era otra app de horarios, pero el aviso de «empezó tu puente» me cambió la rutina: ahora almuerzo tranquila y no corriendo entre una clase y otra.",
  },
  {
    nombre: "Rodrigo Saavedra",
    carrera: "Ing. Industrial · 7.º semestre",
    iniciales: "RS",
    tono: "celeste",
    texto:
      "Armé un partido para el viernes a mediodía y en un rato ya éramos ocho. Antes eso era un grupo de WhatsApp eterno donde nadie confirmaba.",
  },
  {
    nombre: "Fernanda López",
    carrera: "Arquitectura · 1.er semestre",
    iniciales: "FL",
    tono: "turquesa",
    texto:
      "Recién llegada no conocía nada del campus. Me sugirió una sala de estudio que ni sabía que existía y ahora es mi rincón para entregar maquetas.",
  },
];

export function Opiniones() {
  const aparecer = useAparecer();
  return (
    <section id="opiniones" className={styles.seccion} aria-labelledby="titulo-opiniones">
      <motion.header className={styles.cabecera} {...aparecer()}>
        <h2 id="titulo-opiniones" className={styles.tituloSeccion}>
          Lo que se dice en los pasillos
        </h2>
      </motion.header>

      <div className={styles.opiniones}>
        <motion.div className={styles.opinionesFoto} {...aparecer(0.05)}>
          <Image
            src={fotoCafeteria}
            alt="Cuatro estudiantes conversan y ríen alrededor de una mesa de la cafetería, con laptops y salteñas."
            fill
            placeholder="blur"
            sizes="(max-width: 1300px) 100vw, 1240px"
            className={styles.opinionesImagen}
          />
        </motion.div>

        <ul className={styles.tarjetasOpinion}>
          {OPINIONES.map((o, i) => (
            <motion.li key={o.nombre} className={styles.opinion} {...aparecer(0.1 + 0.08 * i)}>
              <figure>
                <span className={styles.estrellas} role="img" aria-label="5 de 5 estrellas">
                  <Estrellas tamano={20} />
                </span>
                <blockquote>{o.texto}</blockquote>
                <figcaption>
                  <span className={styles.iniciales} data-tono={o.tono} aria-hidden="true">
                    {o.iniciales}
                  </span>
                  <span>
                    <b>{o.nombre}</b>
                    <small>{o.carrera}</small>
                  </span>
                </figcaption>
              </figure>
            </motion.li>
          ))}
        </ul>
      </div>
    </section>
  );
}

/* ---------- Llamado final ---------- */

export function Llamado() {
  const aparecer = useAparecer();
  return (
    <section className={styles.llamado} aria-labelledby="titulo-llamado">
      <motion.div className={styles.llamadoCaja} {...aparecer()}>
        <h2 id="titulo-llamado">Tu próximo puente ya está en camino.</h2>
        <p>Entra con tu correo @ucb.edu.bo y deja lista tu semana en un par de minutos.</p>
        <div className={styles.llamadoBotones}>
          <Link href="/login?modo=registro" className={styles.llamadoPrimario}>
            <span>Crear mi cuenta</span>
            <IconoFlechaDerecha tamano={18} />
          </Link>
          <Link href="/login" className={styles.llamadoSecundario}>
            Ya tengo cuenta
          </Link>
        </div>
      </motion.div>
    </section>
  );
}

"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AvisoFlotante, useAviso } from "@/components/app/Aviso";
import comun from "@/components/app/Comun.module.css";
import {
  IconoAjustes,
  IconoBandera,
  IconoCalendario,
  IconoCandado,
  IconoCheck,
  IconoPersona,
  IconoSalir,
} from "@/components/app/Iconos";
import { guardarConfiguracion, obtenerConfiguracion } from "@/lib/notificaciones";
import { CARRERAS, SEMESTRES } from "@/lib/maqueta";
import { cerrarSesion } from "@/services/authService";
import { useSesion } from "@/lib/useSesion";
import estilos from "./perfil.module.css";

type Pestana = "informacion" | "ajustes" | "seguridad" | "reportar";

const PESTANAS: { id: Pestana; nombre: string; Icono: typeof IconoPersona }[] = [
  { id: "informacion", nombre: "Información personal", Icono: IconoPersona },
  { id: "ajustes", nombre: "Ajustes", Icono: IconoAjustes },
  { id: "seguridad", nombre: "Seguridad", Icono: IconoCandado },
  { id: "reportar", nombre: "Reportar", Icono: IconoBandera },
];

function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/).filter(Boolean);
  if (partes.length === 0) return "?";
  return (partes.length === 1 ? partes[0].slice(0, 2) : partes[0][0] + partes[1][0]).toUpperCase();
}

export default function Perfil() {
  const router = useRouter();
  const { sesion } = useSesion();
  const aviso = useAviso();
  const [pestana, setPestana] = useState<Pestana>("informacion");
  const [nombre, setNombre] = useState(sesion?.usuario.nombre ?? "");
  const [carrera, setCarrera] = useState(CARRERAS[0]);
  const [semestre, setSemestre] = useState(5);
  const [avisosActivos, setAvisosActivos] = useState<boolean | null>(null);
  const [errorAvisos, setErrorAvisos] = useState("");
  const [guardandoAvisos, setGuardandoAvisos] = useState(false);

  useEffect(() => {
    if (!sesion) return;
    let activo = true;
    obtenerConfiguracion()
      .then((c) => {
        if (activo) setAvisosActivos(c.notificacionesActivas);
      })
      .catch((e: Error) => {
        if (activo) setErrorAvisos(e.message);
      });
    return () => {
      activo = false;
    };
  }, [sesion]);

  async function cambiarAvisos(valor: boolean) {
    setGuardandoAvisos(true);
    setErrorAvisos("");
    try {
      const c = await guardarConfiguracion({ notificacionesActivas: valor });
      setAvisosActivos(c.notificacionesActivas);
    } catch (e) {
      setErrorAvisos((e as Error).message);
    } finally {
      setGuardandoAvisos(false);
    }
  }

  function salir() {
    cerrarSesion();
    router.replace("/login");
  }

  const usuario = sesion?.usuario;

  return (
    <main className={comun.pagina}>
      <h1 className={comun.titulo}>Mi perfil</h1>

      <section className={`${comun.tarjeta} ${estilos.cabecera}`}>
        <div className={estilos.avatar} aria-hidden="true">
          {iniciales(usuario?.nombre ?? "")}
        </div>
        <div className={estilos.datos}>
          <h2 className={estilos.nombre}>{usuario?.nombre}</h2>
          <p className={estilos.correo}>{usuario?.correo}</p>
          <div className={estilos.insignias}>
            <span className={estilos.verificado}>
              <IconoCheck tamano={14} /> Verificado
            </span>
            <span className={estilos.semestre}>Semestre {semestre}</span>
          </div>
        </div>
      </section>

      <div className={estilos.pestanas} role="tablist" aria-label="Secciones del perfil">
        {PESTANAS.map(({ id, nombre: etiqueta, Icono }) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={pestana === id}
            aria-label={etiqueta}
            title={etiqueta}
            className={pestana === id ? estilos.pestanaActiva : estilos.pestana}
            onClick={() => setPestana(id)}
          >
            <Icono tamano={20} />
          </button>
        ))}
      </div>

      <section className={comun.tarjeta} role="tabpanel">
        {pestana === "informacion" && (
          <form
            className={estilos.formulario}
            onSubmit={(e) => {
              e.preventDefault();
              aviso.mostrar("Guardar el perfil estará disponible pronto.");
            }}
          >
            <h2 className={estilos.seccion}>Información personal</h2>
            <label className={estilos.campo}>
              Nombre completo
              <input value={nombre} onChange={(e) => setNombre(e.target.value)} autoComplete="name" />
            </label>
            <label className={estilos.campo}>
              Correo institucional
              <input value={usuario?.correo ?? ""} disabled readOnly />
              <small>El correo no puede modificarse · Verificado con JWT</small>
            </label>
            <div className={estilos.fila}>
              <label className={estilos.campo}>
                Carrera
                <select value={carrera} onChange={(e) => setCarrera(e.target.value)}>
                  {CARRERAS.map((c) => (
                    <option key={c}>{c}</option>
                  ))}
                </select>
              </label>
              <label className={estilos.campo}>
                Semestre
                <select value={semestre} onChange={(e) => setSemestre(Number(e.target.value))}>
                  {SEMESTRES.map((s) => (
                    <option key={s} value={s}>
                      {s}°
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <button type="submit" className={estilos.primario}>
              Guardar cambios
            </button>
            <Link href="/horario" className={estilos.secundario}>
              <IconoCalendario tamano={18} /> Editar mi horario
            </Link>
          </form>
        )}

        {pestana === "ajustes" && (
          <div className={estilos.formulario}>
            <h2 className={estilos.seccion}>Ajustes</h2>
            <label className={estilos.interruptor}>
              <span>
                <strong>Avisos de puente disponible</strong>
                <small>Te avisamos cuando empieza un hueco libre entre clases.</small>
              </span>
              <input
                type="checkbox"
                checked={avisosActivos ?? false}
                disabled={avisosActivos === null || guardandoAvisos}
                onChange={(e) => cambiarAvisos(e.target.checked)}
              />
            </label>
            {errorAvisos && (
              <p className={estilos.error} role="alert">
                {errorAvisos}
              </p>
            )}
            <button type="button" className={estilos.salir} onClick={salir}>
              <IconoSalir tamano={18} /> Cerrar sesión
            </button>
          </div>
        )}

        {pestana === "seguridad" && (
          <form className={estilos.formulario} onSubmit={(e) => e.preventDefault()}>
            <h2 className={estilos.seccion}>Seguridad</h2>
            <label className={estilos.campo}>
              Contraseña actual
              <input type="password" disabled autoComplete="current-password" />
            </label>
            <label className={estilos.campo}>
              Nueva contraseña
              <input type="password" disabled autoComplete="new-password" />
            </label>
            <label className={estilos.campo}>
              Confirmar nueva contraseña
              <input type="password" disabled autoComplete="new-password" />
            </label>
            <p className={comun.nota}>Cambiar la contraseña estará disponible pronto.</p>
            <button type="submit" className={estilos.primario} disabled>
              Cambiar contraseña
            </button>
          </form>
        )}

        {pestana === "reportar" && (
          <form
            className={estilos.formulario}
            onSubmit={(e) => {
              e.preventDefault();
              aviso.mostrar("El envío de reportes estará disponible pronto.");
            }}
          >
            <h2 className={estilos.seccion}>Reportar un problema</h2>
            <label className={estilos.campo}>
              Tipo de reporte
              <select defaultValue="error">
                <option value="error">Algo no funciona</option>
                <option value="lugar">Información incorrecta de un lugar</option>
                <option value="sugerencia">Sugerencia</option>
              </select>
            </label>
            <label className={estilos.campo}>
              Cuéntanos qué pasó
              <textarea rows={4} />
            </label>
            <button type="submit" className={estilos.primario}>
              Enviar reporte
            </button>
          </form>
        )}
      </section>

      <AvisoFlotante texto={aviso.texto} />
    </main>
  );
}

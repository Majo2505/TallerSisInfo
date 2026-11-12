import { pedirApi } from "./api";

export type Perfil = {
  nombre: string;
  correo: string;
  rol: "ESTUDIANTE" | "ADMIN";
  carrera: string | null;
  semestre: number | null;
  /** False si entra solo con Google: en Seguridad se crea una contraseña sin pedir la actual. */
  tieneContrasena?: boolean;
  conGoogle?: boolean;
};

export type CambiosPerfil = { nombre: string; carrera: string | null; semestre: number | null };

export const LARGO_MAXIMO_NOMBRE = 100;

export const CARRERAS = [
  "Ingeniería de Sistemas",
  "Ingeniería Comercial",
  "Ingeniería Industrial",
  "Ingeniería Civil",
  "Derecho",
  "Psicología",
  "Arquitectura",
  "Administración de Empresas",
  "Comunicación Social",
  "Medicina",
];

export const SEMESTRES = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

export const obtenerPerfil = () => pedirApi<Perfil>("/api/perfil");
/** Cambia la contraseña (o la crea si la cuenta entra solo con Google). Responde 204. */
export const cambiarContrasena = (actual: string | null, nueva: string, confirmacion: string) =>
  pedirApi<void>("/api/perfil/contrasena", { method: "PUT", body: JSON.stringify({ actual, nueva, confirmacion }) });

/** Las mismas reglas que el servidor (ReglasCuenta.ValidarNueva), para avisar antes de enviar. */
export function errorContrasenaNueva(nueva: string, confirmacion: string): string | null {
  if (!nueva) return "Escribe la nueva contraseña.";
  if (nueva.length < 8) return "La contraseña tiene que tener al menos 8 caracteres.";
  if (new TextEncoder().encode(nueva).length > 72) return "La contraseña es demasiado larga (máximo 72 caracteres sin tildes).";
  if (nueva.trim() !== nueva) return "La contraseña no puede empezar ni terminar con espacios.";
  if (!/\p{L}/u.test(nueva) || !/\d/.test(nueva)) return "La contraseña tiene que tener al menos una letra y un número.";
  if (nueva !== confirmacion) return "Las contraseñas no coinciden.";
  return null;
}

export const actualizarPerfil = (cambios: CambiosPerfil) =>
  pedirApi<Perfil>("/api/perfil", { method: "PUT", body: JSON.stringify(cambios) });

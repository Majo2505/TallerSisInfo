import { pedirApi } from "./api";

export type Perfil = {
  nombre: string;
  correo: string;
  rol: "ESTUDIANTE" | "ADMIN";
  carrera: string | null;
  semestre: number | null;
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
export const actualizarPerfil = (cambios: CambiosPerfil) =>
  pedirApi<Perfil>("/api/perfil", { method: "PUT", body: JSON.stringify(cambios) });

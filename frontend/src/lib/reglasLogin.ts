export const MENSAJE_CAMPOS_LOGIN = "Por favor, complete todos los campos";

/** HU1: antes de llamar a la API, el login exige los dos campos con el texto exacto del criterio de aceptación. */
export function validarLogin(correo: string, password: string): string | null {
  return correo.trim() && password.trim() ? null : MENSAJE_CAMPOS_LOGIN;
}

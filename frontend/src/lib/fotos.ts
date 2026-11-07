import { API_URL } from "./config";

/** Lo mismo que acepta el servidor (ReglasFotoLugar). */
export const TIPOS_FOTO = ["image/jpeg", "image/png", "image/webp"];
export const BYTES_MAXIMOS_FOTO = 2 * 1024 * 1024;
/** Una foto de celular pesa unos 3 a 12 MB; más de 25 MB ya no es una foto normal y decodificarla traba el navegador. */
const BYTES_MAXIMOS_ORIGINAL = 25 * 1024 * 1024;
const LADO_MAXIMO = 1600;

/** La ruta de la API (`/api/lugares/12/foto?v=…`) como URL completa, o null si el lugar no tiene foto. */
export const urlDeFoto = (ruta: string | null | undefined) => (ruta ? `${API_URL}${ruta}` : null);

function aBlob(lienzo: HTMLCanvasElement, tipo: string, calidad: number): Promise<Blob | null> {
  return new Promise((listo) => lienzo.toBlob(listo, tipo, calidad));
}

/**
 * Achica la foto antes de subirla: 1600 px de lado como máximo y WebP (JPG si el navegador no sabe hacer WebP).
 * Una foto de celular de 5 MB queda en unos 200 a 400 KB, que cargan rápido en las tarjetas y caben en la base.
 */
export async function achicarFoto(archivo: File): Promise<Blob> {
  if (!TIPOS_FOTO.includes(archivo.type)) throw new Error("La foto tiene que ser JPG, PNG o WebP.");
  if (archivo.size > BYTES_MAXIMOS_ORIGINAL) throw new Error("Esa foto es demasiado pesada. Elige una de menos de 25 MB.");

  let imagen: ImageBitmap;
  try {
    imagen = await createImageBitmap(archivo, { imageOrientation: "from-image" });
  } catch {
    throw new Error("No pudimos abrir esa foto. Prueba con otra.");
  }

  for (const lado of [LADO_MAXIMO, 1200]) {
    const escala = Math.min(1, lado / Math.max(imagen.width, imagen.height));
    const lienzo = document.createElement("canvas");
    lienzo.width = Math.round(imagen.width * escala);
    lienzo.height = Math.round(imagen.height * escala);
    const contexto = lienzo.getContext("2d");
    if (!contexto) break;
    contexto.imageSmoothingQuality = "high";
    contexto.drawImage(imagen, 0, 0, lienzo.width, lienzo.height);

    for (const calidad of [0.82, 0.7]) {
      const webp = await aBlob(lienzo, "image/webp", calidad);
      const blob = webp?.type === "image/webp" ? webp : await aBlob(lienzo, "image/jpeg", calidad);
      if (blob && blob.size <= BYTES_MAXIMOS_FOTO) {
        imagen.close();
        return blob;
      }
    }
  }

  imagen.close();
  throw new Error("No pudimos achicar esa foto lo suficiente. Prueba con otra.");
}

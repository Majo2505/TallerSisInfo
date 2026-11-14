import Image, { type StaticImageData } from "next/image";
import estilos from "./FondoVivo.module.css";

type Props = {
  /** Con foto, la portada se ve como la del onboarding: imagen real con un velo azul marino donde va el texto. */
  foto?: StaticImageData;
  /** `object-position` de la foto: dónde está lo importante (la persona) para que no lo tape el texto. */
  enfoque?: string;
};

/** Fondo de las portadas azul marino: luces que se mueven lento o, si se le pasa, una fotografía. */
export default function FondoVivo({ foto, enfoque = "75% 50%" }: Props) {
  return (
    <div className={estilos.fondo} aria-hidden="true">
      {foto ? (
        <>
          <span className={estilos.fotoMarco}>
            <Image src={foto} alt="" fill placeholder="blur" sizes="(max-width: 900px) 100vw, 1400px" className={estilos.foto} style={{ objectPosition: enfoque }} />
          </span>
          <span className={estilos.velo} />
        </>
      ) : (
        <>
          <span className={estilos.luzAzul} />
          <span className={estilos.luzCeleste} />
          <span className={estilos.luzAmarilla} />
        </>
      )}
      <span className={estilos.foco} />
      <span className={estilos.grano} />
    </div>
  );
}

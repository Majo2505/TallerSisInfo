/** Íconos de la portada: trazo de 2 px, redondeado, en `currentColor`. */

export type NombreIcono =
  | "rayo"
  | "calendario"
  | "escudo"
  | "ubicacion"
  | "grupo"
  | "coincidencia"
  | "evento"
  | "enVivo"
  | "camara"
  | "campana"
  | "ojo";

type PropsIcono = { tamano?: number };

const base = {
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 2,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
  "aria-hidden": true,
  focusable: false,
};

const TRAZOS: Record<NombreIcono, React.ReactNode> = {
  rayo: <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8z" />,
  calendario: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="3" />
      <path d="M3 10h18M8 3v4M16 3v4" />
    </>
  ),
  escudo: (
    <>
      <path d="M12 3 4 6v6c0 4.5 3.2 8 8 9 4.8-1 8-4.5 8-9V6l-8-3z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  ubicacion: (
    <>
      <path d="M12 21s-7-6.2-7-11.5a7 7 0 1 1 14 0C19 14.8 12 21 12 21z" />
      <circle cx="12" cy="9.5" r="2.5" />
    </>
  ),
  grupo: (
    <>
      <circle cx="9" cy="8" r="3.2" />
      <path d="M3 20c.6-3.4 3-5.4 6-5.4s5.4 2 6 5.4" />
      <path d="M16 5.2a3 3 0 0 1 0 5.6M18 14.9c1.6.7 2.7 2.4 3 5.1" />
    </>
  ),
  coincidencia: (
    <>
      <circle cx="9" cy="12" r="6" />
      <circle cx="15" cy="12" r="6" />
    </>
  ),
  evento: (
    <>
      <path d="M4 9V6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v3a3 3 0 0 0 0 6v3a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-3a3 3 0 0 0 0-6z" />
      <path d="M14 4v16" strokeDasharray="2 3" />
    </>
  ),
  enVivo: (
    <>
      <circle cx="12" cy="12" r="2" />
      <path d="M8.2 15.8a5.4 5.4 0 0 1 0-7.6M15.8 8.2a5.4 5.4 0 0 1 0 7.6M5.1 18.9a9.8 9.8 0 0 1 0-13.8M18.9 5.1a9.8 9.8 0 0 1 0 13.8" />
    </>
  ),
  camara: (
    <>
      <path d="M4 8a2 2 0 0 1 2-2h2l1.5-2h5L16 6h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2z" />
      <circle cx="12" cy="12.5" r="3.5" />
    </>
  ),
  campana: (
    <>
      <path d="M6 16V11a6 6 0 1 1 12 0v5l1.5 2h-15z" />
      <path d="M10 21h4" />
    </>
  ),
  ojo: (
    <>
      <path d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12z" />
      <circle cx="12" cy="12" r="3" />
    </>
  ),
};

export function Icono({ nombre, tamano = 24 }: { nombre: NombreIcono } & PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      {TRAZOS[nombre]}
    </svg>
  );
}

/** Cinco estrellas rellenas para las opiniones. El texto accesible lo pone quien las usa. */
export function Estrellas({ tamano = 18 }: PropsIcono) {
  return (
    <span style={{ display: "inline-flex", gap: 3 }} aria-hidden="true">
      {Array.from({ length: 5 }, (_, i) => (
        <svg key={i} width={tamano} height={tamano} viewBox="0 0 24 24" focusable="false">
          <path
            d="M12 2.8l2.8 5.8 6.3.9-4.6 4.4 1.1 6.3L12 17.2l-5.6 3 1.1-6.3-4.6-4.4 6.3-.9z"
            fill="currentColor"
            stroke="currentColor"
            strokeWidth="1.2"
            strokeLinejoin="round"
          />
        </svg>
      ))}
    </span>
  );
}

import type { NombreIcono } from "./pasos";

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

function Rayo({ tamano = 40 }: PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8z" />
    </svg>
  );
}

function Calendario({ tamano = 40 }: PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <rect x="3" y="5" width="18" height="16" rx="3" />
      <path d="M3 10h18M8 3v4M16 3v4" />
    </svg>
  );
}

function Escudo({ tamano = 40 }: PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M12 3 4 6v6c0 4.5 3.2 8 8 9 4.8-1 8-4.5 8-9V6l-8-3z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  );
}

function Ubicacion({ tamano = 40 }: PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M12 21s-7-6.2-7-11.5a7 7 0 1 1 14 0C19 14.8 12 21 12 21z" />
      <circle cx="12" cy="9.5" r="2.5" />
    </svg>
  );
}

export function Informacion({ tamano = 16 }: PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 11v5M12 8h.01" />
    </svg>
  );
}

export function Flecha({ tamano = 20 }: PropsIcono) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="m9 6 6 6-6 6" />
    </svg>
  );
}

const ICONOS: Record<NombreIcono, (props: PropsIcono) => React.JSX.Element> = {
  rayo: Rayo,
  calendario: Calendario,
  escudo: Escudo,
  ubicacion: Ubicacion,
};

export function Icono({ nombre, tamano }: { nombre: NombreIcono } & PropsIcono) {
  const Componente = ICONOS[nombre];
  return <Componente tamano={tamano} />;
}

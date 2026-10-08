type Props = { tamano?: number };

const base = {
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 2,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
  "aria-hidden": true,
  focusable: false,
};

export function Logo({ tamano = 24 }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M6 4h7a4 4 0 0 1 0 8H6zM6 12h8a4 4 0 0 1 0 8H6zM6 4v16" />
    </svg>
  );
}

export function Rayo({ tamano = 20 }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8z" />
    </svg>
  );
}

export function Candado({ tamano = 20 }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <rect x="4" y="11" width="16" height="10" rx="2" />
      <path d="M8 11V7a4 4 0 0 1 8 0v4" />
    </svg>
  );
}

export function Personas({ tamano = 20 }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <circle cx="9" cy="8" r="3.5" />
      <path d="M2.5 20c0-3.6 2.9-6 6.5-6s6.5 2.4 6.5 6" />
      <path d="M16 4.6a3.5 3.5 0 0 1 0 6.8M18 14.4c2.2.7 3.5 2.6 3.5 5.6" />
    </svg>
  );
}

export function Ojo({ tamano = 20 }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

export function OjoTachado({ tamano = 20 }: Props) {
  return (
    <svg width={tamano} height={tamano} viewBox="0 0 24 24" {...base}>
      <path d="M10.7 5.1A10 10 0 0 1 12 5c6.4 0 10 7 10 7a17 17 0 0 1-3.2 4M6.6 6.6A17 17 0 0 0 2 12s3.6 7 10 7a9.7 9.7 0 0 0 5.4-1.6" />
      <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2M3 3l18 18" />
    </svg>
  );
}

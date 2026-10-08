import type { Metadata } from "next";
import { Inter, Plus_Jakarta_Sans } from "next/font/google";
import "./globals.css";

const fuenteTitulo = Plus_Jakarta_Sans({
  subsets: ["latin"],
  weight: ["700", "800"],
  variable: "--fuente-titulo",
  display: "swap",
});

const fuenteTexto = Inter({
  subsets: ["latin"],
  variable: "--fuente-texto",
  display: "swap",
});

export const metadata: Metadata = {
  title: "BREAKU",
  description: "Plataforma web que detecta puentes horarios en el campus",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es" className={`${fuenteTitulo.variable} ${fuenteTexto.variable}`}>
      <body>{children}</body>
    </html>
  );
}

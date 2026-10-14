import type { Metadata } from "next";
import { Open_Sans, Roboto } from "next/font/google";
import "./globals.css";

// Tipografías de la UCB: Open Sans en títulos (ucb.edu.bo) y Roboto en la interfaz (Sistema Académico Nacional).
const fuenteTitulo = Open_Sans({
  subsets: ["latin"],
  weight: ["600", "700", "800"],
  variable: "--fuente-titulo",
  display: "swap",
});

const fuenteTexto = Roboto({
  subsets: ["latin"],
  weight: ["400", "500", "700"],
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

import type { Metadata, Viewport } from "next";
import { Manrope } from "next/font/google";
import "./globals.css";

// Manrope: geométrica y moderna, con números claros para minutos y horas. Una sola familia para títulos e interfaz.
const fuente = Manrope({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700", "800"],
  variable: "--fuente",
  display: "swap",
});

export const metadata: Metadata = {
  title: "BREAKU",
  description: "Plataforma web que detecta puentes horarios en el campus",
};

// En el celular: la página llega a la muesca (las barras se apartan con env(safe-area-inset-*)),
// el teclado de Android achica el área visible y la barra de estado toma el color del fondo.
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
  interactiveWidget: "resizes-content",
  themeColor: "#f6f7fb",
  colorScheme: "light",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es" className={fuente.variable}>
      <body>{children}</body>
    </html>
  );
}

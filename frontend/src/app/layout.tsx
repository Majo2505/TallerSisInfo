import type { Metadata, Viewport } from "next";
import { Manrope } from "next/font/google";
import "./globals.css";

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

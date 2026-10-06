import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "BREAKU",
  description: "Plataforma web que detecta puentes horarios en el campus",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}

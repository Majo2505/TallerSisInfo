export function iniciales(nombre: string): string {
  const palabras = nombre.trim().split(/\s+/).filter(Boolean);
  if (palabras.length === 0) return "?";
  const propias = palabras.filter((p) => p[0] === p[0].toLocaleUpperCase("es") && p[0] !== p[0].toLocaleLowerCase("es"));
  const usadas = propias.length > 0 ? propias : palabras;
  if (usadas.length === 1) return Array.from(usadas[0]).slice(0, 2).join("").toLocaleUpperCase("es");
  const primera = Array.from(usadas[0])[0];
  const ultima = Array.from(usadas[usadas.length - 1])[0];
  return (primera + ultima).toLocaleUpperCase("es");
}

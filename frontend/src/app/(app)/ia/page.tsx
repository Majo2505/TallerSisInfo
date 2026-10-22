import Recomendaciones from "@/components/app/Recomendaciones";
import { ESTADOS_ANIMO, EstadoAnimo } from "@/lib/maqueta";

type Props = {
  searchParams: Promise<{ estado?: string; min?: string }>;
};

export default async function PaginaIa({ searchParams }: Props) {
  const { estado, min } = await searchParams;
  const estadoInicial = ESTADOS_ANIMO.find((e) => e.id === estado)?.id as EstadoAnimo | undefined;
  const minutos = Number(min);
  const minutosLibres = Number.isInteger(minutos) && minutos > 0 && minutos <= 600 ? minutos : undefined;
  return <Recomendaciones estadoInicial={estadoInicial} minutosLibres={minutosLibres} />;
}

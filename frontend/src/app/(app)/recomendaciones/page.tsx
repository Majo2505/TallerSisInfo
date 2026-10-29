import Recomendaciones from "@/components/app/Recomendaciones";
import { ESTADOS_ANIMO, EstadoAnimo } from "@/lib/maqueta";

type Props = {
  searchParams: Promise<{ estado?: string; min?: string; puente?: string }>;
};

const enteroPositivo = (valor: string | undefined, maximo: number) => {
  const n = Number(valor);
  return Number.isInteger(n) && n > 0 && n <= maximo ? n : undefined;
};

export default async function PaginaRecomendaciones({ searchParams }: Props) {
  const { estado, min, puente } = await searchParams;
  const estadoInicial = ESTADOS_ANIMO.find((e) => e.id === estado)?.id as EstadoAnimo | undefined;
  return (
    <Recomendaciones
      estadoInicial={estadoInicial}
      minutosLibres={enteroPositivo(min, 600)}
      puenteInicial={enteroPositivo(puente, Number.MAX_SAFE_INTEGER)}
    />
  );
}

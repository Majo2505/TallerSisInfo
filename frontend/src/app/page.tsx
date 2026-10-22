import Onboarding from "../components/onboarding/Onboarding";

type Props = {
  searchParams: Promise<{ onboarding?: string }>;
};

export default async function Home({ searchParams }: Props) {
  // En desarrollo, /?onboarding=1 ignora la marca de "ya visto".
  const { onboarding } = await searchParams;
  return <Onboarding forzar={onboarding === "1"} />;
}

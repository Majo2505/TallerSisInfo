import Onboarding from "../components/onboarding/Onboarding";

type Props = {
  searchParams: Promise<{ onboarding?: string }>;
};

export default async function Home({ searchParams }: Props) {
  const { onboarding } = await searchParams;
  return <Onboarding forzar={onboarding === "1"} />;
}

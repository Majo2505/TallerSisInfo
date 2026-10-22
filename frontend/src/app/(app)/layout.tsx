import ShellApp from "@/components/app/ShellApp";

export default function LayoutApp({ children }: { children: React.ReactNode }) {
  return <ShellApp>{children}</ShellApp>;
}

import type { ReactNode } from "react";
import { cn } from "@/components/ui/cn";

export function PageContainer({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return <div className={cn("mx-auto w-full max-w-6xl px-4 md:px-8", className)}>{children}</div>;
}
import { cn } from "@/components/ui/cn";

export function SkeletonBlock({ className }: { className?: string }) {
  return <div className={cn("animate-pulse rounded-lg bg-[var(--color-border)]/45", className)} />;
}

export function SkeletonLines({ lines = 4, className }: { lines?: number; className?: string }) {
  return (
    <div className={cn("surface-card space-y-3 p-5", className)}>
      {Array.from({ length: lines }).map((_, index) => (
        <SkeletonBlock
          key={`skeleton-line-${index}`}
          className={cn("h-3", index % 3 === 0 ? "w-full" : index % 3 === 1 ? "w-5/6" : "w-3/4")}
        />
      ))}
    </div>
  );
}
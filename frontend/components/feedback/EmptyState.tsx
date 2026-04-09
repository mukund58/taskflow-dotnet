import type { ReactNode } from "react";

type Props = {
  title: string;
  description: string;
  action?: ReactNode;
};

export function EmptyState({ title, description, action }: Props) {
  return (
    <div className="surface-card p-6">
      <p className="text-xs uppercase tracking-[0.2em] text-[var(--color-muted)]">Empty State</p>
      <h3 className="mt-3 text-lg font-semibold">{title}</h3>
      <p className="mt-2 text-sm text-[var(--color-muted)]">{description}</p>
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  );
}
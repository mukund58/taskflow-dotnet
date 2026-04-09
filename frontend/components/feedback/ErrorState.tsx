import { Button } from "@/components/ui/Button";

type Props = {
  title?: string;
  message: string;
  onRetry?: () => void;
};

export function ErrorState({ title = "Something went wrong", message, onRetry }: Props) {
  return (
    <div className="surface-card p-6">
      <p className="text-xs uppercase tracking-[0.2em] text-[var(--color-danger)]">Error</p>
      <h3 className="mt-3 text-lg font-semibold">{title}</h3>
      <p className="mt-2 text-sm text-[var(--color-muted)]">{message}</p>
      {onRetry ? (
        <div className="mt-4">
          <Button variant="danger" onClick={onRetry}>
            Retry
          </Button>
        </div>
      ) : null}
    </div>
  );
}
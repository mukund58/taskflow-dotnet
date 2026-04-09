export function FormFieldError({ message }: { message?: string }) {
  if (!message) {
    return null;
  }

  return <p className="mt-1 text-xs font-medium text-[var(--color-danger)]">{message}</p>;
}
"use client";

import { ErrorState } from "@/components/feedback/ErrorState";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="py-12">
      <ErrorState
        title="Something failed while loading this page"
        message={error.message || "Please retry in a moment."}
        onRetry={reset}
      />
    </div>
  );
}
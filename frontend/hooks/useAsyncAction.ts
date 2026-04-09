"use client";

import { useCallback, useState } from "react";

export function useAsyncAction<TArgs extends unknown[], TResult>(
  action: (...args: TArgs) => Promise<TResult>,
) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = useCallback(
    async (...args: TArgs) => {
      try {
        setIsLoading(true);
        setError(null);
        return await action(...args);
      } catch (caughtError) {
        const message = caughtError instanceof Error ? caughtError.message : "Unexpected error";
        setError(message);
        throw caughtError;
      } finally {
        setIsLoading(false);
      }
    },
    [action],
  );

  return {
    run,
    isLoading,
    error,
    clearError: () => setError(null),
  };
}
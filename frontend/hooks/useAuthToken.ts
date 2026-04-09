"use client";

import { useState } from "react";
import { clearAuthToken, getAuthToken, setAuthToken } from "@/services/auth/token-store";

export function useAuthToken() {
  const [token, setTokenState] = useState<string | null>(() => getAuthToken());

  const updateToken = (nextToken: string) => {
    setAuthToken(nextToken);
    setTokenState(nextToken);
  };

  const clearToken = () => {
    clearAuthToken();
    setTokenState(null);
  };

  return {
    token,
    isAuthenticated: Boolean(token),
    setToken: updateToken,
    clearToken,
  };
}
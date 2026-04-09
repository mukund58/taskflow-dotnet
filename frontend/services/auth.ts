import { apiClient } from "@/services/api/client";
import { clearAuthToken, setAuthToken } from "@/services/auth/token-store";
import { getSessionUserFromToken } from "@/lib/auth/jwt";
import { useAuthStore } from "@/store/authStore";
import type { ApiResponse } from "@/types/api";
import type { AuthPayload, LoginInput, RegisterInput } from "@/types/auth";

async function unwrapAuthResponse(path: string, payload: LoginInput | RegisterInput) {
  const response = await apiClient.post<ApiResponse<AuthPayload>>(path, payload);
  const authPayload = response.data;

  if (authPayload?.token) {
    setAuthToken(authPayload.token);

    const sessionUser = getSessionUserFromToken(authPayload.token);
    if (sessionUser) {
      useAuthStore.getState().setSessionFromToken(authPayload.token, sessionUser);
    }
  }

  return authPayload;
}

export function login(payload: LoginInput) {
  return unwrapAuthResponse("/api/auth/login", payload);
}

export function register(payload: RegisterInput) {
  return unwrapAuthResponse("/api/auth/register", payload);
}

export function logout() {
  clearAuthToken();
  useAuthStore.getState().clearSession();
}
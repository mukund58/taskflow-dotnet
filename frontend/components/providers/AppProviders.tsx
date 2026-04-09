"use client";

import { useEffect, useMemo } from "react";
import { usePathname, useRouter } from "next/navigation";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "react-hot-toast";
import { getAuthToken, clearAuthToken } from "@/services/auth/token-store";
import { getSessionUserFromToken, isTokenExpired } from "@/lib/auth/jwt";
import { useAuthStore } from "@/store/authStore";

const protectedPrefixes = ["/dashboard", "/tasks", "/task", "/projects", "/settings"];
const authPrefixes = ["/auth/login", "/auth/register"];

function isProtectedPath(pathname: string) {
  return protectedPrefixes.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

function isAuthPath(pathname: string) {
  return authPrefixes.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

function RouteGuard() {
  const router = useRouter();
  const pathname = usePathname();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const setSession = useAuthStore((state) => state.setSessionFromToken);
  const clearSession = useAuthStore((state) => state.clearSession);

  useEffect(() => {
    const token = getAuthToken();

    if (!token) {
      clearSession();
      return;
    }

    if (isTokenExpired(token)) {
      clearAuthToken();
      clearSession();
      return;
    }

    const user = getSessionUserFromToken(token);
    if (!user) {
      clearAuthToken();
      clearSession();
      return;
    }

    setSession(token, user);
  }, [setSession, clearSession]);

  useEffect(() => {
    if (isProtectedPath(pathname) && !isAuthenticated) {
      const token = getAuthToken();
      const hasHydratableSession = Boolean(
        token &&
        !isTokenExpired(token) &&
        getSessionUserFromToken(token),
      );

      if (hasHydratableSession) {
        return;
      }

      router.replace("/auth/login");
      return;
    }

    if (isAuthPath(pathname) && isAuthenticated) {
      router.replace("/dashboard");
    }
  }, [pathname, isAuthenticated, router]);

  return null;
}

export function AppProviders({ children }: { children: React.ReactNode }) {
  const queryClient = useMemo(() => new QueryClient(), []);

  return (
    <QueryClientProvider client={queryClient}>
      <RouteGuard />
      {children}
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 3200,
          style: {
            border: "1px solid var(--color-border)",
            background: "var(--color-card)",
            color: "var(--color-foreground)",
          },
        }}
      />
    </QueryClientProvider>
  );
}

"use client";

import { Suspense, useEffect, useMemo } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "react-hot-toast";
import { getAuthToken, clearAuthToken } from "@/services/auth/token-store";
import { getSessionUserFromToken, isTokenExpired } from "@/lib/auth/jwt";
import { useAuthStore } from "@/store/authStore";

const protectedPrefixes = ["/dashboard", "/tasks", "/task", "/projects", "/settings"];
const protectedExactPaths = ["/invitations"];
const authPrefixes = ["/auth/login", "/auth/register"];

function isProtectedPath(pathname: string) {
  if (protectedExactPaths.includes(pathname)) {
    return true;
  }

  return protectedPrefixes.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

function isAuthPath(pathname: string) {
  return authPrefixes.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

function resolveSafeRedirect(rawRedirect: string | null) {
  if (!rawRedirect) {
    return null;
  }

  if (!rawRedirect.startsWith("/") || rawRedirect.startsWith("//")) {
    return null;
  }

  return rawRedirect;
}

/**
 * RouteGuard uses useSearchParams() which requires a <Suspense> boundary.
 * Without it, Next.js cannot statically prerender pages like /_not-found,
 * causing Vercel build failures with "prerender-error".
 */
function RouteGuard() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const searchParamsString = searchParams.toString();
  const redirectParam = searchParams.get("redirect");
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

      const search = searchParamsString;
      const currentPath = search ? `${pathname}?${search}` : pathname;
      const redirectQuery = new URLSearchParams({ redirect: currentPath });
      router.replace(`/auth/login?${redirectQuery.toString()}`);
      return;
    }

    if (isAuthPath(pathname) && isAuthenticated) {
      const redirectTarget = resolveSafeRedirect(redirectParam);
      router.replace(redirectTarget ?? "/dashboard");
    }
  }, [pathname, searchParamsString, redirectParam, isAuthenticated, router]);

  return null;
}

export function AppProviders({ children }: { children: React.ReactNode }) {
  const queryClient = useMemo(() => new QueryClient(), []);

  return (
    <QueryClientProvider client={queryClient}>
      {/*
        Suspense is required because RouteGuard calls useSearchParams().
        Next.js needs a Suspense boundary to statically prerender pages
        (/_not-found, etc.) without crashing the Vercel build.
      */}
      <Suspense fallback={null}>
        <RouteGuard />
      </Suspense>
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

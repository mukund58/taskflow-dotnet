"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { clearAuthToken } from "@/services/auth/token-store";
import { useAuthStore } from "@/store/authStore";

const navItems = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/tasks", label: "Tasks" },
  { href: "/projects", label: "Projects" },
  { href: "/settings", label: "Settings" },
];

export function MainNav() {
  const router = useRouter();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);

  const handleLogout = () => {
    clearAuthToken();
    clearSession();
    router.push("/auth/login");
  };

  return (
    <nav className="flex flex-wrap items-center gap-2 md:justify-end">
      {navItems.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          className="rounded-full border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-2 text-sm font-medium text-[var(--color-foreground)] transition hover:border-[var(--color-accent)] hover:text-[var(--color-accent)]"
        >
          {item.label}
        </Link>
      ))}

      {isAuthenticated && user ? (
        <>
          <Badge variant="secondary">{user.role}</Badge>
          <span className="px-2 text-sm text-muted-foreground">{user.name}</span>
          <Button variant="outline" size="sm" onClick={handleLogout}>
            Logout
          </Button>
        </>
      ) : (
        <Button asChild size="sm">
          <Link href="/auth/login">Sign in</Link>
        </Button>
      )}
    </nav>
  );
}
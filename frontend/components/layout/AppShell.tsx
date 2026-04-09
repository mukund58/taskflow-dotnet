"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { MainNav } from "@/components/layout/MainNav";
import { PageContainer } from "@/components/layout/PageContainer";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/store/authStore";

const sidebarItems = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/tasks", label: "Tasks" },
  { href: "/projects", label: "Projects" },
  { href: "/settings", label: "Settings" },
];

function isProtectedPath(pathname: string) {
  return ["/dashboard", "/tasks", "/task", "/projects", "/settings"].some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const showSidebar = isAuthenticated && isProtectedPath(pathname);

  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--color-border)]/80 bg-[rgba(255,248,236,0.9)] backdrop-blur">
        <PageContainer className="flex flex-col gap-4 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.22em] text-[var(--color-muted)]">GDG Hackathon</p>
            <h1 className="text-2xl font-semibold tracking-tight">GDG Taskboard</h1>
          </div>
          <MainNav />
        </PageContainer>
      </header>

      <main>
        <PageContainer className="py-8 md:py-10">
          {showSidebar ? (
            <div className="grid gap-6 lg:grid-cols-[220px_minmax(0,1fr)]">
              <aside className="surface-card h-fit p-3">
                <p className="px-3 pb-2 text-xs uppercase tracking-[0.14em] text-muted-foreground">Workspace</p>
                <nav className="space-y-1">
                  {sidebarItems.map((item) => {
                    const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`);

                    return (
                      <Link
                        key={item.href}
                        href={item.href}
                        className={cn(
                          "block rounded-lg px-3 py-2 text-sm transition",
                          isActive
                            ? "bg-primary text-primary-foreground"
                            : "text-foreground hover:bg-accent hover:text-accent-foreground",
                        )}
                      >
                        {item.label}
                      </Link>
                    );
                  })}
                </nav>
              </aside>

              <section>{children}</section>
            </div>
          ) : (
            children
          )}
        </PageContainer>
      </main>
    </div>
  );
}
import Link from "next/link";
import { ArrowRight, ChartColumnIncreasing, ShieldCheck, UsersRound } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/Card";

const featureCards = [
  {
    title: "Team-first workflow",
    description: "Projects, assignees, comments, checklists, and watchers in one flow.",
    icon: UsersRound,
  },
  {
    title: "Secure by design",
    description: "RBAC + ownership + project-level access are already enforced in backend APIs.",
    icon: ShieldCheck,
  },
  {
    title: "Actionable dashboard",
    description: "Track active, completed, overdue, and workload distribution to unblock teams.",
    icon: ChartColumnIncreasing,
  },
];

const quickLinks = [
  { href: "/dashboard", label: "Open dashboard" },
  { href: "/tasks", label: "Browse tasks" },
  { href: "/projects", label: "View projects" },
];

export function HomeLanding() {
  return (
    <div className="space-y-8 md:space-y-10">
      <section className="relative overflow-hidden rounded-2xl border border-border bg-card p-6 shadow-[0_22px_45px_rgba(44,31,17,0.1)] md:p-10">
        <div className="pointer-events-none absolute -top-14 -right-12 h-48 w-48 rounded-full bg-primary/15 blur-2xl" />
        <div className="pointer-events-none absolute -bottom-16 -left-10 h-40 w-40 rounded-full bg-[var(--color-success)]/15 blur-2xl" />

        <div className="relative z-10 max-w-3xl space-y-5">
          <Badge variant="secondary">Custom Landing Page</Badge>

          <h2 className="text-3xl font-semibold tracking-tight md:text-5xl">
            Plan, ship, and track work like a real product team.
          </h2>

          <p className="max-w-2xl text-sm leading-7 text-muted-foreground md:text-base">
            GDG Taskboard is your mini Jira-style workspace for hackathon execution. It combines project
            ownership, task lifecycle, collaboration features, and dashboard analytics with a clean,
            production-style UI.
          </p>

          <div className="flex flex-wrap gap-3">
            <Button asChild size="lg">
              <Link href="/dashboard">
                Go to dashboard
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
            <Button asChild size="lg" variant="outline">
              <Link href="/auth/login">Sign in</Link>
            </Button>
            <Button asChild size="lg" variant="ghost">
              {/* <Link href="/foundation">
                UI foundation
                <Sparkles className="h-4 w-4" />
              </Link> */}
            </Button>
          </div>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-3">
        {featureCards.map((feature) => {
          const Icon = feature.icon;

          return (
            <Card key={feature.title}>
              <CardHeader className="space-y-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                  <Icon className="h-5 w-5" />
                </div>
                <CardTitle>{feature.title}</CardTitle>
              </CardHeader>
              <CardContent className="pb-6">
                <CardDescription className="text-sm leading-6">{feature.description}</CardDescription>
              </CardContent>
            </Card>
          );
        })}
      </section>

      <section className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Ready for next implementation steps</CardTitle>
            <CardDescription>
              Start with auth flow, then task CRUD, dashboard depth, and final UX polish.
            </CardDescription>
          </CardHeader>
          <CardContent className="pb-6">
            <ul className="space-y-2 text-sm text-muted-foreground">
              <li>- Auth routes and session redirect behavior</li>
              <li>- Task list filtering + pagination + sorting</li>
              <li>- Task detail with checklist and comments</li>
              <li>- Dashboard charts and workload insights</li>
            </ul>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick actions</CardTitle>
            <CardDescription>Jump directly to core areas while building.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 pb-6">
            {quickLinks.map((item) => (
              <Button key={item.href} asChild variant="outline" className="w-full justify-between">
                <Link href={item.href}>
                  {item.label}
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </Button>
            ))}
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
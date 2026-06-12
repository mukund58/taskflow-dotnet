"use client";

import Link from "next/link";
import {
  ArrowUpRight,
  TrendingUp,
  Users,
  Zap,
  BarChart3,
  ShieldCheck,
  ArrowRight,
  CheckCircle2,
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";

/* ─────────────────────────────────────────
   Floating stat card
───────────────────────────────────────── */
function StatCard({
  label,
  value,
  change,
  className = "",
}: {
  label: string;
  value: string;
  change: string;
  className?: string;
}) {
  return (
    <div
      className={`absolute z-30 rounded-2xl border border-[#d6c6ad]
        bg-[#fff8ec] px-5 py-4 shadow-[0_12px_40px_rgba(44,31,17,0.22)]
        backdrop-blur-sm ${className}`}
    >
      <p className="text-[11px] font-medium text-[#6f655b]">{label}</p>
      <p className="mt-0.5 text-2xl font-bold tracking-tight text-[#2b2620]">{value}</p>
      <span className="mt-1.5 inline-flex items-center gap-1 rounded-full bg-[#d6f0e4] px-2 py-0.5 text-[11px] font-semibold text-[#256a4f]">
        <TrendingUp className="h-3 w-3" />
        {change}
      </span>
    </div>
  );
}

/* ─────────────────────────────────────────
   Dashboard mock preview
───────────────────────────────────────── */
function DashboardMock() {
  const bars = [55, 72, 45, 88, 63, 79, 91, 58, 74, 67, 82, 70];

  return (
    <div className="relative w-full">
      {/* ── floating stat cards ── */}
      {/* left card */}
      <StatCard
        label="Tasks Completed"
        value="1,284"
        change="+23.4%"
        className="bottom-16 -left-4 sm:left-0 md:-left-8 lg:-left-4"
      />
      {/* right card */}
      <StatCard
        label="Active Projects"
        value="38"
        change="+12.1%"
        className="-top-6 -right-4 sm:right-0 md:-right-8 lg:-right-4"
      />

      {/* ── mock browser window ── */}
      <div
        className="relative overflow-hidden rounded-t-2xl border border-b-0 border-[#d6c6ad]
          shadow-[0_-6px_60px_rgba(44,31,17,0.18)]"
        style={{ background: "#fff8ec" }}
      >
        {/* title bar */}
        <div className="flex items-center gap-2 border-b border-[#e5d6bf] bg-[#f3ecdf] px-5 py-3">
          <span className="h-3 w-3 rounded-full bg-[#ff5f57]" />
          <span className="h-3 w-3 rounded-full bg-[#ffbd2e]" />
          <span className="h-3 w-3 rounded-full bg-[#28ca41]" />
          <div className="ml-4 flex gap-1">
            {["Overview", "Tasks", "Projects", "Team"].map((t, i) => (
              <span
                key={t}
                className={`rounded-full px-3 py-0.5 text-xs font-medium ${i === 0
                  ? "bg-[#bc4a3c] text-white"
                  : "text-[#6f655b] hover:text-[#2b2620]"
                  }`}
              >
                {t}
              </span>
            ))}
          </div>
          <span className="ml-auto rounded-full bg-[#efe1cc] px-3 py-0.5 text-[10px] text-[#6f655b]">
            Jun 2026
          </span>
        </div>

        {/* content */}
        <div className="flex divide-x divide-[#e5d6bf]">
          {/* sidebar */}
          <div className="hidden w-44 flex-shrink-0 space-y-1 bg-[#f8f2e7] p-4 md:block">
            <p className="mb-3 text-[10px] uppercase tracking-widest text-[#6f655b]">Workspace</p>
            {["Dashboard", "My Tasks", "Projects", "Invitations", "Settings"].map((item, i) => (
              <div
                key={item}
                className={`flex cursor-default items-center gap-2 rounded-lg px-3 py-2 text-xs font-medium ${i === 0 ? "bg-[#bc4a3c]/10 text-[#bc4a3c]" : "text-[#6f655b]"
                  }`}
              >
                <span
                  className={`h-1.5 w-1.5 flex-shrink-0 rounded-full ${i === 0 ? "bg-[#bc4a3c]" : "bg-[#d6c6ad]"
                    }`}
                />
                {item}
              </div>
            ))}
          </div>

          {/* main area */}
          <div className="flex-1 p-5">
            <p className="mb-4 text-sm font-semibold text-[#2b2620]">Dashboard Overview</p>

            {/* stat row */}
            <div className="mb-4 grid grid-cols-3 gap-3">
              {[
                { label: "Total Tasks", val: "1,284", cls: "bg-[#bc4a3c]/10 text-[#bc4a3c]" },
                { label: "In Progress", val: "47", cls: "bg-amber-100 text-amber-700" },
                { label: "Completed", val: "981", cls: "bg-[#d6f0e4] text-[#256a4f]" },
              ].map((s) => (
                <div key={s.label} className="rounded-xl border border-[#e5d6bf] bg-[#f8f2e7] p-3">
                  <p className="text-[10px] text-[#6f655b]">{s.label}</p>
                  <p className={`mt-1 rounded px-1 text-lg font-bold ${s.cls}`}>{s.val}</p>
                </div>
              ))}
            </div>

            {/* recent tasks list */}
            <div className="mb-4 rounded-xl border border-[#e5d6bf] bg-[#f8f2e7] p-4">
              <p className="mb-3 text-[10px] font-medium text-[#6f655b]">Recent tasks</p>
              <div className="space-y-2">
                {[
                  { name: "Design landing hero", status: "Done", statusCls: "bg-[#d6f0e4] text-[#256a4f]" },
                  { name: "API auth endpoints", status: "In Progress", statusCls: "bg-amber-100 text-amber-700" },
                  { name: "Dashboard charts", status: "Todo", statusCls: "bg-[#e5d6bf] text-[#6f655b]" },
                  { name: "Mobile responsive", status: "In Progress", statusCls: "bg-amber-100 text-amber-700" },
                ].map((t) => (
                  <div key={t.name} className="flex items-center justify-between rounded-lg bg-[#fff8ec] px-3 py-2">
                    <span className="text-[11px] text-[#2b2620]">{t.name}</span>
                    <span className={`rounded-full px-2 py-0.5 text-[9px] font-semibold ${t.statusCls}`}>{t.status}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* bar chart */}
            <div className="rounded-xl border border-[#e5d6bf] bg-[#f8f2e7] p-4">
              <p className="mb-3 text-[10px] font-medium text-[#6f655b]">
                Tasks completed · last 12 days
              </p>
              <div className="flex h-36 items-end gap-1">
                {bars.map((h, i) => (
                  <div
                    key={i}
                    className="flex-1 rounded-t-sm bg-[#bc4a3c]"
                    style={{ height: `${h}%`, opacity: 0.4 + h / 180 }}
                  />
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ─────────────────────────────────────────
   Feature cards
───────────────────────────────────────── */
const features = [
  {
    icon: Users,
    title: "Team-first workflow",
    desc: "Projects, assignees, comments, checklists, and watchers in one seamless flow.",
    accent: "bg-[#bc4a3c]/10 text-[#bc4a3c]",
  },
  {
    icon: ShieldCheck,
    title: "Secure by design",
    desc: "RBAC + ownership + project-level access enforced at every API endpoint.",
    accent: "bg-[#256a4f]/10 text-[#256a4f]",
  },
  {
    icon: BarChart3,
    title: "Actionable insights",
    desc: "Track active, completed, and overdue tasks with workload distribution analytics.",
    accent: "bg-[#8b5a15]/10 text-[#8b5a15]",
  },
  {
    icon: Zap,
    title: "Instant updates",
    desc: "Real-time task status, invitations, and notifications keep your whole team in sync.",
    accent: "bg-[#bc4a3c]/10 text-[#bc4a3c]",
  },
];

/* ─────────────────────────────────────────
   Page
───────────────────────────────────────── */
export function HomeLanding() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <div>
      {/* ══════════════════════════════════════
          HERO — full viewport, centered layout
      ══════════════════════════════════════ */}
      <section
        className="relative flex min-h-screen flex-col overflow-hidden"
        style={{
          background:
            "radial-gradient(ellipse at 65% -15%, rgba(188,74,60,0.22) 0%, transparent 55%), " +
            "radial-gradient(ellipse at -5% 95%, rgba(139,90,21,0.16) 0%, transparent 45%), " +
            "linear-gradient(170deg, #f5ede0 0%, #e9dccb 100%)",
        }}
      >
        {/* dot-grid overlay */}
        <div
          className="pointer-events-none absolute inset-0 opacity-35"
          style={{
            backgroundImage: "radial-gradient(rgba(111,101,91,0.2) 0.7px, transparent 0.7px)",
            backgroundSize: "18px 18px",
          }}
        />

        {/* ambient blobs */}
        <div className="pointer-events-none absolute -top-40 -right-40 h-[500px] w-[500px] rounded-full bg-[#bc4a3c]/8 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-24 -left-24 h-80 w-80 rounded-full bg-[#8b5a15]/10 blur-3xl" />

        {/* ── CONTENT ── */}
        <div className="relative z-10 flex flex-1 flex-col">

          {/* ── text block: centered ── */}
          <div className="flex flex-col items-center px-5 pt-24 pb-10 text-center md:pt-28 md:pb-12">
            {/* headline */}
            <h2
              className="max-w-4xl text-[clamp(2.2rem,5vw,5rem)] font-extrabold uppercase
                leading-[1.05] tracking-tight text-[#2b2620]"
            >
              THE ONLY TASKBOARD{" "}
              <span className="relative inline-block whitespace-nowrap">
                <span className="relative z-10 rounded-lg px-3 text-[#fff8ec]">YOUR TEAM</span>
                <span className="absolute inset-0 rounded-lg bg-[#2b2620]" />
              </span>{" "}
              WILL EVER NEED.
            </h2>

            {/* sub-copy */}
            <p className="mt-5 max-w-xl text-base leading-7 text-[#6f655b] md:text-lg">
              A mini Jira-style workspace for fast-moving teams — project ownership, task
              lifecycle, collaboration &amp; dashboard analytics in one clean UI.
            </p>

            {/* CTAs */}
            <div className="mt-8 flex flex-col items-center gap-3 sm:flex-row">
              {isAuthenticated ? (
                <Link
                  href="/dashboard"
                  className="inline-flex items-center gap-2 rounded-full bg-[#2b2620] px-8
                    py-3.5 text-sm font-semibold text-white transition hover:bg-[#bc4a3c]"
                >
                  Go to dashboard <ArrowUpRight className="h-4 w-4" />
                </Link>
              ) : (
                <>
                  <Link
                    href="/auth/login"
                    className="inline-flex w-full items-center justify-center gap-2 rounded-full
                      bg-[#2b2620] px-8 py-3.5 text-sm font-semibold text-white transition
                      hover:bg-[#bc4a3c] sm:w-auto"
                  >
                    Get Started <ArrowUpRight className="h-4 w-4" />
                  </Link>
                  <Link
                    href="/auth/register"
                    className="inline-flex w-full items-center justify-center gap-2 rounded-full
                      border-2 border-[#2b2620] bg-transparent px-8 py-3.5 text-sm font-semibold
                      text-[#2b2620] transition hover:bg-[#2b2620] hover:text-white sm:w-auto"
                  >
                    Learn More <ArrowUpRight className="h-4 w-4" />
                  </Link>
                </>
              )}
            </div>

            {/* trust row */}
            <div className="mt-5 flex flex-wrap justify-center gap-4 text-xs text-[#6f655b]">
              {["Free to use", "Open source", "Production-grade API"].map((t) => (
                <span key={t} className="flex items-center gap-1.5">
                  <CheckCircle2 className="h-3.5 w-3.5 text-[#256a4f]" />
                  {t}
                </span>
              ))}
            </div>
          </div>

          {/* ── Dashboard preview: full-width, bleeds off the bottom ── */}
          <div className="relative mt-auto px-4 sm:px-8 md:px-12 lg:px-16 xl:px-24">
            <DashboardMock />
          </div>

        </div>
      </section>

      {/* ══════════════════════════════════════
          FEATURES
      ══════════════════════════════════════ */}
      <section className="bg-[#f3ecdf] px-5 py-16 md:px-12 lg:px-20 xl:px-28">
        <p className="mb-2 text-center text-xs font-semibold uppercase tracking-[0.22em] text-[#bc4a3c]">
          Why Taskboard
        </p>
        <h3 className="mb-10 text-center text-2xl font-extrabold tracking-tight text-[#2b2620] md:text-3xl">
          Everything your team needs
        </h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {features.map((f) => {
            const Icon = f.icon;
            return (
              <div
                key={f.title}
                className="group rounded-2xl border border-[#d6c6ad] bg-[#fff8ec] p-6
                  transition duration-200 hover:-translate-y-1
                  hover:shadow-[0_10px_36px_rgba(44,31,17,0.12)]"
              >
                <div
                  className={`mb-4 inline-flex h-10 w-10 items-center justify-center
                    rounded-xl ${f.accent}`}
                >
                  <Icon className="h-5 w-5" />
                </div>
                <h4 className="mb-2 text-sm font-semibold text-[#2b2620]">{f.title}</h4>
                <p className="text-sm leading-6 text-[#6f655b]">{f.desc}</p>
              </div>
            );
          })}
        </div>
      </section>

      {/* ══════════════════════════════════════
          BENTO FEATURE CARDS
      ══════════════════════════════════════ */}
      <section className="bg-[#f3ecdf] px-5 pb-16 md:px-12 lg:px-20 xl:px-28">
        {/* heading */}
        <div className="mb-10 max-w-2xl">
          <h3 className="text-3xl font-extrabold leading-tight tracking-tight text-[#2b2620] md:text-4xl">
            If all your{" "}
            <span className="relative inline-block">
              <span className="relative z-10">project needs</span>
              <span
                className="absolute bottom-1 left-0 right-0 h-3 -z-10 rounded"
                style={{ background: "rgba(188,74,60,0.18)" }}
              />
            </span>{" "}
            and wants were a place — that's us.
          </h3>
          <p className="mt-4 text-sm leading-6 text-[#6f655b]">
            Built for teams who move fast. Ship tasks, track progress, and collaborate without
            the overhead of complex tooling.
          </p>
        </div>

        {/* 2×2 bento grid */}
        <div className="grid gap-4 sm:grid-cols-2">

          {/* Card 1 — light */}
          <div className="group relative overflow-hidden rounded-3xl border border-[#d6c6ad] bg-[#fff8ec] p-8 transition hover:shadow-[0_10px_40px_rgba(44,31,17,0.12)]">
            {/* decorative circles */}
            <div className="pointer-events-none absolute -top-8 -right-8 h-36 w-36 rounded-full border-[16px] border-[#e5d6bf] opacity-50" />
            <div className="pointer-events-none absolute -bottom-10 -right-4 h-24 w-24 rounded-full border-[10px] border-[#e5d6bf] opacity-30" />
            <h4 className="mb-1 max-w-[55%] text-xl font-bold leading-snug text-[#2b2620]">
              Task Management
            </h4>
            <p className="mb-10 max-w-[55%] text-xs text-[#6f655b]">
              Create, assign, and track tasks with priority labels, due dates, and status flows.
            </p>
            <Link
              href={isAuthenticated ? "/tasks" : "/auth/register"}
              className="inline-flex items-center gap-2 rounded-full border border-[#2b2620] px-4 py-2 text-xs font-semibold text-[#2b2620] transition hover:bg-[#2b2620] hover:text-white"
            >
              <ArrowUpRight className="h-3.5 w-3.5" /> Learn More
            </Link>
          </div>

          {/* Card 2 — accent warm */}
          <div className="group relative overflow-hidden rounded-3xl p-8 transition hover:brightness-95" style={{ background: "#e8d5c0" }}>
            <div className="pointer-events-none absolute -top-8 -left-8 h-40 w-40 rounded-full border-[16px] border-[#d6c6ad]/60" />
            <div className="pointer-events-none absolute -bottom-6 -right-6 h-28 w-28 rounded-full border-[10px] border-[#d6c6ad]/40" />
            <h4 className="mb-1 max-w-[55%] text-xl font-bold leading-snug text-[#2b2620]">
              Project Collaboration
            </h4>
            <p className="mb-10 max-w-[55%] text-xs text-[#5a4e44]">
              Invite teammates, assign ownership, and manage project-level access with RBAC.
            </p>
            <Link
              href={isAuthenticated ? "/projects" : "/auth/register"}
              className="inline-flex items-center gap-2 rounded-full border border-[#2b2620] px-4 py-2 text-xs font-semibold text-[#2b2620] transition hover:bg-[#2b2620] hover:text-white"
            >
              <ArrowUpRight className="h-3.5 w-3.5" /> Learn More
            </Link>
          </div>

          {/* Card 3 — dark */}
          <div className="group relative overflow-hidden rounded-3xl p-8 transition hover:brightness-110" style={{ background: "#2b2620" }}>
            <div className="pointer-events-none absolute -bottom-10 -right-10 h-40 w-40 rounded-full border-[16px] border-white/5" />
            <div className="pointer-events-none absolute -top-6 -left-6 h-24 w-24 rounded-full border-[10px] border-white/5" />
            <h4 className="mb-1 max-w-[55%] text-xl font-bold leading-snug text-white">
              Dashboard Analytics
            </h4>
            <p className="mb-10 max-w-[55%] text-xs text-white/50">
              Monitor workload, track overdue tasks, and get real-time insights across your workspace.
            </p>
            <Link
              href={isAuthenticated ? "/dashboard" : "/auth/register"}
              className="inline-flex items-center gap-2 rounded-full border border-white/30 px-4 py-2 text-xs font-semibold text-white transition hover:bg-white hover:text-[#2b2620]"
            >
              <ArrowUpRight className="h-3.5 w-3.5" /> Learn More
            </Link>
          </div>

          {/* Card 4 — muted red */}
          <div className="group relative overflow-hidden rounded-3xl p-8 transition hover:brightness-95" style={{ background: "#eedcd9" }}>
            <div className="pointer-events-none absolute -top-8 -right-8 h-36 w-36 rounded-full border-[16px] border-[#d6b4af]/50" />
            <div className="pointer-events-none absolute -bottom-8 -left-8 h-28 w-28 rounded-full border-[10px] border-[#d6b4af]/40" />
            <h4 className="mb-1 max-w-[55%] text-xl font-bold leading-snug text-[#2b2620]">
              Team Invitations
            </h4>
            <p className="mb-10 max-w-[55%] text-xs text-[#5a4e44]">
              Send and manage invitations. Accept, decline, or revoke access — all from one place.
            </p>
            <Link
              href={isAuthenticated ? "/invitations" : "/auth/register"}
              className="inline-flex items-center gap-2 rounded-full border border-[#2b2620] px-4 py-2 text-xs font-semibold text-[#2b2620] transition hover:bg-[#2b2620] hover:text-white"
            >
              <ArrowUpRight className="h-3.5 w-3.5" /> Learn More
            </Link>
          </div>

        </div>
      </section>

      {/* ══════════════════════════════════════
          RICH FOOTER
      ══════════════════════════════════════ */}
      <footer className="bg-[#2b2620]">
        {/* main footer grid */}
        <div className="mx-auto max-w-7xl px-5 pt-14 pb-10 md:px-12">
          <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-5">

            {/* brand col */}
            <div className="sm:col-span-2 lg:col-span-2">
              <div className="mb-3 inline-flex items-center gap-2">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[#bc4a3c]">
                  <BarChart3 className="h-4 w-4 text-white" />
                </div>
                <span className="text-base font-bold text-white">GDG Taskboard</span>
              </div>
              <p className="max-w-xs text-sm leading-6 text-white/40">
                A production-grade task management workspace for fast-moving teams and hackathon
                projects.
              </p>
              <div className="mt-6 flex gap-3">
                {isAuthenticated ? (
                  <Link
                    href="/dashboard"
                    className="inline-flex items-center gap-2 rounded-full bg-[#bc4a3c] px-5 py-2 text-xs font-semibold text-white transition hover:bg-[#a03530]"
                  >
                    Open dashboard <ArrowRight className="h-3.5 w-3.5" />
                  </Link>
                ) : (
                  <>
                    <Link
                      href="/auth/register"
                      className="inline-flex items-center gap-2 rounded-full bg-[#bc4a3c] px-5 py-2 text-xs font-semibold text-white transition hover:bg-[#a03530]"
                    >
                      Create account
                    </Link>
                    <Link
                      href="/auth/login"
                      className="inline-flex items-center gap-2 rounded-full border border-white/20 px-5 py-2 text-xs font-semibold text-white/70 transition hover:border-white/50 hover:text-white"
                    >
                      Sign in
                    </Link>
                  </>
                )}
              </div>
            </div>

            {/* Product links */}
            <div>
              <p className="mb-4 text-[10px] font-bold uppercase tracking-[0.18em] text-white/30">Product</p>
              <ul className="space-y-3">
                {[
                  { label: "Dashboard", href: "/dashboard" },
                  { label: "Tasks", href: "/tasks" },
                  { label: "Projects", href: "/projects" },
                  { label: "Invitations", href: "/invitations" },
                  { label: "Settings", href: "/settings" },
                ].map((l) => (
                  <li key={l.label}>
                    <Link href={l.href} className="text-sm text-white/50 transition hover:text-white">
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>

            {/* Company links */}
            <div>
              <p className="mb-4 text-[10px] font-bold uppercase tracking-[0.18em] text-white/30">Company</p>
              <ul className="space-y-3">
                {[
                  { label: "About", href: "#" },
                  { label: "Open Source", href: "#" },
                  { label: "Contributing", href: "#" },
                  { label: "Changelog", href: "#" },
                ].map((l) => (
                  <li key={l.label}>
                    <Link href={l.href} className="text-sm text-white/50 transition hover:text-white">
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>

            {/* Resources links */}
            <div>
              <p className="mb-4 text-[10px] font-bold uppercase tracking-[0.18em] text-white/30">Resources</p>
              <ul className="space-y-3">
                {[
                  { label: "Documentation", href: "#" },
                  { label: "API Reference", href: "#" },
                  { label: "Support", href: "#" },
                  { label: "Privacy Policy", href: "#" },
                ].map((l) => (
                  <li key={l.label}>
                    <Link href={l.href} className="text-sm text-white/50 transition hover:text-white">
                      {l.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>

          </div>
        </div>

        {/* bottom bar */}
        <div className="border-t border-white/10">
          <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-3 px-5 py-5 text-xs text-white/30 sm:flex-row md:px-12">
            <span>© 2026 GDG Taskboard. All rights reserved.</span>
            <div className="flex items-center gap-4">
              {[
                { label: "GitHub", icon: "⬡" },
                { label: "Twitter", icon: "✕" },
                { label: "Email", icon: "✉" },
              ].map((s) => (
                <a
                  key={s.label}
                  href="#"
                  aria-label={s.label}
                  className="flex h-7 w-7 items-center justify-center rounded-full border border-white/10 text-white/30 transition hover:border-white/30 hover:text-white/60"
                >
                  <span className="text-[10px]">{s.icon}</span>
                </a>
              ))}
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
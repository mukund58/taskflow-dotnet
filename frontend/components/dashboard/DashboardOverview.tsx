"use client";

import Link from "next/link";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { type FormEvent, useCallback, useMemo, useState } from "react";
import type { LucideIcon } from "lucide-react";
import {
  AlertTriangle,
  Activity,
  CheckCircle2,
  ClipboardList,
  FolderKanban,
  Grid2X2,
  List,
  MailPlus,
  Plus,
  RefreshCw,
  Search,
  UserPlus,
  UsersRound,
} from "lucide-react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { type BadgeVariant, Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/Card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/Dialog";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/Select";
import { Table, type TableColumn } from "@/components/ui/Table";
import { cn } from "@/lib/utils";
import { ApiError } from "@/services/api";
import { getDashboardStats } from "@/services/dashboard";
import {
  addProjectMember,
  createProject,
  createProjectInvitation,
  getProjectInvitations,
  getProjectMembers,
  getProjects,
} from "@/services/project";
import { getTasks } from "@/services/task";
import type { Project, ProjectInvitation, ProjectMember } from "@/types/project";
import type { TaskItem } from "@/types/task";

type ViewMode = "grid" | "list";
type SortMode = "name" | "lastUpdated";
type ProjectRole = "Admin" | "Member";

type ProjectInsight = {
  project: Project;
  totalTasks: number;
  activeTasks: number;
  completedTasks: number;
  overdueTasks: number;
  contributors: number;
  latestTaskCreatedAt: string | null;
};

function getErrorMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

function isDoneTask(task: TaskItem) {
  return task.status.trim().toLowerCase() === "done";
}

function isOverdueTask(task: TaskItem) {
  if (!task.dueDate || isDoneTask(task)) {
    return false;
  }

  const dueAt = Date.parse(task.dueDate);
  if (Number.isNaN(dueAt)) {
    return false;
  }

  return dueAt < Date.now();
}

function getProjectInitials(name: string) {
  const words = name
    .split(" ")
    .map((word) => word.trim())
    .filter(Boolean)
    .slice(0, 2);

  if (words.length === 0) {
    return "PR";
  }

  return words.map((word) => word[0]?.toUpperCase() ?? "").join("");
}

function getProjectHealth(projectInsight: ProjectInsight): { label: string; variant: BadgeVariant } {
  if (projectInsight.overdueTasks > 0) {
    return { label: `${projectInsight.overdueTasks} overdue`, variant: "danger" };
  }

  if (projectInsight.activeTasks > 0) {
    return { label: `${projectInsight.activeTasks} active`, variant: "warning" };
  }

  if (projectInsight.totalTasks === 0) {
    return { label: "No tasks", variant: "neutral" };
  }

  return { label: "On track", variant: "success" };
}

function formatRelativeTime(isoDate: string | null) {
  if (!isoDate) {
    return "No recent activity";
  }

  const timestamp = Date.parse(isoDate);
  if (Number.isNaN(timestamp)) {
    return "Updated recently";
  }

  const elapsed = Date.now() - timestamp;
  if (elapsed < 0) {
    return "Updated recently";
  }

  const minute = 60 * 1000;
  const hour = 60 * minute;
  const day = 24 * hour;

  if (elapsed < hour) {
    const minutes = Math.max(1, Math.floor(elapsed / minute));
    return `Edited ${minutes} minute${minutes === 1 ? "" : "s"} ago`;
  }

  if (elapsed < day) {
    const hours = Math.floor(elapsed / hour);
    return `Edited ${hours} hour${hours === 1 ? "" : "s"} ago`;
  }

  const days = Math.floor(elapsed / day);
  return `Edited ${days} day${days === 1 ? "" : "s"} ago`;
}

function formatTimestamp(isoDate: string | null | undefined) {
  if (!isoDate) {
    return "-";
  }

  const timestamp = Date.parse(isoDate);
  if (Number.isNaN(timestamp)) {
    return "-";
  }

  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(timestamp));
}

function buildProjectInsights(projects: Project[], tasks: TaskItem[]) {
  const tasksByProject = new Map<string, TaskItem[]>();

  tasks.forEach((task) => {
    const bucket = tasksByProject.get(task.projectId) ?? [];
    bucket.push(task);
    tasksByProject.set(task.projectId, bucket);
  });

  return projects.map((project) => {
    const projectTasks = tasksByProject.get(project.id) ?? [];
    const activeTasks = projectTasks.filter((task) => !isDoneTask(task)).length;
    const completedTasks = projectTasks.filter((task) => isDoneTask(task)).length;
    const overdueTasks = projectTasks.filter((task) => isOverdueTask(task)).length;
    const contributors = new Set(
      projectTasks
        .map((task) => task.assignedUserId)
        .filter((assignedUserId): assignedUserId is string => Boolean(assignedUserId)),
    ).size;

    const latestTaskCreatedAt = projectTasks
      .map((task) => task.createdAt ?? null)
      .filter((createdAt): createdAt is string => Boolean(createdAt))
      .sort((left, right) => {
        const leftTimestamp = Date.parse(left);
        const rightTimestamp = Date.parse(right);

        const safeLeft = Number.isNaN(leftTimestamp) ? 0 : leftTimestamp;
        const safeRight = Number.isNaN(rightTimestamp) ? 0 : rightTimestamp;

        return safeRight - safeLeft;
      })[0] ?? null;

    return {
      project,
      totalTasks: projectTasks.length,
      activeTasks,
      completedTasks,
      overdueTasks,
      contributors,
      latestTaskCreatedAt,
    } satisfies ProjectInsight;
  });
}

function StatCard({
  label,
  value,
  icon: Icon,
}: {
  label: string;
  value: number;
  icon: LucideIcon;
}) {
  return (
    <Card className="border-border/80 bg-card/85 shadow-none">
      <CardHeader className="pb-3">
        <CardDescription className="flex items-center gap-2 text-xs uppercase tracking-[0.12em]">
          <Icon className="h-3.5 w-3.5" />
          {label}
        </CardDescription>
        <CardTitle className="text-2xl font-semibold md:text-3xl">{value}</CardTitle>
      </CardHeader>
    </Card>
  );
}

function ProjectGridCard({
  insight,
  onManageTeam,
}: {
  insight: ProjectInsight;
  onManageTeam: (insight: ProjectInsight) => void;
}) {
  const health = getProjectHealth(insight);

  return (
    <Card className="border-border/85 bg-card/95 shadow-[0_10px_20px_rgba(44,31,17,0.06)] transition hover:-translate-y-0.5 hover:shadow-[0_16px_30px_rgba(44,31,17,0.1)]">
      <CardHeader className="space-y-4">
        <div className="flex items-start justify-between gap-3">
          <div className="flex min-w-0 items-start gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-border bg-secondary/55 text-sm font-semibold text-foreground">
              {getProjectInitials(insight.project.name)}
            </div>
            <div className="min-w-0">
              <CardTitle className="truncate text-lg leading-6">{insight.project.name}</CardTitle>
              <CardDescription className="mt-1 line-clamp-2 text-sm leading-6">
                {insight.project.description || "No project description provided yet."}
              </CardDescription>
            </div>
          </div>

          <Badge variant="secondary" className="shrink-0">
            {insight.totalTasks} tasks
          </Badge>
        </div>

        <div className="flex flex-wrap gap-2">
          <Badge variant="outline">No active sprints</Badge>
          <Badge variant={health.variant}>{health.label}</Badge>
          <Badge variant="neutral">{insight.completedTasks} done</Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-4 pb-6">
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div className="rounded-lg border border-border/80 bg-background/55 p-3">
            <p className="text-xs text-muted-foreground">Contributors</p>
            <p className="mt-1 font-medium text-foreground">{insight.contributors || 1}</p>
          </div>

          <div className="rounded-lg border border-border/80 bg-background/55 p-3">
            <p className="text-xs text-muted-foreground">Open tasks</p>
            <p className="mt-1 font-medium text-foreground">{insight.activeTasks}</p>
          </div>
        </div>

        <p className="text-xs text-muted-foreground">{formatRelativeTime(insight.latestTaskCreatedAt)}</p>

        <Button variant="outline" size="sm" className="w-full" onClick={() => onManageTeam(insight)}>
          Manage team
        </Button>
      </CardContent>
    </Card>
  );
}

export function DashboardOverview() {
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState("");
  const [activeOnly, setActiveOnly] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>("grid");
  const [sortMode, setSortMode] = useState<SortMode>("name");
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [projectNameDraft, setProjectNameDraft] = useState("");
  const [projectDescriptionDraft, setProjectDescriptionDraft] = useState("");
  const [projectDueDateDraft, setProjectDueDateDraft] = useState("");
  const [isCreatingProject, setIsCreatingProject] = useState(false);
  const [createProjectError, setCreateProjectError] = useState<string | null>(null);

  const [selectedProject, setSelectedProject] = useState<ProjectInsight | null>(null);
  const [isTeamDialogOpen, setIsTeamDialogOpen] = useState(false);
  const [teamMembers, setTeamMembers] = useState<ProjectMember[]>([]);
  const [teamInvitations, setTeamInvitations] = useState<ProjectInvitation[]>([]);
  const [isTeamDataLoading, setIsTeamDataLoading] = useState(false);
  const [teamDataError, setTeamDataError] = useState<string | null>(null);

  const [memberEmailDraft, setMemberEmailDraft] = useState("");
  const [memberRoleDraft, setMemberRoleDraft] = useState<ProjectRole>("Member");
  const [isAddingMember, setIsAddingMember] = useState(false);
  const [memberActionMessage, setMemberActionMessage] = useState<string | null>(null);
  const [memberActionError, setMemberActionError] = useState<string | null>(null);

  const [invitationEmailDraft, setInvitationEmailDraft] = useState("");
  const [invitationRoleDraft, setInvitationRoleDraft] = useState<ProjectRole>("Member");
  const [isSendingInvitation, setIsSendingInvitation] = useState(false);
  const [invitationActionMessage, setInvitationActionMessage] = useState<string | null>(null);
  const [invitationActionError, setInvitationActionError] = useState<string | null>(null);

  const projectsQuery = useQuery({
    queryKey: ["projects", "dashboard"],
    queryFn: () => getProjects(),
  });

  const tasksQuery = useQuery({
    queryKey: ["tasks", "dashboard"],
    queryFn: () => getTasks({ page: 1, pageSize: 300, sortBy: "createdAt", sortDescending: true }),
  });

  const statsQuery = useQuery({
    queryKey: ["dashboard-stats"],
    queryFn: () => getDashboardStats(),
    retry: false,
  });

  const projects = projectsQuery.data ?? [];
  const tasks = tasksQuery.data?.items ?? [];
  const stats = statsQuery.data ?? null;
  const isLoading = projectsQuery.isLoading || tasksQuery.isLoading;
  const isRefreshing = projectsQuery.isFetching || tasksQuery.isFetching || statsQuery.isFetching;
  const error = projectsQuery.isError ? getErrorMessage(projectsQuery.error, "Unable to load projects.") : null;

  const statsNotice = useMemo(() => {
    if (!statsQuery.isError) {
      return null;
    }

    if (
      statsQuery.error instanceof ApiError &&
      (statsQuery.error.status === 401 || statsQuery.error.status === 403)
    ) {
      return "Detailed analytics are available for Admin and Manager roles.";
    }

    return "Dashboard analytics are temporarily unavailable.";
  }, [statsQuery.error, statsQuery.isError]);

  const refreshDashboard = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["projects"] }),
      queryClient.invalidateQueries({ queryKey: ["tasks"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard-stats"] }),
    ]);
  }, [queryClient]);

  const resetCreateProjectForm = useCallback(() => {
    setProjectNameDraft("");
    setProjectDescriptionDraft("");
    setProjectDueDateDraft("");
    setCreateProjectError(null);
  }, []);

  const loadProjectTeamData = useCallback(async (projectId: string) => {
    setIsTeamDataLoading(true);
    setTeamDataError(null);

    const [membersResult, invitationsResult] = await Promise.allSettled([
      getProjectMembers(projectId),
      getProjectInvitations(projectId),
    ]);

    let nextError: string | null = null;

    if (membersResult.status === "fulfilled") {
      setTeamMembers(membersResult.value);
    } else {
      setTeamMembers([]);
      nextError = getErrorMessage(membersResult.reason, "Unable to load project members.");
    }

    if (invitationsResult.status === "fulfilled") {
      setTeamInvitations(invitationsResult.value);
    } else {
      setTeamInvitations([]);

      if (!nextError) {
        nextError = getErrorMessage(invitationsResult.reason, "Unable to load project invitations.");
      }
    }

    setTeamDataError(nextError);

    setIsTeamDataLoading(false);
  }, []);

  const openTeamDialog = useCallback(
    async (projectInsight: ProjectInsight) => {
      setSelectedProject(projectInsight);
      setIsTeamDialogOpen(true);
      setMemberEmailDraft("");
      setMemberRoleDraft("Member");
      setInvitationEmailDraft("");
      setInvitationRoleDraft("Member");
      setMemberActionError(null);
      setMemberActionMessage(null);
      setInvitationActionError(null);
      setInvitationActionMessage(null);

      await loadProjectTeamData(projectInsight.project.id);
    },
    [loadProjectTeamData],
  );

  const closeTeamDialog = useCallback(() => {
    setIsTeamDialogOpen(false);
    setSelectedProject(null);
    setTeamMembers([]);
    setTeamInvitations([]);
    setTeamDataError(null);
  }, []);

  const handleCreateProject = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      try {
        setIsCreatingProject(true);
        setCreateProjectError(null);

        await createProject({
          name: projectNameDraft.trim(),
          description: projectDescriptionDraft.trim(),
          dueDate: projectDueDateDraft ? new Date(projectDueDateDraft).toISOString() : null,
        });

        setIsCreateDialogOpen(false);
        resetCreateProjectForm();
        await refreshDashboard();
      } catch (caughtError) {
        setCreateProjectError(getErrorMessage(caughtError, "Unable to create project."));
      } finally {
        setIsCreatingProject(false);
      }
    },
    [projectDescriptionDraft, projectDueDateDraft, projectNameDraft, refreshDashboard, resetCreateProjectForm],
  );

  const handleAddMember = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!selectedProject) {
        return;
      }

      try {
        setIsAddingMember(true);
        setMemberActionError(null);
        setMemberActionMessage(null);

        await addProjectMember(selectedProject.project.id, {
          email: memberEmailDraft.trim(),
          role: memberRoleDraft,
        });

        setMemberActionMessage("Member added successfully.");
        setMemberEmailDraft("");
        await loadProjectTeamData(selectedProject.project.id);
        await refreshDashboard();
      } catch (caughtError) {
        setMemberActionError(getErrorMessage(caughtError, "Unable to add member."));
      } finally {
        setIsAddingMember(false);
      }
    },
    [loadProjectTeamData, memberEmailDraft, memberRoleDraft, refreshDashboard, selectedProject],
  );

  const handleSendInvitation = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!selectedProject) {
        return;
      }

      try {
        setIsSendingInvitation(true);
        setInvitationActionError(null);
        setInvitationActionMessage(null);

        await createProjectInvitation(selectedProject.project.id, {
          email: invitationEmailDraft.trim(),
          role: invitationRoleDraft,
          expiresInDays: 7,
        });

        setInvitationActionMessage("Invitation sent successfully.");
        setInvitationEmailDraft("");
        await loadProjectTeamData(selectedProject.project.id);
      } catch (caughtError) {
        setInvitationActionError(getErrorMessage(caughtError, "Unable to send invitation."));
      } finally {
        setIsSendingInvitation(false);
      }
    },
    [invitationEmailDraft, invitationRoleDraft, loadProjectTeamData, selectedProject],
  );

  const fallbackTotals = useMemo(() => {
    const completedTasks = tasks.filter((task) => isDoneTask(task)).length;
    const overdueTasks = tasks.filter((task) => isOverdueTask(task)).length;
    const activeTasks = tasks.length - completedTasks;
    const totalUsers = new Set(
      tasks
        .map((task) => task.assignedUserId)
        .filter((assignedUserId): assignedUserId is string => Boolean(assignedUserId)),
    ).size;

    return {
      totalTasks: tasks.length,
      activeTasks,
      completedTasks,
      overdueTasks,
      totalUsers,
    };
  }, [tasks]);

  const projectInsights = useMemo(() => buildProjectInsights(projects, tasks), [projects, tasks]);

  const visibleProjects = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    const filtered = projectInsights.filter((insight) => {
      if (activeOnly && insight.activeTasks === 0) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return (
        insight.project.name.toLowerCase().includes(normalizedQuery) ||
        insight.project.description.toLowerCase().includes(normalizedQuery)
      );
    });

    return filtered.sort((left, right) => {
      if (sortMode === "name") {
        return left.project.name.localeCompare(right.project.name);
      }

      const leftTimestamp = left.latestTaskCreatedAt ? Date.parse(left.latestTaskCreatedAt) : 0;
      const rightTimestamp = right.latestTaskCreatedAt ? Date.parse(right.latestTaskCreatedAt) : 0;

      const safeLeft = Number.isNaN(leftTimestamp) ? 0 : leftTimestamp;
      const safeRight = Number.isNaN(rightTimestamp) ? 0 : rightTimestamp;

      return safeRight - safeLeft;
    });
  }, [activeOnly, projectInsights, searchQuery, sortMode]);

  const summary = {
    totalProjects: projects.length,
    totalTasks: stats?.totalTasks ?? fallbackTotals.totalTasks,
    activeTasks: stats?.activeTasks ?? fallbackTotals.activeTasks,
    completedTasks: stats?.completedTasks ?? fallbackTotals.completedTasks,
    overdueTasks: stats?.overdueTasks ?? fallbackTotals.overdueTasks,
    totalUsers: stats?.totalUsers ?? fallbackTotals.totalUsers,
  };

  const tasksPerUserChartData = useMemo(() => {
    return (stats?.tasksPerUser ?? []).map((entry) => ({
      name: entry.userName,
      active: entry.activeTasks,
      done: entry.completedTasks,
      overdue: entry.overdueTasks,
    }));
  }, [stats?.tasksPerUser]);

  const statusPieData = useMemo(() => {
    const source = stats?.tasksByStatus ?? {};

    return Object.entries(source).map(([name, value]) => ({
      name,
      value,
    }));
  }, [stats?.tasksByStatus]);

  const listColumns: Array<TableColumn<ProjectInsight>> = [
    {
      key: "project",
      header: "Project",
      render: (row) => (
        <div className="space-y-1">
          <p className="font-medium text-foreground">{row.project.name}</p>
          <p className="line-clamp-1 text-xs text-muted-foreground">
            {row.project.description || "No description"}
          </p>
        </div>
      ),
    },
    {
      key: "health",
      header: "Health",
      render: (row) => {
        const health = getProjectHealth(row);

        return <Badge variant={health.variant}>{health.label}</Badge>;
      },
    },
    {
      key: "tasks",
      header: "Task Split",
      render: (row) => (
        <p className="text-xs text-muted-foreground">
          {row.activeTasks} active / {row.completedTasks} done / {row.overdueTasks} overdue
        </p>
      ),
    },
    {
      key: "contributors",
      header: "Contributors",
      render: (row) => <p>{row.contributors || 1}</p>,
    },
    {
      key: "updated",
      header: "Last Updated",
      render: (row) => <p className="text-xs text-muted-foreground">{formatRelativeTime(row.latestTaskCreatedAt)}</p>,
    },
    {
      key: "actions",
      header: "Actions",
      className: "text-right",
      render: (row) => (
        <Button variant="outline" size="sm" onClick={() => void openTeamDialog(row)}>
          Manage Team
        </Button>
      ),
    },
  ];

  if (error) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Dashboard is unavailable</CardTitle>
          <CardDescription>{error}</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2 pb-6">
          <Button asChild>
            <Link href="/auth/login">Go to sign in</Link>
          </Button>
          <Button variant="outline" onClick={() => void refreshDashboard()}>
            Retry
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <section className="surface-card relative overflow-hidden p-6 md:p-8">
        <div className="pointer-events-none absolute -top-16 -left-14 h-40 w-40 rounded-full bg-primary/12 blur-3xl" />
        <div className="pointer-events-none absolute -right-10 -bottom-16 h-36 w-36 rounded-full bg-[var(--color-success)]/12 blur-3xl" />

        <div className="relative z-10 space-y-6">
          <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
            <div className="space-y-2">
              <Badge variant="secondary">Live Dashboard</Badge>
              <h2 className="text-3xl font-semibold tracking-tight">Projects</h2>
              <p className="text-sm text-muted-foreground">Manage all your projects with backend-powered insights.</p>
            </div>

            <div className="flex flex-wrap gap-2">
              <Button size="sm" onClick={() => setIsCreateDialogOpen(true)}>
                <Plus className="h-4 w-4" />
                Create project
              </Button>
              <Button variant="outline" size="sm" onClick={() => void refreshDashboard()} isLoading={isRefreshing}>
                <RefreshCw className="h-4 w-4" />
                Refresh
              </Button>
            </div>
          </div>

          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <StatCard label="Total Projects" value={summary.totalProjects} icon={FolderKanban} />
            <StatCard label="Total Tasks" value={summary.totalTasks} icon={ClipboardList} />
            <StatCard label="Active Tasks" value={summary.activeTasks} icon={Activity} />
            <StatCard label="Completed" value={summary.completedTasks} icon={CheckCircle2} />
            <StatCard label="Overdue" value={summary.overdueTasks} icon={AlertTriangle} />
          </div>

          {statsNotice ? (
            <p className="rounded-lg border border-border/80 bg-background/55 px-3 py-2 text-xs text-muted-foreground">
              {statsNotice}
            </p>
          ) : null}

          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <UsersRound className="h-3.5 w-3.5" />
            <span>{summary.totalUsers} contributors across active work.</span>
          </div>
        </div>
      </section>

      <section className="surface-card p-4 md:p-5">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              className="h-10 bg-background/60 pl-9"
              placeholder="Search projects..."
              aria-label="Search projects"
            />
          </div>

          <Button
            variant={activeOnly ? "secondary" : "outline"}
            size="sm"
            onClick={() => setActiveOnly((currentValue) => !currentValue)}
          >
            <Activity className="h-4 w-4" />
            Active projects only
          </Button>

          <div className="inline-flex rounded-lg border border-border bg-background/55 p-1">
            <button
              type="button"
              onClick={() => setViewMode("grid")}
              className={cn(
                "flex h-8 items-center gap-2 rounded-md px-3 text-xs font-medium transition",
                viewMode === "grid" ? "bg-card text-foreground" : "text-muted-foreground hover:text-foreground",
              )}
              aria-label="Grid view"
            >
              <Grid2X2 className="h-4 w-4" />
              Grid
            </button>

            <button
              type="button"
              onClick={() => setViewMode("list")}
              className={cn(
                "flex h-8 items-center gap-2 rounded-md px-3 text-xs font-medium transition",
                viewMode === "list" ? "bg-card text-foreground" : "text-muted-foreground hover:text-foreground",
              )}
              aria-label="List view"
            >
              <List className="h-4 w-4" />
              List
            </button>
          </div>
        </div>

        <div className="mt-3 flex flex-wrap items-center gap-2 text-xs">
          <span className="text-muted-foreground">Sort by:</span>

          <button
            type="button"
            onClick={() => setSortMode("name")}
            className={cn(
              "rounded-full border px-3 py-1.5 font-medium transition",
              sortMode === "name"
                ? "border-border bg-card text-foreground"
                : "border-border/70 text-muted-foreground hover:text-foreground",
            )}
          >
            Name
          </button>

          <button
            type="button"
            onClick={() => setSortMode("lastUpdated")}
            className={cn(
              "rounded-full border px-3 py-1.5 font-medium transition",
              sortMode === "lastUpdated"
                ? "border-border bg-card text-foreground"
                : "border-border/70 text-muted-foreground hover:text-foreground",
            )}
          >
            Last updated
          </button>

          <span className="ml-auto text-muted-foreground">{visibleProjects.length} shown</span>
        </div>
      </section>

      {stats ? (
        <section className="grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Tasks Per User</CardTitle>
              <CardDescription>Active, completed, and overdue task split by user.</CardDescription>
            </CardHeader>
            <CardContent className="h-[280px] pb-6">
              {tasksPerUserChartData.length === 0 ? (
                <p className="text-sm text-muted-foreground">No user workload data available.</p>
              ) : (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={tasksPerUserChartData} margin={{ top: 8, right: 8, bottom: 8, left: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" fontSize={11} />
                    <YAxis allowDecimals={false} fontSize={11} />
                    <Tooltip />
                    <Bar dataKey="active" stackId="tasks" fill="#bc4a3c" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="done" stackId="tasks" fill="#256a4f" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="overdue" stackId="tasks" fill="#8b5a15" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Workload Distribution</CardTitle>
              <CardDescription>Task status share and system-wide workload indicators.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 pb-6">
              <div className="h-[200px]">
                {statusPieData.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No status data available.</p>
                ) : (
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={statusPieData}
                        dataKey="value"
                        nameKey="name"
                        cx="50%"
                        cy="50%"
                        outerRadius={70}
                        fill="#bc4a3c"
                        label
                      />
                      <Tooltip />
                    </PieChart>
                  </ResponsiveContainer>
                )}
              </div>

              <div className="grid grid-cols-2 gap-3 text-xs text-muted-foreground">
                <div className="rounded-lg border border-border/80 bg-background/55 p-3">
                  <p>Avg tasks/user</p>
                  <p className="mt-1 text-sm font-semibold text-foreground">
                    {stats.workloadDistribution.averageTasksPerUser.toFixed(1)}
                  </p>
                </div>
                <div className="rounded-lg border border-border/80 bg-background/55 p-3">
                  <p>Avg active/user</p>
                  <p className="mt-1 text-sm font-semibold text-foreground">
                    {stats.workloadDistribution.averageActiveTasks.toFixed(1)}
                  </p>
                </div>
                <div className="rounded-lg border border-border/80 bg-background/55 p-3">
                  <p>Due today</p>
                  <p className="mt-1 text-sm font-semibold text-foreground">
                    {stats.workloadDistribution.tasksDueToday}
                  </p>
                </div>
                <div className="rounded-lg border border-border/80 bg-background/55 p-3">
                  <p>Due this week</p>
                  <p className="mt-1 text-sm font-semibold text-foreground">
                    {stats.workloadDistribution.tasksDueThisWeek}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </section>
      ) : null}

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }).map((_, index) => (
            <Card key={index} className="animate-pulse border-border/80 bg-card/80">
              <CardHeader className="space-y-4">
                <div className="h-5 w-28 rounded bg-muted" />
                <div className="h-4 w-full rounded bg-muted" />
                <div className="h-4 w-2/3 rounded bg-muted" />
              </CardHeader>
              <CardContent className="pb-6">
                <div className="h-20 rounded-lg bg-muted" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : null}

      {!isLoading && visibleProjects.length === 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>No projects found</CardTitle>
            <CardDescription>
              Try a different search term, disable the active-only filter, or create a new project from this dashboard.
            </CardDescription>
          </CardHeader>
          <CardContent className="pb-6">
            <Button variant="outline" onClick={() => void refreshDashboard()}>
              Refresh data
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {!isLoading && visibleProjects.length > 0 ? (
        viewMode === "grid" ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {visibleProjects.map((insight) => (
              <ProjectGridCard key={insight.project.id} insight={insight} onManageTeam={openTeamDialog} />
            ))}
          </div>
        ) : (
          <Table columns={listColumns} rows={visibleProjects} rowKey={(row) => row.project.id} />
        )
      ) : null}

      <Dialog
        open={isCreateDialogOpen}
        onOpenChange={(nextOpen) => {
          setIsCreateDialogOpen(nextOpen);

          if (!nextOpen) {
            resetCreateProjectForm();
          }
        }}
      >
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Create project</DialogTitle>
            <DialogDescription>
              You will become the project Admin automatically and can then add teammates or send invitations.
            </DialogDescription>
          </DialogHeader>

          <form id="create-project-form" className="space-y-4" onSubmit={(event) => void handleCreateProject(event)}>
            <div className="space-y-1.5">
              <Label htmlFor="project-name">Project name</Label>
              <Input
                id="project-name"
                value={projectNameDraft}
                onChange={(event) => setProjectNameDraft(event.target.value)}
                placeholder="Team Launch Plan"
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="project-description">Description</Label>
              <Input
                id="project-description"
                value={projectDescriptionDraft}
                onChange={(event) => setProjectDescriptionDraft(event.target.value)}
                placeholder="Short summary for this project"
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="project-due-date">Due date</Label>
              <Input
                id="project-due-date"
                type="date"
                value={projectDueDateDraft}
                onChange={(event) => setProjectDueDateDraft(event.target.value)}
              />
            </div>

            {createProjectError ? <p className="text-sm text-destructive">{createProjectError}</p> : null}
          </form>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              type="submit"
              form="create-project-form"
              isLoading={isCreatingProject}
              disabled={!projectNameDraft.trim()}
            >
              Create project
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isTeamDialogOpen} onOpenChange={(nextOpen) => !nextOpen && closeTeamDialog()}>
        <DialogContent className="max-h-[88vh] max-w-4xl overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{selectedProject?.project.name ?? "Project team"}</DialogTitle>
            <DialogDescription>
              Add existing users directly or send invitations by email.
            </DialogDescription>
          </DialogHeader>

          {teamDataError ? <p className="text-sm text-destructive">{teamDataError}</p> : null}

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Members</CardTitle>
                <CardDescription>Current team members with project access.</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3 pb-6">
                {isTeamDataLoading ? (
                  <p className="text-sm text-muted-foreground">Loading members...</p>
                ) : teamMembers.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No members yet.</p>
                ) : (
                  teamMembers.map((member) => (
                    <div
                      key={member.userId}
                      className="flex items-center justify-between gap-2 rounded-lg border border-border/80 bg-background/55 p-3"
                    >
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-foreground">{member.name}</p>
                        <p className="truncate text-xs text-muted-foreground">{member.email}</p>
                      </div>
                      <div className="flex shrink-0 gap-2">
                        {member.isProjectOwner ? <Badge variant="secondary">Owner</Badge> : null}
                        <Badge variant={member.role === "Admin" ? "warning" : "neutral"}>{member.role}</Badge>
                      </div>
                    </div>
                  ))
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Invitations</CardTitle>
                <CardDescription>Pending and past invites for this project.</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3 pb-6">
                {isTeamDataLoading ? (
                  <p className="text-sm text-muted-foreground">Loading invitations...</p>
                ) : teamInvitations.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No invitations yet.</p>
                ) : (
                  teamInvitations.map((invitation) => (
                    <div
                      key={invitation.id}
                      className="space-y-1 rounded-lg border border-border/80 bg-background/55 p-3"
                    >
                      <div className="flex items-center justify-between gap-2">
                        <p className="truncate text-sm font-medium text-foreground">{invitation.email}</p>
                        <Badge variant={invitation.status === "Pending" ? "warning" : "neutral"}>
                          {invitation.status}
                        </Badge>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Role: {invitation.role} • Expires: {formatTimestamp(invitation.expiresAt)}
                      </p>
                    </div>
                  ))
                )}
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Add member</CardTitle>
                <CardDescription>Add an existing user to this project by email.</CardDescription>
              </CardHeader>
              <CardContent className="pb-6">
                <form className="space-y-3" onSubmit={(event) => void handleAddMember(event)}>
                  <Input
                    value={memberEmailDraft}
                    onChange={(event) => setMemberEmailDraft(event.target.value)}
                    placeholder="member@example.com"
                    type="email"
                    required
                  />

                  <Select value={memberRoleDraft} onValueChange={(value) => setMemberRoleDraft(value as ProjectRole)}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select role" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Member">Member</SelectItem>
                      <SelectItem value="Admin">Admin</SelectItem>
                    </SelectContent>
                  </Select>

                  {memberActionError ? <p className="text-xs text-destructive">{memberActionError}</p> : null}
                  {memberActionMessage ? <p className="text-xs text-[var(--color-success)]">{memberActionMessage}</p> : null}

                  <Button type="submit" className="w-full" isLoading={isAddingMember} disabled={!memberEmailDraft.trim()}>
                    <UserPlus className="h-4 w-4" />
                    Add member
                  </Button>
                </form>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Send invitation</CardTitle>
                <CardDescription>Invite someone who is not yet a member.</CardDescription>
              </CardHeader>
              <CardContent className="pb-6">
                <form className="space-y-3" onSubmit={(event) => void handleSendInvitation(event)}>
                  <Input
                    value={invitationEmailDraft}
                    onChange={(event) => setInvitationEmailDraft(event.target.value)}
                    placeholder="invitee@example.com"
                    type="email"
                    required
                  />

                  <Select
                    value={invitationRoleDraft}
                    onValueChange={(value) => setInvitationRoleDraft(value as ProjectRole)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select role" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Member">Member</SelectItem>
                      <SelectItem value="Admin">Admin</SelectItem>
                    </SelectContent>
                  </Select>

                  {invitationActionError ? <p className="text-xs text-destructive">{invitationActionError}</p> : null}
                  {invitationActionMessage ? (
                    <p className="text-xs text-[var(--color-success)]">{invitationActionMessage}</p>
                  ) : null}

                  <Button
                    type="submit"
                    className="w-full"
                    variant="outline"
                    isLoading={isSendingInvitation}
                    disabled={!invitationEmailDraft.trim()}
                  >
                    <MailPlus className="h-4 w-4" />
                    Send invitation
                  </Button>
                </form>
              </CardContent>
            </Card>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

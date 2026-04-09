"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { type DragEvent, FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { KanbanSquare, List, Plus, RefreshCcw, Search, Trash2 } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
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
import { Table, type TableColumn } from "@/components/ui/Table";
import { ApiError } from "@/services/api";
import { getProjects } from "@/services/project";
import {
  createTask,
  deleteTask,
  getTasks,
  updateTaskStatus,
} from "@/services/task";
import { getUsers } from "@/services/user";
import type { TaskItem, TaskPriority } from "@/types/task";
import type { User } from "@/types/user";

type ViewMode = "table" | "kanban";

const statusColumns = ["Todo", "In Progress", "Done"];

function getErrorMessage(error: unknown, fallback = "Something went wrong") {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

function priorityVariant(priority: string): "danger" | "warning" | "neutral" {
  if (priority.toLowerCase() === "high") {
    return "danger";
  }

  if (priority.toLowerCase() === "medium") {
    return "warning";
  }

  return "neutral";
}

function statusVariant(status: string): "success" | "warning" | "neutral" {
  if (status.toLowerCase() === "done") {
    return "success";
  }

  if (status.toLowerCase() === "in progress") {
    return "warning";
  }

  return "neutral";
}

function formatDate(value?: string | null) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(date);
}

export function TaskListPanel() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();

  const page = Number(searchParams.get("page") ?? "1") || 1;
  const pageSize = Number(searchParams.get("pageSize") ?? "10") || 10;
  const status = searchParams.get("status") ?? "";
  const assignedTo = searchParams.get("assignedTo") ?? "";
  const sortBy = searchParams.get("sortBy") ?? "createdAt";
  const sortDescending = searchParams.get("sortDescending") !== "false";
  const queryText = searchParams.get("q") ?? "";
  const viewMode = (searchParams.get("view") as ViewMode | null) ?? "table";

  const [searchInput, setSearchInput] = useState(queryText);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [newTitle, setNewTitle] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newProjectId, setNewProjectId] = useState("");
  const [newPriority, setNewPriority] = useState<TaskPriority>("Medium");
  const [newDueDate, setNewDueDate] = useState("");
  const [draggingTaskId, setDraggingTaskId] = useState<string | null>(null);
  const [dragOverStatus, setDragOverStatus] = useState<string | null>(null);

  const updateUrl = useCallback((updates: Record<string, string | number | boolean | null | undefined>) => {
    const next = new URLSearchParams(searchParams.toString());

    Object.entries(updates).forEach(([key, value]) => {
      if (value === null || value === undefined || value === "") {
        next.delete(key);
      } else {
        next.set(key, String(value));
      }
    });

    router.replace(`${pathname}?${next.toString()}`);
  }, [pathname, router, searchParams]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      if (searchInput !== queryText) {
        updateUrl({ q: searchInput, page: 1 });
      }
    }, 350);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [searchInput, queryText, updateUrl]);

  const tasksQuery = useQuery({
    queryKey: ["tasks", page, pageSize, status, assignedTo, sortBy, sortDescending],
    queryFn: () =>
      getTasks({
        page,
        pageSize,
        status: status || undefined,
        assignedTo: assignedTo || undefined,
        sortBy,
        sortDescending,
      }),
  });

  const projectsQuery = useQuery({
    queryKey: ["projects", "lookup"],
    queryFn: () => getProjects(),
  });

  const usersQuery = useQuery({
    queryKey: ["users", "lookup"],
    queryFn: () => getUsers(),
    retry: false,
  });

  const createTaskMutation = useMutation({
    mutationFn: createTask,
    onSuccess: () => {
      toast.success("Task created");
      setIsCreateOpen(false);
      setNewTitle("");
      setNewDescription("");
      setNewProjectId("");
      setNewPriority("Medium");
      setNewDueDate("");
      void queryClient.invalidateQueries({ queryKey: ["tasks"] });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not create task"));
    },
  });

  const deleteTaskMutation = useMutation({
    mutationFn: deleteTask,
    onSuccess: () => {
      toast.success("Task deleted");
      void queryClient.invalidateQueries({ queryKey: ["tasks"] });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not delete task"));
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ taskId, statusValue, rowVersion }: { taskId: string; statusValue: string; rowVersion?: number }) =>
      updateTaskStatus(taskId, statusValue, rowVersion),
    onMutate: async ({ taskId, statusValue }) => {
      await queryClient.cancelQueries({ queryKey: ["tasks"] });

      const queryKey = ["tasks", page, pageSize, status, assignedTo, sortBy, sortDescending] as const;
      const previous = queryClient.getQueryData<{ items: TaskItem[] }>(queryKey);

      if (previous?.items) {
        queryClient.setQueryData(queryKey, {
          ...previous,
          items: previous.items.map((task) => (task.id === taskId ? { ...task, status: statusValue } : task)),
        });
      }

      return { previous, queryKey };
    },
    onError: (error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(context.queryKey, context.previous);
      }

      toast.error(getErrorMessage(error, "Could not update task status"));
    },
    onSuccess: () => {
      toast.success("Task status updated");
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: ["tasks"] });
    },
  });

  const rows = useMemo(() => {
    const baseItems = tasksQuery.data?.items ?? [];
    const q = queryText.trim().toLowerCase();

    if (!q) {
      return baseItems;
    }

    return baseItems.filter((task) => {
      return task.title.toLowerCase().includes(q) || task.description.toLowerCase().includes(q);
    });
  }, [tasksQuery.data?.items, queryText]);

  const usersById = useMemo(() => {
    const map = new Map<string, User>();
    (usersQuery.data ?? []).forEach((user) => {
      map.set(user.id, user);
    });
    return map;
  }, [usersQuery.data]);

  const columns: Array<TableColumn<TaskItem>> = [
    {
      key: "title",
      header: "Task",
      render: (row) => (
        <div className="space-y-1">
          <Link href={`/task/${row.id}`} className="font-semibold text-foreground hover:underline">
            {row.title}
          </Link>
          <p className="line-clamp-1 text-xs text-muted-foreground">{row.description || "No description"}</p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (row) => (
        <select
          value={row.status}
          className="h-8 rounded-md border border-border bg-background px-2 text-xs"
          onChange={(event) =>
            updateStatusMutation.mutate({
              taskId: row.id,
              statusValue: event.target.value,
              rowVersion: row.rowVersion,
            })
          }
        >
          {statusColumns.map((statusOption) => (
            <option key={statusOption} value={statusOption}>
              {statusOption}
            </option>
          ))}
        </select>
      ),
    },
    {
      key: "priority",
      header: "Priority",
      render: (row) => <Badge variant={priorityVariant(row.priority)}>{row.priority}</Badge>,
    },
    {
      key: "assignedUserId",
      header: "Assignee",
      render: (row) => (
        <span className="text-xs text-muted-foreground">
          {row.assignedUserId ? usersById.get(row.assignedUserId)?.name ?? "Assigned" : "Unassigned"}
        </span>
      ),
    },
    {
      key: "dueDate",
      header: "Due",
      render: (row) => <span className="text-xs text-muted-foreground">{formatDate(row.dueDate)}</span>,
    },
    {
      key: "actions",
      header: "Actions",
      className: "text-right",
      render: (row) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            const confirmed = window.confirm("Delete this task?");
            if (!confirmed) {
              return;
            }

            deleteTaskMutation.mutate(row.id);
          }}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      ),
    },
  ];

  const grouped = useMemo(() => {
    const map = new Map<string, TaskItem[]>();
    statusColumns.forEach((statusName) => map.set(statusName, []));

    rows.forEach((task) => {
      const bucket = map.get(task.status) ?? [];
      bucket.push(task);
      map.set(task.status, bucket);
    });

    return map;
  }, [rows]);

  const handleCreateTask = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!newProjectId) {
      toast.error("Please select a project");
      return;
    }

    createTaskMutation.mutate({
      title: newTitle.trim(),
      description: newDescription.trim(),
      projectId: newProjectId,
      priority: newPriority,
      dueDate: newDueDate ? new Date(newDueDate).toISOString() : null,
    });
  };

  const handleTaskDragStart = (taskId: string) => (event: DragEvent<HTMLDivElement>) => {
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", taskId);
    setDraggingTaskId(taskId);
  };

  const resetDragState = () => {
    setDraggingTaskId(null);
    setDragOverStatus(null);
  };

  const handleTaskDragEnd = () => {
    resetDragState();
  };

  const handleColumnDragOver = (statusValue: string) => (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";

    if (dragOverStatus !== statusValue) {
      setDragOverStatus(statusValue);
    }
  };

  const handleColumnDragLeave = (statusValue: string) => () => {
    if (dragOverStatus === statusValue) {
      setDragOverStatus(null);
    }
  };

  const handleColumnDrop = (statusValue: string) => (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();

    const droppedTaskId = event.dataTransfer.getData("text/plain") || draggingTaskId;
    if (!droppedTaskId) {
      resetDragState();
      return;
    }

    const droppedTask = rows.find((task) => task.id === droppedTaskId);
    if (!droppedTask || droppedTask.status === statusValue) {
      resetDragState();
      return;
    }

    updateStatusMutation.mutate({
      taskId: droppedTaskId,
      statusValue,
      rowVersion: droppedTask.rowVersion,
    });
    resetDragState();
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle>Tasks</CardTitle>
              <CardDescription>Task table with search, filters, pagination, sorting, and quick actions.</CardDescription>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => void queryClient.invalidateQueries({ queryKey: ["tasks"] })}>
                <RefreshCcw className="h-4 w-4" />
                Refresh
              </Button>
              <Button onClick={() => setIsCreateOpen(true)}>
                <Plus className="h-4 w-4" />
                Create task
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4 pb-6">
          <div className="grid gap-3 lg:grid-cols-6">
            <div className="relative lg:col-span-2">
              <Search className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                className="pl-9"
                placeholder="Search by title"
              />
            </div>

            <select
              value={status}
              className="h-10 rounded-md border border-border bg-background px-3 text-sm"
              onChange={(event) => updateUrl({ status: event.target.value, page: 1 })}
            >
              <option value="">All statuses</option>
              {statusColumns.map((statusOption) => (
                <option key={statusOption} value={statusOption}>
                  {statusOption}
                </option>
              ))}
            </select>

            <select
              value={assignedTo}
              className="h-10 rounded-md border border-border bg-background px-3 text-sm"
              onChange={(event) => updateUrl({ assignedTo: event.target.value, page: 1 })}
              disabled={usersQuery.isError}
            >
              <option value="">All assignees</option>
              {(usersQuery.data ?? []).map((user) => (
                <option key={user.id} value={user.id}>
                  {user.name}
                </option>
              ))}
            </select>

            <select
              value={sortBy}
              className="h-10 rounded-md border border-border bg-background px-3 text-sm"
              onChange={(event) => updateUrl({ sortBy: event.target.value })}
            >
              <option value="createdAt">Sort: Created</option>
              <option value="dueDate">Sort: Due date</option>
              <option value="priority">Sort: Priority</option>
              <option value="title">Sort: Title</option>
              <option value="status">Sort: Status</option>
            </select>

            <Button
              variant="outline"
              onClick={() => updateUrl({ sortDescending: !sortDescending })}
            >
              {sortDescending ? "Descending" : "Ascending"}
            </Button>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="inline-flex rounded-lg border border-border bg-background p-1">
              <Button
                variant={viewMode === "table" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => updateUrl({ view: "table" })}
              >
                <List className="h-4 w-4" />
                Table
              </Button>
              <Button
                variant={viewMode === "kanban" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => updateUrl({ view: "kanban" })}
              >
                <KanbanSquare className="h-4 w-4" />
                Kanban
              </Button>
            </div>

            <div className="text-xs text-muted-foreground">
              Page {tasksQuery.data?.page ?? page} of {tasksQuery.data?.totalPages ?? 1}
            </div>
          </div>

          {tasksQuery.isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 4 }).map((_, index) => (
                <div key={index} className="h-12 animate-pulse rounded-lg bg-muted" />
              ))}
            </div>
          ) : null}
          {tasksQuery.isError ? <p className="text-sm text-destructive">{getErrorMessage(tasksQuery.error)}</p> : null}

          {!tasksQuery.isLoading && !tasksQuery.isError ? (
            viewMode === "table" ? (
              <Table columns={columns} rows={rows} rowKey={(row) => row.id} emptyMessage="No tasks found." />
            ) : (
              <div className="grid gap-4 xl:grid-cols-3">
                {statusColumns.map((statusOption) => (
                  <Card
                    key={statusOption}
                    className={dragOverStatus === statusOption ? "border-primary/60 bg-primary/5" : undefined}
                  >
                    <CardHeader className="pb-3">
                      <div className="flex items-center justify-between">
                        <CardTitle className="text-base">{statusOption}</CardTitle>
                        <Badge variant={statusVariant(statusOption)}>{grouped.get(statusOption)?.length ?? 0}</Badge>
                      </div>
                    </CardHeader>
                    <CardContent
                      className="space-y-3 pb-5"
                      onDragOver={handleColumnDragOver(statusOption)}
                      onDragLeave={handleColumnDragLeave(statusOption)}
                      onDrop={handleColumnDrop(statusOption)}
                    >
                      {(grouped.get(statusOption) ?? []).length === 0 ? (
                        <p className="text-xs text-muted-foreground">No tasks</p>
                      ) : (
                        (grouped.get(statusOption) ?? []).map((task) => (
                          <div
                            key={task.id}
                            className={`rounded-lg border border-border bg-background/50 p-3 ${draggingTaskId === task.id ? "opacity-55" : ""}`}
                            draggable
                            onDragStart={handleTaskDragStart(task.id)}
                            onDragEnd={handleTaskDragEnd}
                          >
                            <Link href={`/task/${task.id}`} className="font-medium hover:underline">
                              {task.title}
                            </Link>
                            <p className="mt-1 text-xs text-muted-foreground">{task.description || "No description"}</p>
                            <div className="mt-2 flex items-center gap-2">
                              <Badge variant={priorityVariant(task.priority)}>{task.priority}</Badge>
                              <span className="text-xs text-muted-foreground">{formatDate(task.dueDate)}</span>
                            </div>
                          </div>
                        ))
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            )
          ) : null}

          <div className="flex items-center justify-end gap-2">
            <Button
              variant="outline"
              disabled={page <= 1}
              onClick={() => updateUrl({ page: page - 1 })}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              disabled={Boolean(tasksQuery.data && !tasksQuery.data.hasNextPage)}
              onClick={() => updateUrl({ page: page + 1 })}
            >
              Next
            </Button>
          </div>
        </CardContent>
      </Card>

      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create task</DialogTitle>
            <DialogDescription>Add a new task with project, priority, and due date.</DialogDescription>
          </DialogHeader>

          <form id="create-task-form" className="space-y-3" onSubmit={handleCreateTask}>
            <div className="space-y-1.5">
              <Label htmlFor="new-task-title">Title</Label>
              <Input
                id="new-task-title"
                value={newTitle}
                onChange={(event) => setNewTitle(event.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="new-task-description">Description</Label>
              <Input
                id="new-task-description"
                value={newDescription}
                onChange={(event) => setNewDescription(event.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="new-task-project">Project</Label>
              <select
                id="new-task-project"
                value={newProjectId}
                className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm"
                onChange={(event) => setNewProjectId(event.target.value)}
                required
              >
                <option value="">Select project</option>
                {(projectsQuery.data ?? []).map((project) => (
                  <option key={project.id} value={project.id}>
                    {project.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="new-task-priority">Priority</Label>
                <select
                  id="new-task-priority"
                  value={newPriority}
                  className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm"
                  onChange={(event) => setNewPriority(event.target.value as TaskPriority)}
                >
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="new-task-due-date">Due date</Label>
                <Input
                  id="new-task-due-date"
                  type="date"
                  value={newDueDate}
                  onChange={(event) => setNewDueDate(event.target.value)}
                />
              </div>
            </div>
          </form>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsCreateOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" form="create-task-form" isLoading={createTaskMutation.isPending}>
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
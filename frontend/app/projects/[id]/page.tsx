"use client";

import Link from "next/link";
import { FormEvent, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { ApiError } from "@/services/api";
import { getProjectById } from "@/services/project";
import { createTask, getTasks } from "@/services/task";
import type { TaskPriority } from "@/types/task";

function getErrorMessage(error: unknown, fallback = "Something went wrong") {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
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

export default function ProjectDetailPage() {
  const params = useParams<{ id: string }>();
  const projectId = String(params.id ?? "");
  const queryClient = useQueryClient();

  const [taskTitle, setTaskTitle] = useState("");
  const [taskDescription, setTaskDescription] = useState("");
  const [taskPriority, setTaskPriority] = useState<TaskPriority>("Medium");
  const [taskDueDate, setTaskDueDate] = useState("");

  const projectQuery = useQuery({
    queryKey: ["project", projectId],
    queryFn: () => getProjectById(projectId),
    enabled: Boolean(projectId),
  });

  const tasksQuery = useQuery({
    queryKey: ["tasks", "project-detail", projectId],
    queryFn: () => getTasks({ page: 1, pageSize: 300, sortBy: "createdAt", sortDescending: true }),
  });

  const createTaskMutation = useMutation({
    mutationFn: createTask,
    onSuccess: async () => {
      toast.success("Task created");
      setTaskTitle("");
      setTaskDescription("");
      setTaskPriority("Medium");
      setTaskDueDate("");
      await queryClient.invalidateQueries({ queryKey: ["tasks"] });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not create task"));
    },
  });

  const projectTasks = useMemo(() => {
    const all = tasksQuery.data?.items ?? [];
    return all.filter((task) => task.projectId === projectId);
  }, [tasksQuery.data?.items, projectId]);

  const handleCreateTask = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    createTaskMutation.mutate({
      title: taskTitle.trim(),
      description: taskDescription.trim(),
      projectId,
      priority: taskPriority,
      dueDate: taskDueDate ? new Date(taskDueDate).toISOString() : null,
    });
  };

  if (projectQuery.isLoading) {
    return (
      <div className="space-y-6">
        <Card className="animate-pulse">
          <CardHeader className="space-y-3">
            <div className="h-8 w-64 rounded bg-muted" />
            <div className="h-4 w-1/2 rounded bg-muted" />
          </CardHeader>
          <CardContent className="grid gap-3 pb-6 lg:grid-cols-2">
            <div className="h-10 rounded bg-muted lg:col-span-2" />
            <div className="h-10 rounded bg-muted lg:col-span-2" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
          </CardContent>
        </Card>

        <Card className="animate-pulse">
          <CardHeader className="space-y-3">
            <div className="h-6 w-40 rounded bg-muted" />
            <div className="h-4 w-2/3 rounded bg-muted" />
          </CardHeader>
          <CardContent className="space-y-3 pb-6">
            <div className="h-16 rounded bg-muted" />
            <div className="h-16 rounded bg-muted" />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (projectQuery.isError || !projectQuery.data) {
    return <p className="text-sm text-destructive">{getErrorMessage(projectQuery.error, "Project not found")}</p>;
  }

  const project = projectQuery.data;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Button asChild variant="ghost" size="sm" className="-ml-2 mb-2">
            <Link href="/projects">
              <ArrowLeft className="h-4 w-4" />
              Back to projects
            </Link>
          </Button>
          <h2 className="text-3xl font-semibold tracking-tight">{project.name}</h2>
          <p className="text-sm text-muted-foreground">{project.description || "No description provided"}</p>
        </div>
        <Badge variant="secondary">{projectTasks.length} tasks</Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create task in this project</CardTitle>
          <CardDescription>Quickly create project-specific tasks.</CardDescription>
        </CardHeader>
        <CardContent className="pb-6">
          <form className="grid gap-3 lg:grid-cols-2" onSubmit={handleCreateTask}>
            <div className="space-y-1.5 lg:col-span-2">
              <Label htmlFor="task-title">Title</Label>
              <Input id="task-title" value={taskTitle} onChange={(event) => setTaskTitle(event.target.value)} required />
            </div>

            <div className="space-y-1.5 lg:col-span-2">
              <Label htmlFor="task-description">Description</Label>
              <Input
                id="task-description"
                value={taskDescription}
                onChange={(event) => setTaskDescription(event.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-priority">Priority</Label>
              <select
                id="task-priority"
                className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm"
                value={taskPriority}
                onChange={(event) => setTaskPriority(event.target.value as TaskPriority)}
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
              </select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="task-due-date">Due date</Label>
              <Input
                id="task-due-date"
                type="date"
                value={taskDueDate}
                onChange={(event) => setTaskDueDate(event.target.value)}
              />
            </div>

            <div className="lg:col-span-2">
              <Button type="submit" isLoading={createTaskMutation.isPending}>Create task</Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Project tasks</CardTitle>
          <CardDescription>All tasks connected to this project.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3 pb-6">
          {tasksQuery.isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 2 }).map((_, index) => (
                <div key={index} className="h-16 animate-pulse rounded-lg bg-muted" />
              ))}
            </div>
          ) : null}
          {tasksQuery.isError ? <p className="text-sm text-destructive">{getErrorMessage(tasksQuery.error)}</p> : null}

          {!tasksQuery.isLoading && !tasksQuery.isError ? (
            projectTasks.length === 0 ? (
              <p className="text-sm text-muted-foreground">No tasks in this project yet.</p>
            ) : (
              projectTasks.map((task) => (
                <div key={task.id} className="rounded-lg border border-border p-3">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <Link href={`/task/${task.id}`} className="font-semibold hover:underline">
                        {task.title}
                      </Link>
                      <p className="text-xs text-muted-foreground">{task.description || "No description"}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={task.priority.toLowerCase() === "high" ? "danger" : task.priority.toLowerCase() === "medium" ? "warning" : "neutral"}>
                        {task.priority}
                      </Badge>
                      <Badge variant={task.status.toLowerCase() === "done" ? "success" : "warning"}>{task.status}</Badge>
                    </div>
                  </div>
                  <p className="mt-2 text-xs text-muted-foreground">Due: {formatDate(task.dueDate)}</p>
                </div>
              ))
            )
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}

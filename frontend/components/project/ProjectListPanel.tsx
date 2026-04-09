"use client";

import Link from "next/link";
import { FormEvent, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { FolderKanban, Plus, Search } from "lucide-react";
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
import { ApiError } from "@/services/api";
import { createProject, getProjects } from "@/services/project";
import { getTasks } from "@/services/task";

function getErrorMessage(error: unknown, fallback = "Something went wrong") {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

export function ProjectListPanel() {
  const queryClient = useQueryClient();
  const [searchText, setSearchText] = useState("");
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [nameDraft, setNameDraft] = useState("");
  const [descriptionDraft, setDescriptionDraft] = useState("");
  const [dueDateDraft, setDueDateDraft] = useState("");

  const projectsQuery = useQuery({
    queryKey: ["projects", "all"],
    queryFn: () => getProjects(),
  });

  const tasksQuery = useQuery({
    queryKey: ["tasks", "project-overview"],
    queryFn: () => getTasks({ page: 1, pageSize: 300, sortBy: "createdAt", sortDescending: true }),
  });

  const createMutation = useMutation({
    mutationFn: createProject,
    onSuccess: async () => {
      toast.success("Project created");
      setIsCreateOpen(false);
      setNameDraft("");
      setDescriptionDraft("");
      setDueDateDraft("");
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not create project"));
    },
  });

  const taskCountsByProject = useMemo(() => {
    const map = new Map<string, number>();

    (tasksQuery.data?.items ?? []).forEach((task) => {
      const current = map.get(task.projectId) ?? 0;
      map.set(task.projectId, current + 1);
    });

    return map;
  }, [tasksQuery.data?.items]);

  const filteredProjects = useMemo(() => {
    const projects = projectsQuery.data ?? [];
    const query = searchText.trim().toLowerCase();

    if (!query) {
      return projects;
    }

    return projects.filter((project) => {
      return project.name.toLowerCase().includes(query) || project.description.toLowerCase().includes(query);
    });
  }, [projectsQuery.data, searchText]);

  const handleCreateProject = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    createMutation.mutate({
      name: nameDraft.trim(),
      description: descriptionDraft.trim(),
      dueDate: dueDateDraft ? new Date(dueDateDraft).toISOString() : null,
    });
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight">Projects</h2>
          <p className="text-sm text-muted-foreground">Create and manage projects connected to your tasks.</p>
        </div>
        <Button size="sm" onClick={() => setIsCreateOpen(true)}>
          <Plus className="h-4 w-4" />
          Create Project
        </Button>
      </div>

      <div className="relative">
        <Search className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={searchText}
          onChange={(event) => setSearchText(event.target.value)}
          className="pl-9"
          placeholder="Search projects"
        />
      </div>

      {projectsQuery.isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }).map((_, index) => (
            <Card key={index} className="animate-pulse">
              <CardHeader className="space-y-3">
                <div className="h-5 w-36 rounded bg-muted" />
                <div className="h-4 w-4/5 rounded bg-muted" />
                <div className="h-4 w-2/5 rounded bg-muted" />
              </CardHeader>
              <CardContent className="pb-6">
                <div className="h-9 rounded bg-muted" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : null}
      {projectsQuery.isError ? <p className="text-sm text-destructive">{getErrorMessage(projectsQuery.error)}</p> : null}

      {!projectsQuery.isLoading && !projectsQuery.isError ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {filteredProjects.length === 0 ? (
            <Card className="md:col-span-2 xl:col-span-3">
              <CardContent className="py-8 text-center text-sm text-muted-foreground">No projects found.</CardContent>
            </Card>
          ) : (
            filteredProjects.map((project) => (
              <Card key={project.id}>
                <CardHeader className="space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <CardTitle className="text-lg">{project.name}</CardTitle>
                      <CardDescription>{project.description || "No description"}</CardDescription>
                    </div>
                    <FolderKanban className="h-5 w-5 text-muted-foreground" />
                  </div>
                  <Badge variant="secondary">{taskCountsByProject.get(project.id) ?? 0} tasks</Badge>
                </CardHeader>
                <CardContent className="flex gap-2 pb-6">
                  <Button asChild variant="outline" size="sm" className="flex-1">
                    <Link href={`/projects/${project.id}`}>Open project</Link>
                  </Button>
                  <Button asChild size="sm" className="flex-1">
                    <Link href="/tasks">View tasks</Link>
                  </Button>
                </CardContent>
              </Card>
            ))
          )}
        </div>
      ) : null}

      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create project</DialogTitle>
            <DialogDescription>Project owner is set automatically to the current user.</DialogDescription>
          </DialogHeader>

          <form id="create-project-form" className="space-y-3" onSubmit={handleCreateProject}>
            <div className="space-y-1.5">
              <Label htmlFor="project-name">Name</Label>
              <Input
                id="project-name"
                value={nameDraft}
                onChange={(event) => setNameDraft(event.target.value)}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="project-description">Description</Label>
              <Input
                id="project-description"
                value={descriptionDraft}
                onChange={(event) => setDescriptionDraft(event.target.value)}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="project-due-date">Due date</Label>
              <Input
                id="project-due-date"
                type="date"
                value={dueDateDraft}
                onChange={(event) => setDueDateDraft(event.target.value)}
              />
            </div>
          </form>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsCreateOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" form="create-project-form" isLoading={createMutation.isPending}>
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
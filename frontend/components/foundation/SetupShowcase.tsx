"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { Badge, type BadgeVariant } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { Modal } from "@/components/ui/Modal";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/Select";
import { Table, type TableColumn } from "@/components/ui/Table";
import { EmptyState } from "@/components/feedback/EmptyState";
import { FormFieldError } from "@/components/forms/FormFieldError";
import { SkeletonLines } from "@/components/feedback/Skeleton";
import {
  validateTaskDraft,
  type TaskDraft,
  type TaskDraftErrors,
} from "@/services/validation/taskDraftValidation";

type DemoTask = {
  id: string;
  title: string;
  owner: string;
  status: "Todo" | "In Progress" | "Done";
  dueDate: string;
};

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

const demoTasks: DemoTask[] = [
  {
    id: "task-1",
    title: "Design dashboard cards",
    owner: "Avery",
    status: "In Progress",
    dueDate: "Apr 11",
  },
  {
    id: "task-2",
    title: "Add comments API integration",
    owner: "Jules",
    status: "Todo",
    dueDate: "Apr 12",
  },
  {
    id: "task-3",
    title: "Finalize responsive QA",
    owner: "Mina",
    status: "Done",
    dueDate: "Apr 8",
  },
];

function toBadgeVariant(status: DemoTask["status"]): BadgeVariant {
  if (status === "Done") {
    return "success";
  }

  if (status === "In Progress") {
    return "warning";
  }

  return "neutral";
}

const columns: Array<TableColumn<DemoTask>> = [
  {
    key: "title",
    header: "Task",
  },
  {
    key: "owner",
    header: "Owner",
  },
  {
    key: "status",
    header: "Status",
    render: (row) => <Badge variant={toBadgeVariant(row.status)}>{row.status}</Badge>,
  },
  {
    key: "dueDate",
    header: "Due",
    className: "font-mono",
  },
];

export function SetupShowcase() {
  const [draft, setDraft] = useState<TaskDraft>({
    title: "",
    priority: "Medium",
    assignee: "",
  });
  const [errors, setErrors] = useState<TaskDraftErrors>({});
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [lastValidatedTask, setLastValidatedTask] = useState<string | null>(null);

  const handleDraftChange = (key: keyof TaskDraft, value: string) => {
    setDraft((currentDraft) => ({
      ...currentDraft,
      [key]: value,
    }));
  };

  const handleValidate = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationResult = validateTaskDraft(draft);
    setErrors(validationResult);

    if (Object.keys(validationResult).length > 0) {
      return;
    }

    setLastValidatedTask(draft.title.trim());
    setIsModalOpen(true);
  };

  return (
    <div className="space-y-7">
      <section className="surface-card grid-dot p-6 md:p-8">
        <p className="text-xs uppercase tracking-[0.24em] text-[var(--color-muted)]">Phase 1 Completed</p>
        <h2 className="mt-3 text-3xl font-semibold tracking-tight md:text-4xl">
          Setup and UI foundation are now ready.
        </h2>
        <p className="mt-3 max-w-3xl text-sm leading-7 text-[var(--color-muted)] md:text-base">
          The project now has a clean shell, reusable UI building blocks, and typed frontend utilities that
          match your backend-first workflow.
        </p>
        <div className="mt-5 flex flex-wrap items-center gap-2">
          <Badge variant="success">Reusable components</Badge>
          <Badge variant="success">Shared API client</Badge>
          <Badge variant="success">Validation pattern</Badge>
          <Button className="ml-auto" onClick={() => setIsModalOpen(true)}>
            Open Foundation Modal
          </Button>
        </div>
      </section>

      <section id="api-status" className="grid gap-6 lg:grid-cols-2">
        <article className="surface-card p-6">
          <p className="text-xs uppercase tracking-[0.24em] text-[var(--color-muted)]">Environment</p>
          <h3 className="mt-3 text-xl font-semibold">API client setup</h3>
          <p className="mt-2 text-sm text-[var(--color-muted)]">
            Base URL is pulled from <code className="font-mono">NEXT_PUBLIC_API_BASE_URL</code>.
          </p>
          <div className="mt-4 rounded-lg border border-[var(--color-border)] bg-white px-3 py-2 font-mono text-xs md:text-sm">
            {apiBaseUrl}
          </div>
          <ul className="mt-4 space-y-2 text-sm text-[var(--color-muted)]">
            <li>- Query param helper and typed request wrappers</li>
            <li>- JWT auto-attachment for authenticated calls</li>
            <li>- Centralized API error handling</li>
          </ul>
        </article>

        <article id="ui-kit" className="surface-card p-6">
          <p className="text-xs uppercase tracking-[0.24em] text-[var(--color-muted)]">Feedback Patterns</p>
          <h3 className="mt-3 text-xl font-semibold">Skeleton and empty states</h3>
          <p className="mt-2 text-sm text-[var(--color-muted)]">
            These are the baseline patterns for loading and no-data screens across dashboard, projects, and
            tasks.
          </p>
          <div className="mt-4">
            <SkeletonLines lines={3} />
          </div>
        </article>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.1fr_1fr]">
        <article className="surface-card p-6">
          <p className="text-xs uppercase tracking-[0.24em] text-[var(--color-muted)]">Forms</p>
          <h3 className="mt-3 text-xl font-semibold">Validation + form controls</h3>
          <form className="mt-4 space-y-4" onSubmit={handleValidate}>
            <div>
              <Label className="mb-1 block" htmlFor="taskTitle">
                Task title
              </Label>
              <Input
                id="taskTitle"
                placeholder="Enter task title"
                value={draft.title}
                onChange={(event) => handleDraftChange("title", event.target.value)}
              />
              <FormFieldError message={errors.title} />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <Label className="mb-1 block" htmlFor="taskPriority">
                  Priority
                </Label>
                <Select value={draft.priority} onValueChange={(value) => handleDraftChange("priority", value)}>
                  <SelectTrigger id="taskPriority">
                    <SelectValue placeholder="Select priority" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                  </SelectContent>
                </Select>
                <FormFieldError message={errors.priority} />
              </div>

              <div>
                <Label className="mb-1 block" htmlFor="taskAssignee">
                  Assignee
                </Label>
                <Input
                  id="taskAssignee"
                  placeholder="Name"
                  value={draft.assignee}
                  onChange={(event) => handleDraftChange("assignee", event.target.value)}
                />
                <FormFieldError message={errors.assignee} />
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-3 pt-1">
              <Button type="submit">Validate Draft</Button>
              <Button
                variant="outline"
                onClick={() => {
                  setDraft({ title: "", priority: "Medium", assignee: "" });
                  setErrors({});
                  setLastValidatedTask(null);
                }}
              >
                Reset
              </Button>
            </div>

            {lastValidatedTask ? (
              <p className="text-sm font-medium text-[var(--color-success)]">
                Draft validated successfully: {lastValidatedTask}
              </p>
            ) : null}
          </form>
        </article>

        <article className="surface-card p-6">
          <p className="text-xs uppercase tracking-[0.24em] text-[var(--color-muted)]">Data Display</p>
          <h3 className="mt-3 text-xl font-semibold">Reusable table + badges</h3>
          <div className="mt-4">
            <Table columns={columns} rows={demoTasks} rowKey={(row) => row.id} />
          </div>
        </article>
      </section>

      <section>
        <EmptyState
          title="No projects have been created yet"
          description="Use this component in project/task screens for graceful first-run UX."
          action={<Button variant="outline">Create your first project</Button>}
        />
      </section>

      <Modal
        open={isModalOpen}
        title="Foundation modal"
        description="This confirms reusable modal behavior is available for dialogs such as delete confirmations and quick-create forms."
        onClose={() => setIsModalOpen(false)}
      >
        <div className="space-y-3 text-sm text-[var(--color-muted)]">
          <p>The frontend foundation now includes:</p>
          <ul className="list-disc space-y-1 pl-5">
            <li>Global app shell and navigation</li>
            <li>Reusable input, select, button, badge, table, and modal primitives</li>
            <li>Shared API client and token utilities</li>
            <li>Validation helper and form error pattern</li>
          </ul>
        </div>
      </Modal>
    </div>
  );
}
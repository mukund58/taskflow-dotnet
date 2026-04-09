"use client";

import { FormEvent, KeyboardEvent, useEffect, useMemo, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Sparkles } from "lucide-react";
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
import {
  addTaskChecklistItem,
  addTaskComment,
  assignTask,
  deleteTaskAttachment,
  downloadTaskAttachment,
  getTaskAttachments,
  getTaskActivity,
  getTaskById,
  getTaskChecklist,
  getTaskChecklistSummary,
  getTaskComments,
  getTasks,
  toggleTaskChecklistItem,
  updateTask,
  updateTaskStatus,
  uploadTaskAttachment,
} from "@/services/task";
import { useAuthStore } from "@/store/authStore";
import { getUsers } from "@/services/user";
import { getProjectMembers } from "@/services/project";
import type { TaskAttachment, TaskPriority, UpdateTaskInput } from "@/types/task";

type Suggestion = {
  userId: string;
  userName: string;
  priority: TaskPriority;
  explanation: string;
};

type MentionSuggestion = {
  userId: string;
  name: string;
  email: string;
  emailLocalPart: string;
  handle: string;
};

type MentionContext = {
  start: number;
  end: number;
  query: string;
};

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
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function formatFileSize(bytes: number) {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return "-";
  }

  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

function computeSuggestedPriority(dueDate?: string | null): TaskPriority {
  if (!dueDate) {
    return "Medium";
  }

  const dueTime = new Date(dueDate).getTime();
  if (Number.isNaN(dueTime)) {
    return "Medium";
  }

  const daysRemaining = (dueTime - Date.now()) / (24 * 60 * 60 * 1000);

  if (daysRemaining <= 2) {
    return "High";
  }

  if (daysRemaining <= 6) {
    return "Medium";
  }

  return "Low";
}

function extractMentionContext(comment: string, caretPosition: number | null): MentionContext | null {
  if (caretPosition === null || caretPosition < 0) {
    return null;
  }

  const beforeCaret = comment.slice(0, caretPosition);
  const match = beforeCaret.match(/(?:^|\s)@([A-Za-z0-9._-]{0,64})$/);
  if (!match) {
    return null;
  }

  const atIndex = beforeCaret.lastIndexOf("@");
  if (atIndex < 0) {
    return null;
  }

  return {
    start: atIndex,
    end: caretPosition,
    query: match[1] ?? "",
  };
}

function normalizeHandle(value: string) {
  return value.trim().toLowerCase().replace(/[^a-z0-9]/g, "");
}

function buildPreferredHandle(name: string, emailLocalPart: string) {
  const fromName = normalizeHandle(name);
  if (fromName.length >= 2) {
    return fromName;
  }

  return normalizeHandle(emailLocalPart);
}

export default function TaskDetailsPage() {
  const params = useParams<{ id: string }>();
  const taskId = String(params.id ?? "");
  const queryClient = useQueryClient();
  const currentUserId = useAuthStore((state) => state.user?.id ?? null);

  const [titleDraft, setTitleDraft] = useState("");
  const [descriptionDraft, setDescriptionDraft] = useState("");
  const [dueDateDraft, setDueDateDraft] = useState("");
  const [newChecklistTitle, setNewChecklistTitle] = useState("");
  const [newComment, setNewComment] = useState("");
  const [commentCursorPosition, setCommentCursorPosition] = useState<number | null>(null);
  const [isMentionMenuOpen, setIsMentionMenuOpen] = useState(false);
  const [activeMentionIndex, setActiveMentionIndex] = useState(0);
  const [attachmentFile, setAttachmentFile] = useState<File | null>(null);
  const [suggestion, setSuggestion] = useState<Suggestion | null>(null);
  const [isSuggestionOpen, setIsSuggestionOpen] = useState(false);

  const commentInputRef = useRef<HTMLInputElement>(null);

  const taskQuery = useQuery({
    queryKey: ["task", taskId],
    queryFn: () => getTaskById(taskId),
    enabled: Boolean(taskId),
  });

  const commentsQuery = useQuery({
    queryKey: ["task-comments", taskId],
    queryFn: () => getTaskComments(taskId),
    enabled: Boolean(taskId),
  });

  const checklistQuery = useQuery({
    queryKey: ["task-checklist", taskId],
    queryFn: () => getTaskChecklist(taskId),
    enabled: Boolean(taskId),
  });

  const checklistSummaryQuery = useQuery({
    queryKey: ["task-checklist-summary", taskId],
    queryFn: () => getTaskChecklistSummary(taskId),
    enabled: Boolean(taskId),
  });

  const activityQuery = useQuery({
    queryKey: ["task-activity", taskId],
    queryFn: () => getTaskActivity(taskId),
    enabled: Boolean(taskId),
  });

  const attachmentsQuery = useQuery({
    queryKey: ["task-attachments", taskId],
    queryFn: () => getTaskAttachments(taskId),
    enabled: Boolean(taskId),
  });

  const usersQuery = useQuery({
    queryKey: ["users", "task-detail"],
    queryFn: () => getUsers(),
    retry: false,
  });

  const projectMembersQuery = useQuery({
    queryKey: ["project-members", taskQuery.data?.projectId],
    queryFn: () => getProjectMembers(taskQuery.data!.projectId),
    enabled: Boolean(taskQuery.data?.projectId),
    retry: false,
  });

  const tasksForSuggestionQuery = useQuery({
    queryKey: ["tasks", "suggestion"],
    queryFn: () => getTasks({ page: 1, pageSize: 100, sortBy: "createdAt", sortDescending: true }),
  });

  useEffect(() => {
    if (!taskQuery.data) {
      return;
    }

    const timeout = window.setTimeout(() => {
      setTitleDraft(taskQuery.data.title);
      setDescriptionDraft(taskQuery.data.description);
      setDueDateDraft(taskQuery.data.dueDate ? taskQuery.data.dueDate.slice(0, 10) : "");
    }, 0);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [taskQuery.data]);

  const refreshTaskSections = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["task", taskId] }),
      queryClient.invalidateQueries({ queryKey: ["task-comments", taskId] }),
      queryClient.invalidateQueries({ queryKey: ["task-checklist", taskId] }),
      queryClient.invalidateQueries({ queryKey: ["task-checklist-summary", taskId] }),
      queryClient.invalidateQueries({ queryKey: ["task-activity", taskId] }),
      queryClient.invalidateQueries({ queryKey: ["task-attachments", taskId] }),
      queryClient.invalidateQueries({ queryKey: ["tasks"] }),
    ]);
  };

  const updateTaskMutation = useMutation({
    mutationFn: ({ payload }: { payload: UpdateTaskInput }) => updateTask(taskId, payload),
    onSuccess: async () => {
      toast.success("Task updated");
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not update task"));
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ status, rowVersion }: { status: string; rowVersion?: number }) =>
      updateTaskStatus(taskId, status, rowVersion),
    onSuccess: async () => {
      toast.success("Status updated");
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not update status"));
    },
  });

  const assignMutation = useMutation({
    mutationFn: ({ userId, rowVersion }: { userId: string; rowVersion?: number }) =>
      assignTask(taskId, userId, rowVersion),
    onSuccess: async () => {
      toast.success("Assignee updated");
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not assign task"));
    },
  });

  const addChecklistMutation = useMutation({
    mutationFn: ({ title }: { title: string }) => addTaskChecklistItem(taskId, { title, order: 0 }),
    onSuccess: async () => {
      toast.success("Checklist item added");
      setNewChecklistTitle("");
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not add checklist item"));
    },
  });

  const toggleChecklistMutation = useMutation({
    mutationFn: ({ checklistItemId }: { checklistItemId: string }) =>
      toggleTaskChecklistItem(taskId, checklistItemId),
    onSuccess: async () => {
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not update checklist"));
    },
  });

  const addCommentMutation = useMutation({
    mutationFn: ({ content }: { content: string }) => addTaskComment(taskId, { content }),
    onSuccess: async () => {
      toast.success("Comment added");
      setNewComment("");
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not add comment"));
    },
  });

  const uploadAttachmentMutation = useMutation({
    mutationFn: ({ file }: { file: File }) => uploadTaskAttachment(taskId, file),
    onSuccess: async () => {
      toast.success("Attachment uploaded");
      setAttachmentFile(null);
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not upload attachment"));
    },
  });

  const deleteAttachmentMutation = useMutation({
    mutationFn: ({ attachmentId }: { attachmentId: string }) => deleteTaskAttachment(taskId, attachmentId),
    onSuccess: async () => {
      toast.success("Attachment deleted");
      await refreshTaskSections();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, "Could not delete attachment"));
    },
  });

  const assigneeName = useMemo(() => {
    if (!taskQuery.data?.assignedUserId) {
      return "Unassigned";
    }

    const users = usersQuery.data ?? [];
    return users.find((user) => user.id === taskQuery.data?.assignedUserId)?.name ?? "Assigned";
  }, [taskQuery.data?.assignedUserId, usersQuery.data]);

  const mentionCandidates = useMemo<MentionSuggestion[]>(() => {
    const projectMembers = projectMembersQuery.data;

    if (projectMembers && projectMembers.length > 0) {
      return projectMembers
        .map((member) => {
          const emailLocalPart = member.email.split("@")[0] ?? "";
          return {
            userId: member.userId,
            name: member.name,
            email: member.email,
            emailLocalPart,
            handle: buildPreferredHandle(member.name, emailLocalPart),
          };
        })
        .filter((member) => member.handle.length >= 2)
        .sort((left, right) => left.name.localeCompare(right.name));
    }

    return (usersQuery.data ?? [])
      .map((user) => {
        const emailLocalPart = user.email.split("@")[0] ?? "";
        return {
          userId: user.id,
          name: user.name,
          email: user.email,
          emailLocalPart,
          handle: buildPreferredHandle(user.name, emailLocalPart),
        };
      })
      .filter((member) => member.handle.length >= 2)
      .sort((left, right) => left.name.localeCompare(right.name));
  }, [projectMembersQuery.data, usersQuery.data]);

  const mentionContext = useMemo(
    () => extractMentionContext(newComment, commentCursorPosition),
    [newComment, commentCursorPosition],
  );

  const mentionSuggestions = useMemo(() => {
    if (!mentionContext) {
      return [];
    }

    const normalizedQuery = mentionContext.query.trim().toLowerCase();

    return mentionCandidates
      .filter((candidate) => {
        if (!normalizedQuery) {
          return true;
        }

        return (
          candidate.handle.includes(normalizedQuery) ||
          candidate.emailLocalPart.toLowerCase().includes(normalizedQuery) ||
          candidate.name.toLowerCase().includes(normalizedQuery)
        );
      })
      .slice(0, 6);
  }, [mentionCandidates, mentionContext]);

  useEffect(() => {
    if (!mentionContext || mentionSuggestions.length === 0) {
      setIsMentionMenuOpen(false);
      setActiveMentionIndex(0);
      return;
    }

    setIsMentionMenuOpen(true);
    setActiveMentionIndex((current) => Math.min(current, mentionSuggestions.length - 1));
  }, [mentionContext, mentionSuggestions.length]);

  const commitMentionSelection = (candidate: MentionSuggestion) => {
    if (!mentionContext) {
      return;
    }

    const before = newComment.slice(0, mentionContext.start);
    const after = newComment.slice(mentionContext.end);
    const mentionText = `@${candidate.handle}`;
    const trailingSpace = after.length === 0 || after.startsWith(" ") ? "" : " ";
    const nextComment = `${before}${mentionText}${trailingSpace}${after}`;
    const nextCursor = (before + mentionText + trailingSpace).length;

    setNewComment(nextComment);
    setCommentCursorPosition(nextCursor);
    setIsMentionMenuOpen(false);
    setActiveMentionIndex(0);

    requestAnimationFrame(() => {
      if (!commentInputRef.current) {
        return;
      }

      commentInputRef.current.focus();
      commentInputRef.current.setSelectionRange(nextCursor, nextCursor);
    });
  };

  const handleCommentInputKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (!isMentionMenuOpen || mentionSuggestions.length === 0) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveMentionIndex((index) => (index + 1) % mentionSuggestions.length);
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveMentionIndex((index) => (index - 1 + mentionSuggestions.length) % mentionSuggestions.length);
      return;
    }

    if (event.key === "Enter" || event.key === "Tab") {
      event.preventDefault();
      commitMentionSelection(mentionSuggestions[activeMentionIndex] ?? mentionSuggestions[0]);
      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();
      setIsMentionMenuOpen(false);
    }
  };

  const syncCommentCursorPosition = () => {
    setCommentCursorPosition(commentInputRef.current?.selectionStart ?? null);
  };

  const handleDownloadAttachment = async (attachment: TaskAttachment) => {
    try {
      await downloadTaskAttachment(taskId, attachment.id, attachment.fileName);
    } catch (error) {
      toast.error(getErrorMessage(error, "Could not download attachment"));
    }
  };

  const handleSaveTaskInfo = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!taskQuery.data) {
      return;
    }

    updateTaskMutation.mutate({
      payload: {
        title: titleDraft.trim(),
        description: descriptionDraft.trim(),
        status: taskQuery.data.status,
        priority: taskQuery.data.priority,
        assignedUserId: taskQuery.data.assignedUserId ?? null,
        dueDate: dueDateDraft ? new Date(dueDateDraft).toISOString() : null,
        rowVersion: taskQuery.data.rowVersion,
      },
    });
  };

  const handleSuggestAssignment = () => {
    const users = usersQuery.data ?? [];
    const tasks = tasksForSuggestionQuery.data?.items ?? [];

    if (users.length === 0) {
      toast.error("User list unavailable for suggestion");
      return;
    }

    const activeCountByUser = new Map<string, number>();
    users.forEach((user) => activeCountByUser.set(user.id, 0));

    tasks.forEach((task) => {
      if (!task.assignedUserId) {
        return;
      }

      if (task.status.toLowerCase() === "done") {
        return;
      }

      const current = activeCountByUser.get(task.assignedUserId) ?? 0;
      activeCountByUser.set(task.assignedUserId, current + 1);
    });

    const sorted = [...users]
      .map((user) => ({
        user,
        activeCount: activeCountByUser.get(user.id) ?? 0,
      }))
      .sort((left, right) => left.activeCount - right.activeCount);

    const best = sorted[0];
    if (!best || !taskQuery.data) {
      toast.error("Suggestion unavailable");
      return;
    }

    const suggestedPriority = computeSuggestedPriority(taskQuery.data.dueDate);
    setSuggestion({
      userId: best.user.id,
      userName: best.user.name,
      priority: suggestedPriority,
      explanation: `${best.user.name} currently has the lightest active workload (${best.activeCount} active tasks).`,
    });
    setIsSuggestionOpen(true);
  };

  const handleApplySuggestion = () => {
    if (!taskQuery.data || !suggestion) {
      return;
    }

    updateTaskMutation.mutate({
      payload: {
        title: titleDraft.trim(),
        description: descriptionDraft.trim(),
        status: taskQuery.data.status,
        priority: suggestion.priority,
        assignedUserId: suggestion.userId,
        dueDate: dueDateDraft ? new Date(dueDateDraft).toISOString() : null,
        rowVersion: taskQuery.data.rowVersion,
      },
    });

    setIsSuggestionOpen(false);
  };

  if (taskQuery.isLoading) {
    return (
      <div className="space-y-6">
        <Card className="animate-pulse">
          <CardHeader className="space-y-3">
            <div className="h-7 w-56 rounded bg-muted" />
            <div className="h-4 w-3/4 rounded bg-muted" />
          </CardHeader>
          <CardContent className="grid gap-4 pb-6 lg:grid-cols-2">
            <div className="h-10 rounded bg-muted lg:col-span-2" />
            <div className="h-10 rounded bg-muted lg:col-span-2" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
          </CardContent>
        </Card>

        <div className="grid gap-6 xl:grid-cols-2">
          <Card className="animate-pulse">
            <CardHeader className="space-y-3">
              <div className="h-6 w-32 rounded bg-muted" />
              <div className="h-4 w-2/3 rounded bg-muted" />
            </CardHeader>
            <CardContent className="space-y-3 pb-6">
              <div className="h-10 rounded bg-muted" />
              <div className="h-14 rounded bg-muted" />
              <div className="h-14 rounded bg-muted" />
            </CardContent>
          </Card>

          <Card className="animate-pulse">
            <CardHeader className="space-y-3">
              <div className="h-6 w-32 rounded bg-muted" />
              <div className="h-4 w-2/3 rounded bg-muted" />
            </CardHeader>
            <CardContent className="space-y-3 pb-6">
              <div className="h-10 rounded bg-muted" />
              <div className="h-16 rounded bg-muted" />
              <div className="h-16 rounded bg-muted" />
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  if (taskQuery.isError || !taskQuery.data) {
    return <p className="text-sm text-destructive">{getErrorMessage(taskQuery.error, "Task not found")}</p>;
  }

  const task = taskQuery.data;
  const checklistItems = checklistQuery.data ?? [];
  const checklistSummary = checklistSummaryQuery.data;
  const comments = commentsQuery.data ?? [];
  const attachments = attachmentsQuery.data ?? [];
  const activities = activityQuery.data ?? [];

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle className="text-2xl">{task.title}</CardTitle>
              <CardDescription>Task detail with edit controls, checklist, comments, and activity.</CardDescription>
            </div>
            <Badge variant={task.status.toLowerCase() === "done" ? "success" : "warning"}>{task.status}</Badge>
          </div>
        </CardHeader>
        <CardContent className="pb-6">
          <form className="grid gap-4 lg:grid-cols-2" onSubmit={handleSaveTaskInfo}>
            <div className="space-y-1.5 lg:col-span-2">
              <Label htmlFor="task-title">Title</Label>
              <Input
                id="task-title"
                value={titleDraft}
                onChange={(event) => setTitleDraft(event.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5 lg:col-span-2">
              <Label htmlFor="task-description">Description</Label>
              <Input
                id="task-description"
                value={descriptionDraft}
                onChange={(event) => setDescriptionDraft(event.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label>Status</Label>
              <select
                className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm"
                value={task.status}
                onChange={(event) =>
                  updateStatusMutation.mutate({
                    status: event.target.value,
                    rowVersion: task.rowVersion,
                  })
                }
              >
                <option value="Todo">Todo</option>
                <option value="In Progress">In Progress</option>
                <option value="Done">Done</option>
              </select>
            </div>

            <div className="space-y-1.5">
              <Label>Priority</Label>
              <select
                className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm"
                value={task.priority}
                onChange={(event) => {
                  updateTaskMutation.mutate({
                    payload: {
                      title: titleDraft.trim(),
                      description: descriptionDraft.trim(),
                      status: task.status,
                      priority: event.target.value,
                      assignedUserId: task.assignedUserId ?? null,
                      dueDate: dueDateDraft ? new Date(dueDateDraft).toISOString() : null,
                      rowVersion: task.rowVersion,
                    },
                  });
                }}
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
                value={dueDateDraft}
                onChange={(event) => setDueDateDraft(event.target.value)}
              />
            </div>

            <div className="space-y-1.5">
              <Label>Assigned user</Label>
              <select
                className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm"
                value={task.assignedUserId ?? ""}
                onChange={(event) => {
                  const nextUserId = event.target.value;
                  if (!nextUserId) {
                    return;
                  }
                  assignMutation.mutate({ userId: nextUserId, rowVersion: task.rowVersion });
                }}
                disabled={usersQuery.isError}
              >
                <option value="">{assigneeName}</option>
                {(usersQuery.data ?? []).map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="lg:col-span-2 flex flex-wrap gap-2">
              <Button type="submit" isLoading={updateTaskMutation.isPending}>
                Save changes
              </Button>
              <Button
                type="button"
                variant="outline"
                onClick={handleSuggestAssignment}
                isLoading={tasksForSuggestionQuery.isFetching || usersQuery.isFetching}
              >
                <Sparkles className="h-4 w-4" />
                Suggest assignment
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Dialog open={isSuggestionOpen} onOpenChange={setIsSuggestionOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>AI Assignment Suggestion</DialogTitle>
            <DialogDescription>
              Suggested assignee and priority based on current workload and due date urgency.
            </DialogDescription>
          </DialogHeader>

          {suggestion ? (
            <div className="space-y-3 rounded-lg border border-border bg-background/60 p-4 text-sm">
              <p>
                <span className="font-semibold">Suggested user:</span> {suggestion.userName}
              </p>
              <p>
                <span className="font-semibold">Suggested priority:</span> {suggestion.priority}
              </p>
              <p className="text-muted-foreground">{suggestion.explanation}</p>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Generate a suggestion first.</p>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsSuggestionOpen(false)}>
              Close
            </Button>
            <Button onClick={handleApplySuggestion} disabled={!suggestion} isLoading={updateTaskMutation.isPending}>
              Apply suggestion
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Checklist</CardTitle>
            <CardDescription>
              {checklistSummary
                ? `${checklistSummary.completedItems}/${checklistSummary.totalItems} complete (${Math.round(checklistSummary.percentageComplete)}%)`
                : "Track progress with checklist items"}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 pb-6">
            {checklistSummary ? (
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary transition-all"
                  style={{ width: `${Math.max(0, Math.min(100, checklistSummary.percentageComplete))}%` }}
                />
              </div>
            ) : null}

            <form
              className="flex gap-2"
              onSubmit={(event) => {
                event.preventDefault();
                if (!newChecklistTitle.trim()) {
                  return;
                }

                addChecklistMutation.mutate({ title: newChecklistTitle.trim() });
              }}
            >
              <Input
                value={newChecklistTitle}
                onChange={(event) => setNewChecklistTitle(event.target.value)}
                placeholder="Add checklist item"
              />
              <Button type="submit" isLoading={addChecklistMutation.isPending}>Add</Button>
            </form>

            {checklistItems.length === 0 ? (
              <p className="text-sm text-muted-foreground">No checklist items yet.</p>
            ) : (
              checklistItems.map((item) => (
                <label key={item.id} className="flex items-center justify-between gap-3 rounded-lg border border-border p-3">
                  <span className="text-sm">{item.title}</span>
                  <input
                    type="checkbox"
                    checked={item.isCompleted}
                    onChange={() => toggleChecklistMutation.mutate({ checklistItemId: item.id })}
                  />
                </label>
              ))
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Comments</CardTitle>
            <CardDescription>Discuss task updates with your team. Use @name to mention project members.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 pb-6">
            <form
              className="flex gap-2"
              onSubmit={(event) => {
                event.preventDefault();
                if (isMentionMenuOpen && mentionSuggestions.length > 0) {
                  commitMentionSelection(mentionSuggestions[activeMentionIndex] ?? mentionSuggestions[0]);
                  return;
                }

                if (!newComment.trim()) {
                  return;
                }
                addCommentMutation.mutate({ content: newComment.trim() });
              }}
            >
              <div className="relative flex-1">
                <Input
                  ref={commentInputRef}
                  value={newComment}
                  onChange={(event) => {
                    setNewComment(event.target.value);
                    setCommentCursorPosition(event.target.selectionStart);
                  }}
                  onKeyDown={handleCommentInputKeyDown}
                  onKeyUp={syncCommentCursorPosition}
                  onClick={syncCommentCursorPosition}
                  onFocus={syncCommentCursorPosition}
                  onBlur={() => {
                    window.setTimeout(() => {
                      setIsMentionMenuOpen(false);
                    }, 120);
                  }}
                  placeholder="Add a comment (e.g., @alex please review)"
                />

                {isMentionMenuOpen && mentionSuggestions.length > 0 ? (
                  <div className="absolute z-20 mt-1 max-h-56 w-full overflow-auto rounded-md border border-border bg-popover p-1 shadow-md">
                    {mentionSuggestions.map((candidate, index) => (
                      <button
                        key={candidate.userId}
                        type="button"
                        className={`flex w-full items-center justify-between rounded px-2 py-1.5 text-left text-sm ${
                          index === activeMentionIndex ? "bg-accent text-accent-foreground" : "hover:bg-accent/70"
                        }`}
                        onMouseDown={(mouseEvent) => {
                          mouseEvent.preventDefault();
                          commitMentionSelection(candidate);
                        }}
                      >
                        <span className="font-medium">{candidate.name}</span>
                        <span className="text-xs text-muted-foreground">@{candidate.handle}</span>
                      </button>
                    ))}
                  </div>
                ) : null}
              </div>
              <Button type="submit" isLoading={addCommentMutation.isPending}>Post</Button>
            </form>

            {comments.length === 0 ? (
              <p className="text-sm text-muted-foreground">No comments yet.</p>
            ) : (
              comments.map((comment) => (
                <div key={comment.id} className="rounded-lg border border-border p-3">
                  <div className="flex items-center justify-between gap-2">
                    <p className="text-sm font-semibold">{comment.authorName}</p>
                    <span className="text-xs text-muted-foreground">{formatDate(comment.createdAt)}</span>
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">{comment.content}</p>
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Attachments</CardTitle>
          <CardDescription>Upload files and download task assets.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3 pb-6">
          <form
            className="flex flex-wrap items-end gap-2"
            onSubmit={(event) => {
              event.preventDefault();

              if (!attachmentFile) {
                toast.error("Please choose a file first");
                return;
              }

              uploadAttachmentMutation.mutate({ file: attachmentFile });
            }}
          >
            <div className="min-w-[220px] flex-1 space-y-1.5">
              <Label htmlFor="task-attachment-file">Choose file</Label>
              <Input
                id="task-attachment-file"
                type="file"
                onChange={(event) => setAttachmentFile(event.target.files?.[0] ?? null)}
              />
            </div>
            <Button type="submit" isLoading={uploadAttachmentMutation.isPending}>
              Upload file
            </Button>
          </form>

          {attachmentsQuery.isError ? (
            <p className="text-sm text-destructive">{getErrorMessage(attachmentsQuery.error)}</p>
          ) : null}

          {attachmentsQuery.isLoading ? <p className="text-sm text-muted-foreground">Loading attachments...</p> : null}

          {!attachmentsQuery.isLoading && attachments.length === 0 ? (
            <p className="text-sm text-muted-foreground">No attachments yet.</p>
          ) : null}

          {!attachmentsQuery.isLoading && attachments.length > 0 ? (
            attachments.map((attachment) => (
              <div key={attachment.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border p-3">
                <div className="space-y-1">
                  <p className="text-sm font-semibold">{attachment.fileName}</p>
                  <p className="text-xs text-muted-foreground">
                    {formatFileSize(attachment.fileSizeBytes)} · uploaded by {attachment.uploadedByUserName} · {formatDate(attachment.uploadedAt)}
                  </p>
                </div>

                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" size="sm" onClick={() => void handleDownloadAttachment(attachment)}>
                    Download
                  </Button>

                  {currentUserId === attachment.uploadedByUserId ? (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      isLoading={deleteAttachmentMutation.isPending}
                      onClick={() => deleteAttachmentMutation.mutate({ attachmentId: attachment.id })}
                    >
                      Delete
                    </Button>
                  ) : null}
                </div>
              </div>
            ))
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Activity log</CardTitle>
          <CardDescription>Recent task events from backend activity tracking.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 pb-6">
          {activities.length === 0 ? (
            <p className="text-sm text-muted-foreground">No activity yet.</p>
          ) : (
            activities.map((activity) => (
              <div key={activity.id} className="rounded-lg border border-border p-3 text-sm">
                <p className="font-medium">{activity.action}</p>
                <p className="text-xs text-muted-foreground">
                  {activity.oldValue ? `${activity.oldValue} -> ` : ""}
                  {activity.newValue ?? "-"}
                </p>
                <p className="mt-1 text-xs text-muted-foreground">{formatDate(activity.createdAt)}</p>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
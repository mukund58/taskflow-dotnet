export type TaskDraft = {
  title: string;
  priority: string;
  assignee: string;
};

export type TaskDraftErrors = Partial<Record<keyof TaskDraft, string>>;

export function validateTaskDraft(draft: TaskDraft) {
  const errors: TaskDraftErrors = {};

  if (!draft.title.trim()) {
    errors.title = "Task title is required.";
  } else if (draft.title.trim().length < 3) {
    errors.title = "Task title must be at least 3 characters.";
  }

  if (!draft.priority) {
    errors.priority = "Priority is required.";
  }

  if (!draft.assignee.trim()) {
    errors.assignee = "Assignee is required.";
  }

  return errors;
}
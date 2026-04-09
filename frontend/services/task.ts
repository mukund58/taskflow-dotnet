import { apiClient } from "@/services/api/client";
import type { ApiResponse, PaginatedResponse } from "@/types/api";
import type {
  CreateTaskChecklistInput,
  CreateTaskCommentInput,
  CreateTaskInput,
  TaskActivity,
  TaskAttachment,
  TaskChecklistItem,
  TaskChecklistSummary,
  TaskComment,
  TaskItem,
  TaskQuery,
  UpdateTaskInput,
} from "@/types/task";

export async function getTasks(query: TaskQuery = {}) {
  const response = await apiClient.get<ApiResponse<PaginatedResponse<TaskItem>>>("/api/tasks", {
    auth: true,
    query,
  });

  return response.data;
}

export async function getTaskById(taskId: string) {
  const response = await apiClient.get<ApiResponse<TaskItem>>(`/api/tasks/${taskId}`, {
    auth: true,
  });

  return response.data;
}

export async function createTask(payload: CreateTaskInput) {
  const response = await apiClient.post<ApiResponse<TaskItem>>("/api/tasks", payload, {
    auth: true,
  });

  return response.data;
}

export async function updateTask(taskId: string, payload: UpdateTaskInput) {
  const response = await apiClient.put<ApiResponse<TaskItem>>(`/api/tasks/${taskId}`, payload, {
    auth: true,
  });

  return response.data;
}

export async function updateTaskStatus(taskId: string, status: string, rowVersion?: number) {
  const response = await apiClient.patch<ApiResponse<TaskItem>>(
    `/api/tasks/${taskId}/status`,
    rowVersion === undefined ? { status } : { status, rowVersion },
    { auth: true },
  );

  return response.data;
}

export async function assignTask(taskId: string, userId: string, rowVersion?: number) {
  const response = await apiClient.patch<ApiResponse<TaskItem>>(
    `/api/tasks/${taskId}/assign`,
    rowVersion === undefined ? { userId } : { userId, rowVersion },
    { auth: true },
  );

  return response.data;
}

export async function deleteTask(taskId: string) {
  await apiClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}`, {
    auth: true,
  });
}

export async function getTaskActivity(taskId: string) {
  const response = await apiClient.get<ApiResponse<TaskActivity[]>>(`/api/tasks/${taskId}/activity`, {
    auth: true,
  });

  return response.data;
}

export async function getTaskChecklist(taskId: string) {
  const response = await apiClient.get<ApiResponse<TaskChecklistItem[]>>(`/api/tasks/${taskId}/checklist`, {
    auth: true,
  });

  return response.data;
}

export async function getTaskChecklistSummary(taskId: string) {
  const response = await apiClient.get<ApiResponse<TaskChecklistSummary>>(`/api/tasks/${taskId}/checklist/summary`, {
    auth: true,
  });

  return response.data;
}

export async function addTaskChecklistItem(taskId: string, payload: CreateTaskChecklistInput) {
  const response = await apiClient.post<ApiResponse<TaskChecklistItem>>(
    `/api/tasks/${taskId}/checklist`,
    { title: payload.title, order: payload.order ?? 0 },
    { auth: true },
  );

  return response.data;
}

export async function toggleTaskChecklistItem(taskId: string, checklistItemId: string) {
  const response = await apiClient.patch<ApiResponse<TaskChecklistItem>>(
    `/api/tasks/${taskId}/checklist/${checklistItemId}/toggle`,
    {},
    { auth: true },
  );

  return response.data;
}

export async function updateTaskChecklistItemCompletion(taskId: string, checklistItemId: string, isCompleted: boolean) {
  const response = await apiClient.patch<ApiResponse<TaskChecklistItem>>(
    `/api/tasks/${taskId}/checklist/${checklistItemId}`,
    { isCompleted },
    { auth: true },
  );

  return response.data;
}

export async function getTaskComments(taskId: string) {
  const response = await apiClient.get<ApiResponse<TaskComment[]>>(`/api/tasks/${taskId}/comments`, {
    auth: true,
  });

  return response.data;
}

export async function addTaskComment(taskId: string, payload: CreateTaskCommentInput) {
  const response = await apiClient.post<ApiResponse<TaskComment>>(`/api/tasks/${taskId}/comments`, payload, {
    auth: true,
  });

  return response.data;
}

export async function updateTaskComment(taskId: string, commentId: string, content: string) {
  const response = await apiClient.put<ApiResponse<TaskComment>>(
    `/api/tasks/${taskId}/comments/${commentId}`,
    { content },
    { auth: true },
  );

  return response.data;
}

export async function deleteTaskComment(taskId: string, commentId: string) {
  await apiClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/comments/${commentId}`, {
    auth: true,
  });
}

export async function getTaskAttachments(taskId: string) {
  const response = await apiClient.get<ApiResponse<TaskAttachment[]>>(`/api/tasks/${taskId}/attachments`, {
    auth: true,
  });

  return response.data;
}

export async function uploadTaskAttachment(taskId: string, file: File) {
  const payload = new FormData();
  payload.append("file", file);

  const response = await apiClient.post<ApiResponse<TaskAttachment>>(`/api/tasks/${taskId}/attachments/upload`, payload, {
    auth: true,
  });

  return response.data;
}

export async function deleteTaskAttachment(taskId: string, attachmentId: string) {
  await apiClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/attachments/${attachmentId}`, {
    auth: true,
  });
}

export async function downloadTaskAttachment(taskId: string, attachmentId: string, fileName: string) {
  const fileBlob = await apiClient.blob(`/api/tasks/${taskId}/attachments/${attachmentId}/download`, {
    auth: true,
  });

  const objectUrl = window.URL.createObjectURL(fileBlob);
  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  window.URL.revokeObjectURL(objectUrl);
}
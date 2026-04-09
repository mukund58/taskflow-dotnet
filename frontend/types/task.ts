export type TaskStatus = "Todo" | "In Progress" | "Done";
export type TaskPriority = "Low" | "Medium" | "High";

export type TaskItem = {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  projectId: string;
  assignedUserId?: string | null;
  dueDate?: string | null;
  createdAt?: string;
  rowVersion?: number;
};

export type TaskQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  assignedTo?: string;
  sortBy?: string;
  sortDescending?: boolean;
};

export type CreateTaskInput = {
  title: string;
  description: string;
  projectId: string;
  dueDate?: string | null;
  priority: TaskPriority;
};

export type UpdateTaskInput = {
  title: string;
  description: string;
  status: string;
  priority: string;
  assignedUserId?: string | null;
  dueDate?: string | null;
  rowVersion?: number;
};

export type TaskActivity = {
  id: string;
  taskItemId: string;
  action: string;
  oldValue?: string | null;
  newValue?: string | null;
  actorUserId: string;
  createdAt: string;
};

export type TaskChecklistItem = {
  id: string;
  title: string;
  isCompleted: boolean;
  order?: number;
  position?: number;
  taskId?: string;
  taskItemId?: string;
  createdAt: string;
  completedAt?: string | null;
};

export type CreateTaskChecklistInput = {
  title: string;
  order?: number;
};

export type TaskChecklistSummary = {
  totalItems: number;
  completedItems: number;
  percentageComplete: number;
};

export type TaskComment = {
  id: string;
  content: string;
  taskId: string;
  authorId: string;
  authorName: string;
  authorEmail: string;
  createdAt: string;
  updatedAt?: string | null;
};

export type CreateTaskCommentInput = {
  content: string;
};

export type TaskAttachment = {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  fileExtension: string;
  storagePath: string;
  taskId: string;
  uploadedByUserId: string;
  uploadedByUserName: string;
  uploadedAt: string;
};
export type UserTaskStats = {
  userId: string;
  userName: string;
  totalTasks: number;
  activeTasks: number;
  completedTasks: number;
  overdueTasks: number;
};

export type UserWorkload = {
  userId: string;
  userName: string;
  taskCount: number;
  activeTaskCount: number;
};

export type WorkloadDistribution = {
  averageTasksPerUser: number;
  averageActiveTasks: number;
  mostLoadedUser: UserWorkload;
  leastLoadedUser: UserWorkload;
  overdueBySeverity: Record<string, number>;
  tasksDueToday: number;
  tasksDueThisWeek: number;
  tasksDueThisMonth: number;
};

export type DashboardStats = {
  totalTasks: number;
  totalUsers: number;
  activeTasks: number;
  completedTasks: number;
  overdueTasks: number;
  tasksPerUser: UserTaskStats[];
  tasksByStatus: Record<string, number>;
  tasksByPriority: Record<string, number>;
  workloadDistribution: WorkloadDistribution;
};
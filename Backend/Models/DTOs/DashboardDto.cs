namespace Backend.Models.DTOs;

public class DashboardStatsDto
{
    public int TotalTasks { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public List<UserTaskStatsDto> TasksPerUser { get; set; } = new();
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
    public WorkloadDistributionDto WorkloadDistribution { get; set; } = new();
}

public class UserTaskStatsDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
}

public class WorkloadDistributionDto
{
    public double AverageTasksPerUser { get; set; }
    public double AverageActiveTasks { get; set; }
    public UserWorkloadDto MostLoadedUser { get; set; } = new();
    public UserWorkloadDto LeastLoadedUser { get; set; } = new();
    public Dictionary<string, int> OverdueBySeverity { get; set; } = new(); // Priority: count
    public int TasksDueToday { get; set; }
    public int TasksDueThisWeek { get; set; }
    public int TasksDueThisMonth { get; set; }
}

public class UserWorkloadDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int ActiveTaskCount { get; set; }
}

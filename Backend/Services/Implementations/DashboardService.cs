namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardStats()
    {
        var now = DateTime.UtcNow;

        // Get all tasks (excluding soft deleted)
        var allTasks = await _context.Tasks
            .AsNoTracking()
            .ToListAsync();

        // Get all users
        var allUsers = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        // Calculate basic stats
        var totalTasks = allTasks.Count;
        var totalUsers = allUsers.Count;
        var activeTasks = allTasks.Count(t => t.Status != "Done");
        var completedTasks = allTasks.Count(t => t.Status == "Done");
        var overdueTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != "Done");

        // Tasks by status
        var tasksByStatus = allTasks
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Tasks by priority
        var tasksByPriority = allTasks
            .GroupBy(t => t.Priority)
            .ToDictionary(g => g.Key, g => g.Count());

        // Tasks per user
        var tasksPerUser = new List<UserTaskStatsDto>();

        foreach (var user in allUsers)
        {
            var userTasks = allTasks.Where(t => t.AssignedUserId == user.Id).ToList();
            var userActiveTasks = userTasks.Count(t => t.Status != "Done");
            var userCompletedTasks = userTasks.Count(t => t.Status == "Done");
            var userOverdueTasks = userTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != "Done");

            tasksPerUser.Add(new UserTaskStatsDto
            {
                UserId = user.Id,
                UserName = user.Name,
                TotalTasks = userTasks.Count,
                ActiveTasks = userActiveTasks,
                CompletedTasks = userCompletedTasks,
                OverdueTasks = userOverdueTasks
            });
        }

        // Calculate workload distribution
        var workloadDistribution = CalculateWorkloadDistribution(allTasks, tasksPerUser, now);

        return new DashboardStatsDto
        {
            TotalTasks = totalTasks,
            TotalUsers = totalUsers,
            ActiveTasks = activeTasks,
            CompletedTasks = completedTasks,
            OverdueTasks = overdueTasks,
            TasksByStatus = tasksByStatus,
            TasksByPriority = tasksByPriority,
            TasksPerUser = tasksPerUser,
            WorkloadDistribution = workloadDistribution
        };
    }

    private WorkloadDistributionDto CalculateWorkloadDistribution(List<Backend.Models.Entities.TaskItem> allTasks, List<UserTaskStatsDto> tasksPerUser, DateTime now)
    {
        var distribution = new WorkloadDistributionDto();

        // Average tasks per user
        distribution.AverageTasksPerUser = tasksPerUser.Count > 0
            ? tasksPerUser.Average(u => u.TotalTasks)
            : 0;

        distribution.AverageActiveTasks = tasksPerUser.Count > 0
            ? tasksPerUser.Average(u => u.ActiveTasks)
            : 0;

        // Most and least loaded users
        if (tasksPerUser.Any())
        {
            var mostLoaded = tasksPerUser.OrderByDescending(u => u.TotalTasks).FirstOrDefault();
            if (mostLoaded != null)
            {
                distribution.MostLoadedUser = new UserWorkloadDto
                {
                    UserId = mostLoaded.UserId,
                    UserName = mostLoaded.UserName,
                    TaskCount = mostLoaded.TotalTasks,
                    ActiveTaskCount = mostLoaded.ActiveTasks
                };
            }

            var leastLoaded = tasksPerUser.OrderBy(u => u.TotalTasks).FirstOrDefault();
            if (leastLoaded != null)
            {
                distribution.LeastLoadedUser = new UserWorkloadDto
                {
                    UserId = leastLoaded.UserId,
                    UserName = leastLoaded.UserName,
                    TaskCount = leastLoaded.TotalTasks,
                    ActiveTaskCount = leastLoaded.ActiveTasks
                };
            }
        }

        // Overdue tasks by priority
        var overdueTasks = allTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != "Done");
        distribution.OverdueBySeverity = overdueTasks
            .GroupBy(t => t.Priority)
            .ToDictionary(g => g.Key, g => g.Count());

        // Tasks due dates
        var today = now.Date;
        var weekEnd = today.AddDays(7);
        var monthEnd = today.AddMonths(1);

        distribution.TasksDueToday = allTasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value.Date == today &&
            t.Status != "Done");

        distribution.TasksDueThisWeek = allTasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value.Date > today &&
            t.DueDate.Value.Date <= weekEnd &&
            t.Status != "Done");

        distribution.TasksDueThisMonth = allTasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value.Date > today &&
            t.DueDate.Value.Date <= monthEnd &&
            t.Status != "Done");

        return distribution;
    }
}
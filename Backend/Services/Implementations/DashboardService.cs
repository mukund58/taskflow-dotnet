namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DashboardService> _logger;
    private const string DashboardStatsCacheKey = "dashboard:stats:v1";
    private static readonly DistributedCacheEntryOptions DashboardCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
        SlidingExpiration = TimeSpan.FromSeconds(30)
    };

    public DashboardService(AppDbContext context, IDistributedCache cache, ILogger<DashboardService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStats(Guid userId, bool elevatedAccess)
    {
        var cacheKey = elevatedAccess ? DashboardStatsCacheKey : $"{DashboardStatsCacheKey}:{userId}";
        try
        {
            var cachedPayload = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrWhiteSpace(cachedPayload))
            {
                var cachedStats = JsonSerializer.Deserialize<DashboardStatsDto>(cachedPayload);
                if (cachedStats != null)
                {
                    return cachedStats;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read dashboard stats from cache");
        }

        var stats = await BuildDashboardStatsAsync(userId, elevatedAccess);

        try
        {
            var payload = JsonSerializer.Serialize(stats);
            await _cache.SetStringAsync(cacheKey, payload, DashboardCacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache dashboard stats");
        }

        return stats;
    }

    private async Task<DashboardStatsDto> BuildDashboardStatsAsync(Guid userId, bool elevatedAccess)
    {
        var now = DateTime.UtcNow;

        List<Guid>? accessibleProjectIds = null;
        if (!elevatedAccess)
        {
            accessibleProjectIds = await _context.Projects
                .AsNoTracking()
                .Where(p =>
                    p.OwnerUserId == userId ||
                    _context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync();
        }

        // Get all tasks (excluding soft deleted)
        var tasksQuery = _context.Tasks.AsNoTracking().AsQueryable();
        if (accessibleProjectIds != null)
        {
            tasksQuery = tasksQuery.Where(t => accessibleProjectIds.Contains(t.ProjectId));
        }
        var allTasks = await tasksQuery.ToListAsync();

        // Get all users
        var usersQuery = _context.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (accessibleProjectIds != null)
        {
            var accessibleUserIds = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => accessibleProjectIds.Contains(pm.ProjectId))
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            var ownersList = await _context.Projects
                .AsNoTracking()
                .Where(p => accessibleProjectIds.Contains(p.Id) && p.OwnerUserId.HasValue)
                .Select(p => p.OwnerUserId!.Value)
                .Distinct()
                .ToListAsync();

            var unionUserIds = accessibleUserIds.Union(ownersList).ToList();
            usersQuery = usersQuery.Where(u => unionUserIds.Contains(u.Id));
        }
        var allUsers = await usersQuery.ToListAsync();

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
namespace Backend.Data;

using Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(SeedingOptions options, CancellationToken cancellationToken = default)
    {
        var targetUsers = Math.Max(1, options.Users);
        var targetProjects = Math.Max(1, options.Projects);
        var targetTasks = Math.Max(1, options.Tasks);

        var users = await EnsureUsersAsync(targetUsers, options.DefaultPassword, cancellationToken);
        var projects = await EnsureProjectsAsync(targetProjects, users, cancellationToken);
        await EnsureProjectOwnerMembershipsAsync(projects, cancellationToken);
        await EnsureTasksAsync(targetTasks, users, projects, cancellationToken);

        var userCount = await _context.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
        var projectCount = await _context.Projects.CountAsync(cancellationToken);
        var taskCount = await _context.Tasks.CountAsync(cancellationToken);

        _logger.LogInformation(
            "Seeding completed. Users={UserCount}, Projects={ProjectCount}, Tasks={TaskCount}",
            userCount,
            projectCount,
            taskCount);
    }

    private async Task<List<User>> EnsureUsersAsync(int targetUsers, string defaultPassword, CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        if (users.Count >= targetUsers)
        {
            _logger.LogInformation("Users already seeded. Current count: {UserCount}", users.Count);
            return users;
        }

        var seedUsers = BuildUserDefinitions(targetUsers);
        var existingEmails = users
            .Select(u => u.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seedUser in seedUsers)
        {
            if (existingEmails.Contains(seedUser.Email))
                continue;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = seedUser.Name,
                Email = seedUser.Email,
                Role = seedUser.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword)
            };

            users.Add(user);
            _context.Users.Add(user);
            existingEmails.Add(seedUser.Email);
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Users seeded. Current count: {UserCount}", users.Count);
        return users;
    }

    private async Task<List<Project>> EnsureProjectsAsync(int targetProjects, IReadOnlyList<User> users, CancellationToken cancellationToken)
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        if (projects.Count >= targetProjects)
        {
            _logger.LogInformation("Projects already seeded. Current count: {ProjectCount}", projects.Count);
            return projects;
        }

        var ownerIds = users.Select(u => u.Id).ToArray();

        for (var i = projects.Count; i < targetProjects; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = $"Seed Project {i + 1}",
                Description = "Auto-generated seeded project",
                OwnerUserId = ownerIds[i % ownerIds.Length]
            };

            projects.Add(project);
            _context.Projects.Add(project);
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Projects seeded. Current count: {ProjectCount}", projects.Count);
        return projects;
    }

    private async Task EnsureProjectOwnerMembershipsAsync(IReadOnlyList<Project> projects, CancellationToken cancellationToken)
    {
        foreach (var project in projects)
        {
            if (!project.OwnerUserId.HasValue)
                continue;

            var ownerUserId = project.OwnerUserId.Value;

            var alreadyMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == ownerUserId, cancellationToken);

            if (alreadyMember)
                continue;

            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = ownerUserId,
                Role = "Admin",
                AddedByUserId = ownerUserId,
                AddedAt = DateTime.UtcNow
            });
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureTasksAsync(int targetTasks, IReadOnlyList<User> users, IReadOnlyList<Project> projects, CancellationToken cancellationToken)
    {
        var existingCount = await _context.Tasks.CountAsync(cancellationToken);
        if (existingCount >= targetTasks)
        {
            _logger.LogInformation("Tasks already seeded. Current count: {TaskCount}", existingCount);
            return;
        }

        var statuses = new[] { "Todo", "In Progress", "Done" };
        var priorities = new[] { "Low", "Medium", "High" };
        var userIds = users.Select(u => u.Id).ToArray();
        var projectIds = projects.Select(p => p.Id).ToArray();
        var random = new Random(42);

        for (var i = existingCount; i < targetTasks; i++)
        {
            var status = statuses[i % statuses.Length];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));
            var dueDate = DateTime.UtcNow.Date.AddDays(random.Next(-7, 21));

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = $"Seed Task {i + 1}",
                Description = "Auto-generated seeded task",
                Status = status,
                Priority = priorities[i % priorities.Length],
                ProjectId = projectIds[i % projectIds.Length],
                AssignedUserId = userIds[i % userIds.Length],
                CreatedAt = createdAt,
                DueDate = dueDate
            };

            _context.Tasks.Add(task);
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);

        var finalCount = await _context.Tasks.CountAsync(cancellationToken);
        _logger.LogInformation("Tasks seeded. Current count: {TaskCount}", finalCount);
    }

    private static List<SeedUserDefinition> BuildUserDefinitions(int targetUsers)
    {
        var defaults = new List<SeedUserDefinition>
        {
            new("Seed Admin", "seed.admin@example.com", "Admin"),
            new("Seed Manager", "seed.manager@example.com", "Manager"),
            new("Seed User", "seed.user@example.com", "User"),
            new("Seed Viewer", "seed.viewer@example.com", "Viewer")
        };

        var users = new List<SeedUserDefinition>();

        for (var i = 0; i < targetUsers; i++)
        {
            if (i < defaults.Count)
            {
                users.Add(defaults[i]);
            }
            else
            {
                users.Add(new SeedUserDefinition($"Seed User {i + 1}", $"seed.user{i + 1}@example.com", "User"));
            }
        }

        return users;
    }

    private sealed record SeedUserDefinition(string Name, string Email, string Role);
}

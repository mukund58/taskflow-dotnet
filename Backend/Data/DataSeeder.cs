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

    public async Task SeedAsync(
        SeedingOptions options,
        CancellationToken cancellationToken = default)
    {
        var targetUsers = Math.Max(1, options.Users);
        var targetProjects = Math.Max(1, options.Projects);
        var targetTasks = Math.Max(1, options.Tasks);

        var users = await EnsureUsersAsync(
            targetUsers,
            options.DefaultPassword,
            cancellationToken);

        var projects = await EnsureProjectsAsync(
            targetProjects,
            users,
            cancellationToken);

        await EnsureProjectOwnerMembershipsAsync(
            projects,
            cancellationToken);

        await EnsureTasksAsync(
            targetTasks,
            users,
            projects,
            cancellationToken);

        var userCount = await _context.Users
            .CountAsync(u => !u.IsDeleted, cancellationToken);

        var projectCount = await _context.Projects
            .CountAsync(cancellationToken);

        var taskCount = await _context.Tasks
            .CountAsync(cancellationToken);

        _logger.LogInformation(
            "Seeding completed. Users={UserCount}, Projects={ProjectCount}, Tasks={TaskCount}",
            userCount,
            projectCount,
            taskCount);
    }

    // =========================================================
    // USERS
    // =========================================================

    private async Task<List<User>> EnsureUsersAsync(
        int targetUsers,
        string defaultPassword,
        CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        if (users.Count >= targetUsers)
        {
            _logger.LogInformation(
                "Users already seeded. Current count: {UserCount}",
                users.Count);

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

        _logger.LogInformation(
            "Users seeded. Current count: {UserCount}",
            users.Count);

        return users;
    }

    // =========================================================
    // PROJECTS
    // =========================================================

    private async Task<List<Project>> EnsureProjectsAsync(
        int targetProjects,
        IReadOnlyList<User> users,
        CancellationToken cancellationToken)
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        if (projects.Count >= targetProjects)
        {
            _logger.LogInformation(
                "Projects already seeded. Current count: {ProjectCount}",
                projects.Count);

            return projects;
        }

        var projectDefinitions = BuildProjectDefinitions(targetProjects);

        var ownerIds = users
            .Select(u => u.Id)
            .ToArray();

        for (var i = projects.Count; i < targetProjects; i++)
        {
            var definition = projectDefinitions[i];

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = definition.Name,
                Description = definition.Description,
                OwnerUserId = ownerIds[i % ownerIds.Length]
            };

            projects.Add(project);
            _context.Projects.Add(project);
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Projects seeded. Current count: {ProjectCount}",
            projects.Count);

        return projects;
    }

    // =========================================================
    // PROJECT MEMBERS
    // =========================================================

    private async Task EnsureProjectOwnerMembershipsAsync(
        IReadOnlyList<Project> projects,
        CancellationToken cancellationToken)
    {
        foreach (var project in projects)
        {
            if (!project.OwnerUserId.HasValue)
                continue;

            var ownerUserId = project.OwnerUserId.Value;

            var alreadyMember = await _context.ProjectMembers
                .AnyAsync(
                    pm =>
                        pm.ProjectId == project.Id &&
                        pm.UserId == ownerUserId,
                    cancellationToken);

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

    // =========================================================
    // TASKS
    // =========================================================

    private async Task EnsureTasksAsync(
        int targetTasks,
        IReadOnlyList<User> users,
        IReadOnlyList<Project> projects,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.Tasks
            .CountAsync(cancellationToken);

        if (existingCount >= targetTasks)
        {
            _logger.LogInformation(
                "Tasks already seeded. Current count: {TaskCount}",
                existingCount);

            return;
        }

        var taskDefinitions = BuildTaskDefinitions();

        var userIds = users
            .Select(u => u.Id)
            .ToArray();

        var projectIds = projects
            .Select(p => p.Id)
            .ToArray();

        var random = new Random(42);

        for (var i = existingCount; i < targetTasks; i++)
        {
            var definition = taskDefinitions[i % taskDefinitions.Count];

            var createdAt = DateTime.UtcNow
                .AddDays(-random.Next(2, 30));

            var dueDate = DateTime.UtcNow
                .Date
                .AddDays(random.Next(-3, 21));

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),

                Title = definition.Title,

                Description = definition.Description,

                Status = definition.Status,

                Priority = definition.Priority,

                ProjectId = projectIds[i % projectIds.Length],

                AssignedUserId = userIds[i % userIds.Length],

                CreatedAt = createdAt,

                DueDate = dueDate
            };

            _context.Tasks.Add(task);
        }

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);

        var finalCount = await _context.Tasks
            .CountAsync(cancellationToken);

        _logger.LogInformation(
            "Tasks seeded. Current count: {TaskCount}",
            finalCount);
    }

    // =========================================================
    // USER DEFINITIONS
    // =========================================================

    private static List<SeedUserDefinition> BuildUserDefinitions(
        int targetUsers)
    {
        var defaults = new List<SeedUserDefinition>
        {
            new(
                "Aarav Patel",
                "aarav.patel@example.com",
                "Admin"
            ),

            new(
                "Priya Shah",
                "priya.shah@example.com",
                "Manager"
            ),

            new(
                "Rahul Mehta",
                "rahul.mehta@example.com",
                "User"
            ),

            new(
                "Neha Joshi",
                "neha.joshi@example.com",
                "User"
            ),

            new(
                "Kunal Desai",
                "kunal.desai@example.com",
                "User"
            ),

            new(
                "Ananya Patel",
                "ananya.patel@example.com",
                "Viewer"
            )
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
                users.Add(
                    new SeedUserDefinition(
                        $"Developer {i + 1}",
                        $"developer{i + 1}@example.com",
                        "User"
                    )
                );
            }
        }

        return users;
    }

    // =========================================================
    // PROJECT DEFINITIONS
    // =========================================================

    private static List<SeedProjectDefinition> BuildProjectDefinitions(
        int targetProjects)
    {
        var defaults = new List<SeedProjectDefinition>
        {
            new(
                "TaskFlow",
                "A project management platform for organizing teams, tasks, and project workflows."
            ),

            new(
                "Inventory Management System",
                "Internal application for tracking products, stock levels, suppliers, and inventory movements."
            ),

            new(
                "Customer Portal",
                "Web portal for customers to submit requests, track progress, and communicate with support."
            ),

            new(
                "Mobile App Redesign",
                "Redesign of the mobile application with improved navigation, accessibility, and user experience."
            ),

            new(
                "Analytics Dashboard",
                "Dashboard for monitoring application usage, team productivity, and project performance."
            ),

            new(
                "Authentication Service",
                "Centralized authentication and authorization service for internal applications."
            )
        };

        var projects = new List<SeedProjectDefinition>();

        for (var i = 0; i < targetProjects; i++)
        {
            if (i < defaults.Count)
            {
                projects.Add(defaults[i]);
            }
            else
            {
                projects.Add(
                    new SeedProjectDefinition(
                        $"Internal Project {i + 1}",
                        "Internal development project for the engineering team."
                    )
                );
            }
        }

        return projects;
    }

    // =========================================================
    // TASK DEFINITIONS
    // =========================================================

    private static List<SeedTaskDefinition> BuildTaskDefinitions()
    {
        return new List<SeedTaskDefinition>
        {
            new(
                "Set up PostgreSQL database",
                "Create the initial database schema and configure the PostgreSQL connection.",
                "Done",
                "High"
            ),

            new(
                "Implement JWT authentication",
                "Add login, token generation, and authentication middleware.",
                "Done",
                "High"
            ),

            new(
                "Create project management API",
                "Implement endpoints for creating, updating, and deleting projects.",
                "In Progress",
                "High"
            ),

            new(
                "Build project dashboard",
                "Create the dashboard UI showing project progress and recent activity.",
                "In Progress",
                "Medium"
            ),

            new(
                "Add task assignment",
                "Allow project managers to assign tasks to team members.",
                "In Progress",
                "Medium"
            ),

            new(
                "Implement task filtering",
                "Add filtering by status, priority, project, and assigned user.",
                "Todo",
                "Medium"
            ),

            new(
                "Add API validation",
                "Validate incoming request data and return meaningful validation errors.",
                "Done",
                "High"
            ),

            new(
                "Improve error handling",
                "Add centralized exception handling and consistent API error responses.",
                "Done",
                "Medium"
            ),

            new(
                "Write API documentation",
                "Document the main API endpoints and request/response formats.",
                "Todo",
                "Low"
            ),

            new(
                "Add project member management",
                "Allow project owners to add and remove team members.",
                "In Progress",
                "High"
            ),

            new(
                "Implement search",
                "Add search functionality for projects and tasks.",
                "Todo",
                "Medium"
            ),

            new(
                "Optimize database queries",
                "Review frequently used queries and add indexes where necessary.",
                "Todo",
                "High"
            ),

            new(
                "Add automated tests",
                "Create unit and integration tests for the core project and task services.",
                "In Progress",
                "High"
            ),

            new(
                "Review responsive layout",
                "Ensure the dashboard works correctly on tablet and mobile screen sizes.",
                "Todo",
                "Low"
            ),

            new(
                "Prepare production deployment",
                "Configure environment variables, logging, and production deployment settings.",
                "Todo",
                "High"
            )
        };
    }

    // =========================================================
    // RECORDS
    // =========================================================

    private sealed record SeedUserDefinition(
        string Name,
        string Email,
        string Role
    );

    private sealed record SeedProjectDefinition(
        string Name,
        string Description
    );

    private sealed record SeedTaskDefinition(
        string Title,
        string Description,
        string Status,
        string Priority
    );
}
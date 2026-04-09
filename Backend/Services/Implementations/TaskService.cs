namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem> Create(CreateTaskDto dto, Guid actorUserId)
    {
        return await ExecuteInTransactionAsync(() =>
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                ProjectId = dto.ProjectId,
                DueDate = dto.DueDate,
                Priority = dto.Priority
            };

            _context.Tasks.Add(task);

            _context.TaskActivities.Add(new TaskActivity
            {
                Id = Guid.NewGuid(),
                TaskItemId = task.Id,
                Action = "TaskCreated",
                NewValue = task.Title,
                ActorUserId = actorUserId
            });

            return Task.FromResult(task);
        });
    }

    public async Task<List<TaskItem>> GetAll(string? status, Guid? assignedTo)
    {
        var query = _context.Tasks
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        if (assignedTo.HasValue)
            query = query.Where(x => x.AssignedUserId == assignedTo);

        return await query.ToListAsync();
    }

    public async Task<PaginatedResponseDto<TaskItem>> GetAllPaginatedAsync(TaskQueryDto query)
    {
        var taskQuery = _context.Tasks
            .AsNoTracking()
            .AsQueryable();

        if (query.ProjectIds is { Count: > 0 })
            taskQuery = taskQuery.Where(x => query.ProjectIds.Contains(x.ProjectId));
        else if (query.ProjectIds is { Count: 0 })
            taskQuery = taskQuery.Where(_ => false);

        // Apply filters
        if (!string.IsNullOrEmpty(query.Status))
            taskQuery = taskQuery.Where(x => x.Status == query.Status);

        if (query.AssignedTo.HasValue)
            taskQuery = taskQuery.Where(x => x.AssignedUserId == query.AssignedTo);

        // Apply sorting
        taskQuery = query.SortBy?.ToLower() switch
        {
            "duedate" => query.SortDescending
                ? taskQuery.OrderByDescending(x => x.DueDate)
                : taskQuery.OrderBy(x => x.DueDate),
            "priority" => query.SortDescending
                ? taskQuery.OrderByDescending(x => x.Priority)
                : taskQuery.OrderBy(x => x.Priority),
            "title" => query.SortDescending
                ? taskQuery.OrderByDescending(x => x.Title)
                : taskQuery.OrderBy(x => x.Title),
            "status" => query.SortDescending
                ? taskQuery.OrderByDescending(x => x.Status)
                : taskQuery.OrderBy(x => x.Status),
            _ => query.SortDescending
                ? taskQuery.OrderByDescending(x => x.CreatedAt)
                : taskQuery.OrderBy(x => x.CreatedAt),
        };

        var total = await taskQuery.CountAsync();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100)); // Max 100 per page
        var skip = (page - 1) * pageSize;

        var items = await taskQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PaginatedResponseDto<TaskItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<TaskItem> GetById(Guid taskId)
    {
        return await GetTaskByIdOrThrowAsync(taskId);
    }

    public async Task<TaskItem> Update(Guid taskId, UpdateTaskDto dto, Guid actorUserId)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetTaskByIdOrThrowAsync(taskId);
            ApplyExpectedRowVersion(task, dto.RowVersion);

            var oldStatus = task.Status;
            var oldAssignedUserId = task.AssignedUserId;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.AssignedUserId = dto.AssignedUserId;
            task.DueDate = dto.DueDate;

            if (!string.Equals(oldStatus, task.Status, StringComparison.OrdinalIgnoreCase))
            {
                _context.TaskActivities.Add(new TaskActivity
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    Action = "StatusChanged",
                    OldValue = oldStatus,
                    NewValue = task.Status,
                    ActorUserId = actorUserId
                });
            }

            if (oldAssignedUserId != task.AssignedUserId)
            {
                _context.TaskActivities.Add(new TaskActivity
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    Action = "Assigned",
                    OldValue = oldAssignedUserId?.ToString(),
                    NewValue = task.AssignedUserId?.ToString(),
                    ActorUserId = actorUserId
                });
            }

            return task;
        });
    }

    public async Task Delete(Guid taskId)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetTaskByIdOrThrowAsync(taskId);
            task.IsDeleted = true;
        });
    }

    public async Task<TaskItem> UpdateStatus(Guid taskId, string status, Guid actorUserId, long? expectedRowVersion = null)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetTaskByIdOrThrowAsync(taskId);
            ApplyExpectedRowVersion(task, expectedRowVersion);

            var oldStatus = task.Status;
            task.Status = status;

            if (!string.Equals(oldStatus, status, StringComparison.OrdinalIgnoreCase))
            {
                _context.TaskActivities.Add(new TaskActivity
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    Action = "StatusChanged",
                    OldValue = oldStatus,
                    NewValue = status,
                    ActorUserId = actorUserId
                });
            }

            return task;
        });
    }

    public async Task<TaskItem> Assign(Guid taskId, Guid userId, Guid actorUserId, long? expectedRowVersion = null)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetTaskByIdOrThrowAsync(taskId);
            ApplyExpectedRowVersion(task, expectedRowVersion);

            var oldAssignedUserId = task.AssignedUserId;
            task.AssignedUserId = userId;

            if (oldAssignedUserId != userId)
            {
                _context.TaskActivities.Add(new TaskActivity
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    Action = "Assigned",
                    OldValue = oldAssignedUserId?.ToString(),
                    NewValue = userId.ToString(),
                    ActorUserId = actorUserId
                });
            }

            return task;
        });
    }

    public async Task<TaskItem> UpdatePriority(Guid taskId, string priority)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetTaskByIdOrThrowAsync(taskId);
            task.Priority = priority;
            return task;
        });
    }

    public async Task<List<TaskActivity>> GetActivity(Guid taskId)
    {
        await GetTaskByIdOrThrowAsync(taskId);

        return await _context.TaskActivities
            .AsNoTracking()
            .Where(x => x.TaskItemId == taskId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ChecklistItem>> GetChecklistItems(Guid taskId)
    {
        await GetTaskByIdOrThrowAsync(taskId);

        return await _context.ChecklistItems
            .AsNoTracking()
            .Where(x => x.TaskItemId == taskId)
            .OrderBy(x => x.Position)
            .ToListAsync();
    }

    public async Task<ChecklistItem> AddChecklistItem(Guid taskId, CreateChecklistItemDto dto)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            await GetTaskByIdOrThrowAsync(taskId);

            var maxPosition = await _context.ChecklistItems
                .Where(x => x.TaskItemId == taskId)
                .Select(x => (int?)x.Position)
                .MaxAsync();

            var checklistItem = new ChecklistItem
            {
                Id = Guid.NewGuid(),
                TaskItemId = taskId,
                Title = dto.Title,
                Position = (maxPosition ?? 0) + 1
            };

            _context.ChecklistItems.Add(checklistItem);
            return checklistItem;
        });
    }

    public async Task<ChecklistItem> UpdateChecklistItemCompletion(Guid taskId, Guid checklistItemId, bool isCompleted)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            await GetTaskByIdOrThrowAsync(taskId);

            var checklistItem = await _context.ChecklistItems
                .FirstOrDefaultAsync(x => x.TaskItemId == taskId && x.Id == checklistItemId);

            if (checklistItem == null)
                throw new KeyNotFoundException("Checklist item not found");

            checklistItem.IsCompleted = isCompleted;
            checklistItem.CompletedAt = isCompleted ? DateTime.UtcNow : null;

            return checklistItem;
        });
    }

    private void ApplyExpectedRowVersion(TaskItem task, long? expectedRowVersion)
    {
        if (!expectedRowVersion.HasValue)
            return;

        _context.Entry(task).Property(x => x.RowVersion).OriginalValue = expectedRowVersion.Value;
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        if (!SupportsExplicitTransactions())
        {
            var fallbackResult = await action();
            await _context.SaveChangesAsync();
            return fallbackResult;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var result = await action();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        if (!SupportsExplicitTransactions())
        {
            await action();
            await _context.SaveChangesAsync();
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await action();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private bool SupportsExplicitTransactions()
    {
        return !string.Equals(
            _context.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.InMemory",
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<TaskItem> GetTaskByIdOrThrowAsync(Guid taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
            throw new KeyNotFoundException("Task not found");

        return task;
    }
}
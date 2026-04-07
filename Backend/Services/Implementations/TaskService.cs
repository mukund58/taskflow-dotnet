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

        await _context.SaveChangesAsync();

        return task;
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

    public async Task<TaskItem> GetById(Guid taskId)
    {
        return await GetTaskByIdOrThrowAsync(taskId);
    }

    public async Task<TaskItem> Update(Guid taskId, UpdateTaskDto dto, Guid actorUserId)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);
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

        await _context.SaveChangesAsync();

        return task;
    }

    public async Task Delete(Guid taskId)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);

        task.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<TaskItem> UpdateStatus(Guid taskId, string status, Guid actorUserId)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);
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

        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem> Assign(Guid taskId, Guid userId, Guid actorUserId)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);
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

        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem> UpdatePriority(Guid taskId, string priority)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);

        task.Priority = priority;

        await _context.SaveChangesAsync();

        return task;
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
        await _context.SaveChangesAsync();

        return checklistItem;
    }

    public async Task<ChecklistItem> UpdateChecklistItemCompletion(Guid taskId, Guid checklistItemId, bool isCompleted)
    {
        await GetTaskByIdOrThrowAsync(taskId);

        var checklistItem = await _context.ChecklistItems
            .FirstOrDefaultAsync(x => x.TaskItemId == taskId && x.Id == checklistItemId);

        if (checklistItem == null)
            throw new KeyNotFoundException("Checklist item not found");

        checklistItem.IsCompleted = isCompleted;
        checklistItem.CompletedAt = isCompleted ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync();

        return checklistItem;
    }

    private async Task<TaskItem> GetTaskByIdOrThrowAsync(Guid taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
            throw new KeyNotFoundException("Task not found");

        return task;
    }
}
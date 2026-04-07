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

    public async Task<TaskItem> Create(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            ProjectId = dto.ProjectId,
            DueDate = dto.DueDate,
            Priority = dto.Priority
        };

        _context.Tasks.Add(task);
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

    public async Task<PaginatedResponseDto<TaskItem>> GetAllPaginatedAsync(TaskQueryDto query)
    {
        var taskQuery = _context.Tasks
            .AsNoTracking()
            .AsQueryable();

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

    public async Task<TaskItem> Update(Guid taskId, UpdateTaskDto dto)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.AssignedUserId = dto.AssignedUserId;
        task.DueDate = dto.DueDate;

        await _context.SaveChangesAsync();

        return task;
    }

    public async Task Delete(Guid taskId)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);

        task.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<TaskItem> UpdateStatus(Guid taskId, string status)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);

        task.Status = status;

        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem> Assign(Guid taskId, Guid userId)
    {
        var task = await GetTaskByIdOrThrowAsync(taskId);

        task.AssignedUserId = userId;

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

    private async Task<TaskItem> GetTaskByIdOrThrowAsync(Guid taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
            throw new KeyNotFoundException("Task not found");

        return task;
    }
}
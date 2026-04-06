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
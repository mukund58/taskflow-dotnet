namespace Backend.Services.Implementations;

using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interface;
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
            ProjectId = dto.ProjectId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<List<TaskItem>> GetAll(string? status, Guid? assignedTo)
    {
        var query = _context.Tasks.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        if (assignedTo.HasValue)
            query = query.Where(x => x.AssignedUserId == assignedTo);

        return await query.ToListAsync();
    }

    public async Task<TaskItem> UpdateStatus(Guid taskId, string status)
    {
        var task = await _context.Tasks.FindAsync(taskId);

        if (task == null)
            throw new Exception("Task not found");

        task.Status = status;

        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem> Assign(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);

        if (task == null)
            throw new Exception("Task not found");

        task.AssignedUserId = userId;

        await _context.SaveChangesAsync();

        return task;
    }
}
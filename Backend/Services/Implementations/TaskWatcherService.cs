namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class TaskWatcherService : ITaskWatcherService
{
    private readonly AppDbContext _context;

    public TaskWatcherService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddWatcherAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new KeyNotFoundException("Task not found");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var existingWatcher = await _context.Set<TaskWatcher>()
            .FirstOrDefaultAsync(w => w.TaskId == taskId && w.UserId == userId);

        if (existingWatcher != null)
            throw new InvalidOperationException("User is already watching this task");

        var watcher = new TaskWatcher
        {
            TaskId = taskId,
            UserId = userId,
            WatchedSince = DateTime.UtcNow
        };

        _context.Set<TaskWatcher>().Add(watcher);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveWatcherAsync(Guid taskId, Guid userId)
    {
        var watcher = await _context.Set<TaskWatcher>()
            .FirstOrDefaultAsync(w => w.TaskId == taskId && w.UserId == userId);

        if (watcher == null)
            throw new KeyNotFoundException("Watcher not found");

        _context.Set<TaskWatcher>().Remove(watcher);
        await _context.SaveChangesAsync();
    }

    public async Task<List<object>> GetTaskWatchersAsync(Guid taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new KeyNotFoundException("Task not found");

        return await _context.Set<TaskWatcher>()
            .Where(w => w.TaskId == taskId)
            .Include(w => w.User)
            .Select(w => (object)new
            {
                w.User.Id,
                w.User.Name,
                w.User.Email,
                w.WatchedSince
            })
            .ToListAsync();
    }

    public async Task<List<object>> GetUserWatchedTasksAsync(Guid userId)
    {
        var watchedTasks = await _context.Set<TaskWatcher>()
            .Where(w => w.UserId == userId)
            .Include(w => w.Task)
            .Select(w => (object)w.Task)
            .ToListAsync();

        return watchedTasks;
    }

    public async Task<bool> IsWatchingAsync(Guid taskId, Guid userId)
    {
        return await _context.Set<TaskWatcher>()
            .AnyAsync(w => w.TaskId == taskId && w.UserId == userId);
    }
}

namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface ITaskWatcherService
{
    Task AddWatcherAsync(Guid taskId, Guid userId);
    Task RemoveWatcherAsync(Guid taskId, Guid userId);
    Task<List<object>> GetTaskWatchersAsync(Guid taskId);
    Task<List<object>> GetUserWatchedTasksAsync(Guid userId);
    Task<bool> IsWatchingAsync(Guid taskId, Guid userId);
}

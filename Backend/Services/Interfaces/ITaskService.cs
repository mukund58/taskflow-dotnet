namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface ITaskService
{
    Task<TaskItem> Create(CreateTaskDto dto, Guid actorUserId);
    Task<List<TaskItem>> GetAll(string? status, Guid? assignedTo);
    Task<TaskItem> GetById(Guid taskId);
    Task<TaskItem> Update(Guid taskId, UpdateTaskDto dto, Guid actorUserId);
    Task Delete(Guid taskId);
    Task<TaskItem> UpdateStatus(Guid taskId, string status, Guid actorUserId);
    Task<TaskItem> Assign(Guid taskId, Guid userId, Guid actorUserId);
    Task<TaskItem> UpdatePriority(Guid taskId, string priority);
    Task<List<TaskActivity>> GetActivity(Guid taskId);
    Task<List<ChecklistItem>> GetChecklistItems(Guid taskId);
    Task<ChecklistItem> AddChecklistItem(Guid taskId, CreateChecklistItemDto dto);
    Task<ChecklistItem> UpdateChecklistItemCompletion(Guid taskId, Guid checklistItemId, bool isCompleted);
}

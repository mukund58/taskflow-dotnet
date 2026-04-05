namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface ITaskService
{
    Task<TaskItem> Create(CreateTaskDto dto);
    Task<List<TaskItem>> GetAll(string? status, Guid? assignedTo);
    Task<TaskItem> Update(Guid taskId, UpdateTaskDto dto);
    Task Delete(Guid taskId);
    Task<TaskItem> UpdateStatus(Guid taskId, string status);
    Task<TaskItem> Assign(Guid taskId, Guid userId);
}

namespace Backend.Services.Interface;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface ITaskService
{
    Task<TaskItem> Create(CreateTaskDto dto);
    Task<List<TaskItem>> GetAll(string? status, Guid? assignedTo);
    Task<TaskItem> UpdateStatus(Guid taskId, string status);
    Task<TaskItem> Assign(Guid taskId, Guid userId);
}

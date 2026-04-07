namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface ITaskAttachmentService
{
    Task<TaskAttachmentDto> CreateAttachmentAsync(Guid taskId, CreateTaskAttachmentDto attachmentDto, Guid userId);
    Task<List<TaskAttachmentDto>> GetTaskAttachmentsAsync(Guid taskId);
    Task<TaskAttachmentDto> GetAttachmentByIdAsync(Guid attachmentId);
    Task DeleteAttachmentAsync(Guid attachmentId, Guid userId);
}

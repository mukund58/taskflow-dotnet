namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class TaskAttachmentService : ITaskAttachmentService
{
    private readonly AppDbContext _context;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public TaskAttachmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskAttachmentDto> CreateAttachmentAsync(Guid taskId, CreateTaskAttachmentDto attachmentDto, Guid userId)
    {
        // Verify task exists
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new KeyNotFoundException("Task not found");

        // Validate file size
        if (attachmentDto.FileSizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB");

        var attachment = new TaskAttachment
        {
            Id = Guid.NewGuid(),
            FileName = attachmentDto.FileName,
            FileSizeBytes = attachmentDto.FileSizeBytes,
            FileExtension = attachmentDto.FileExtension,
            StoragePath = attachmentDto.StoragePath,
            TaskId = taskId,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        return await MapToTaskAttachmentDtoAsync(attachment);
    }

    public async Task<List<TaskAttachmentDto>> GetTaskAttachmentsAsync(Guid taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new KeyNotFoundException("Task not found");

        var dtos = new List<TaskAttachmentDto>();
        var attachments = await _context.Attachments
            .Where(a => a.TaskId == taskId)
            .Include(a => a.UploadedByUser)
            .ToListAsync();

        foreach (var attachment in attachments)
        {
            dtos.Add(await MapToTaskAttachmentDtoAsync(attachment));
        }

        return dtos;
    }

    public async Task<TaskAttachmentDto> GetAttachmentByIdAsync(Guid attachmentId)
    {
        var attachment = await _context.Attachments
            .Include(a => a.UploadedByUser)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);

        if (attachment == null)
            throw new KeyNotFoundException("Attachment not found");

        return await MapToTaskAttachmentDtoAsync(attachment);
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId, Guid userId)
    {
        var attachment = await _context.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (attachment == null)
            throw new KeyNotFoundException("Attachment not found");

        // Verify the user is the one who uploaded the file or is an admin
        if (attachment.UploadedByUserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own attachments");

        attachment.IsDeleted = true;
        _context.Attachments.Update(attachment);
        await _context.SaveChangesAsync();
    }

    private async Task<TaskAttachmentDto> MapToTaskAttachmentDtoAsync(TaskAttachment attachment)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == attachment.UploadedByUserId);

        return new TaskAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FileSizeBytes = attachment.FileSizeBytes,
            FileExtension = attachment.FileExtension,
            StoragePath = attachment.StoragePath,
            TaskId = attachment.TaskId,
            UploadedByUserId = attachment.UploadedByUserId,
            UploadedByUserName = user?.Name ?? "Unknown",
            UploadedAt = attachment.UploadedAt
        };
    }
}

namespace Backend.Models.Entities;

public class TaskAttachment
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; } // Store file size for tracking

    public string FileExtension { get; set; } = string.Empty; // e.g., "pdf", "docx"

    public string StoragePath { get; set; } = string.Empty; // Path where file is stored

    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
}

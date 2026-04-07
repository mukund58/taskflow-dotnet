namespace Backend.Models.DTOs;

public class TaskAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileExtension { get; set; }
    public string StoragePath { get; set; }
    public Guid TaskId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CreateTaskAttachmentDto
{
    public string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileExtension { get; set; }
    public string StoragePath { get; set; }
}

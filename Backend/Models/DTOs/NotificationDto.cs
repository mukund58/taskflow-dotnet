namespace Backend.Models.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Message { get; set; }
    public string Type { get; set; } // TaskCreated, TaskAssigned, TaskCommented, CommentLiked, etc.
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? CommentId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class CreateNotificationDto
{
    public string Message { get; set; }
    public string Type { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? CommentId { get; set; }
}

public class MarkNotificationAsReadDto
{
    public bool IsRead { get; set; }
}

namespace Backend.Models.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty; // TaskCreated, TaskAssigned, TaskCommented, etc.

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public Guid? CommentId { get; set; } // If notification is about a comment

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}

namespace Backend.Models.Entities;

public class TaskComment
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false; // soft delete for comments
}

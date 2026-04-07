namespace Backend.Models.Entities;

public class TaskActivity
{
    public Guid Id { get; set; }

    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
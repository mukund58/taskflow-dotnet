namespace Backend.Models.Entities;

public class ChecklistItem
{
    public Guid Id { get; set; }

    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
    public int Position { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
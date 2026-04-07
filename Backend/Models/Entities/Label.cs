namespace Backend.Models.Entities;

public class Label
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#808080"; // Hex color code

    public string Description { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    // Navigation property for many-to-many
    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}

public class TaskLabel
{
    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public Guid LabelId { get; set; }
    public Label Label { get; set; } = null!;
}

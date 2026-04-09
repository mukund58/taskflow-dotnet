namespace Backend.Models.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;


    public string Status { get; set; } = "Todo"; // Todo, InProgress, Done
    public string Priority { get; set; } = "Medium";

    // public int CreatedById { get; set; }   //  ownership - disabled for now

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; } // Optional due date for tasks
    public bool IsDeleted { get; set; } = false; // soft delete
    public long RowVersion { get; set; } = 1;
}
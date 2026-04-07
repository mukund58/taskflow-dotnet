namespace Backend.Models.DTOs;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium"; // Low, Medium, High
}

public class UpdateTaskStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateTaskPriorityDto
{
    public string Priority { get; set; } = string.Empty;
}

public class AssignTaskDto
{
    public Guid UserId { get; set; }
}

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public DateTime? DueDate { get; set; }
}

public class CreateChecklistItemDto
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateChecklistItemCompletionDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public bool? IsCompleted { get; set; }
public class CreateCommentDto
{
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

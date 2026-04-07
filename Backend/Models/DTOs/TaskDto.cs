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

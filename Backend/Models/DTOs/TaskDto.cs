namespace Backend.Models.DTOs;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; }= string.Empty;
    public Guid ProjectId { get; set; }
}

public class UpdateTaskStatusDto
{
    public string Status { get; set; }= string.Empty;
}

public class AssignTaskDto
{
    public Guid UserId { get; set; }
}
 
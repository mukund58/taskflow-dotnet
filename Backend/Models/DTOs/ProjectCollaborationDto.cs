namespace Backend.Models.DTOs;

public class AddProjectMemberDto
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = "Member";
}

public class CreateProjectInvitationDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public int ExpiresInDays { get; set; } = 7;
}

public class ProjectMemberDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public bool IsProjectOwner { get; set; }
    public DateTime AddedAt { get; set; }
}

public class ProjectInvitationDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Status { get; set; } = "Pending";
    public Guid InvitedByUserId { get; set; }
    public string InvitedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

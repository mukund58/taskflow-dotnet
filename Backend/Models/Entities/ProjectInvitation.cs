namespace Backend.Models.Entities;

public class ProjectInvitation
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Member"; // Admin, Member

    public string Status { get; set; } = "Pending"; // Pending, Accepted, Revoked, Expired

    public Guid InvitedByUserId { get; set; }
    public User InvitedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}

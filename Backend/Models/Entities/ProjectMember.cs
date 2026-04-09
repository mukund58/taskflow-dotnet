namespace Backend.Models.Entities;

public class ProjectMember
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Role { get; set; } = "Member"; // Admin, Member

    public Guid AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

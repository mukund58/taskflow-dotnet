namespace Backend.Models.Entities;

public class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; }= string.Empty;

    public string Description { get; set; }= string.Empty;

    public DateTime? DueDate { get; set; }

    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
}

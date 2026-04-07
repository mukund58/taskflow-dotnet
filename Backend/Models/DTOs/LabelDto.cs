namespace Backend.Models.DTOs;

public class LabelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; } // Hex format like "#00FF00"
    public string Description { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLabelDto
{
    public string Name { get; set; }
    public string Color { get; set; } // Hex format
    public string Description { get; set; }
    public Guid ProjectId { get; set; }
}

public class UpdateLabelDto
{
    public string Name { get; set; }
    public string Color { get; set; }
    public string Description { get; set; }
}

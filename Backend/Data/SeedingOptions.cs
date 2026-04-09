namespace Backend.Data;

public class SeedingOptions
{
    public bool Enabled { get; set; } = false;
    public int Users { get; set; } = 5;
    public int Projects { get; set; } = 2;
    public int Tasks { get; set; } = 20;
    public string DefaultPassword { get; set; } = "Password123!";
}

namespace Backend.Models.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; }= string.Empty;

    public string PasswordHash { get; set; }= string.Empty;

    public string Role { get; set; } = "User";

    public string Theme { get; set; } = "system";

    public string Language { get; set; } = "en";

    public string Timezone { get; set; } = "UTC";

    public bool EmailNotificationsEnabled { get; set; } = true;

    public bool PushNotificationsEnabled { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }
}

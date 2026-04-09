namespace Backend.Models.DTOs;

public class UserSettingsDto
{
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
}

public class UpdateUserSettingsDto
{
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
}

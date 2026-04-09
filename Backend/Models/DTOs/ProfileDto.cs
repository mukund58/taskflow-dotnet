namespace Backend.Models.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
}

public class UpdateUserProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class DeleteAccountDto
{
    public string CurrentPassword { get; set; } = string.Empty;
}

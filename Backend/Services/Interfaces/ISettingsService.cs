namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface ISettingsService
{
    Task<UserSettingsDto> GetCurrentUserSettingsAsync(Guid userId);
    Task<UserSettingsDto> UpdateCurrentUserSettingsAsync(Guid userId, UpdateUserSettingsDto dto);
}

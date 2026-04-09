namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface IProfileService
{
    Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateCurrentUserProfileAsync(Guid userId, UpdateUserProfileDto dto);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task DeleteAccountAsync(Guid userId, DeleteAccountDto dto);
}

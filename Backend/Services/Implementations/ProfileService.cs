namespace Backend.Services.Implementations;

using BCrypt.Net;
using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _context;

    public ProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId)
    {
        var user = await GetActiveUserOrThrowAsync(userId, asNoTracking: true);
        return MapToUserProfile(user);
    }

    public async Task<UserProfileDto> UpdateCurrentUserProfileAsync(Guid userId, UpdateUserProfileDto dto)
    {
        var user = await GetActiveUserOrThrowAsync(userId, asNoTracking: false);

        var normalizedEmail = dto.Email.Trim();
        var emailInUse = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != userId &&
                !u.IsDeleted &&
                u.Email.ToLower() == normalizedEmail.ToLower());

        if (emailInUse)
            throw new InvalidOperationException("Email already in use");

        user.Name = dto.Name.Trim();
        user.Email = normalizedEmail;
        user.Theme = dto.Theme;
        user.Language = dto.Language.Trim();
        user.Timezone = dto.Timezone.Trim();
        user.EmailNotificationsEnabled = dto.EmailNotificationsEnabled;
        user.PushNotificationsEnabled = dto.PushNotificationsEnabled;

        await _context.SaveChangesAsync();
        return MapToUserProfile(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await GetActiveUserOrThrowAsync(userId, asNoTracking: false);

        if (!BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountDto dto)
    {
        var user = await GetActiveUserOrThrowAsync(userId, asNoTracking: false);

        if (!BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.Name = "Deleted User";
        user.Email = $"deleted-{user.Id:N}@deleted.local";
        user.PasswordHash = BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

        await _context.SaveChangesAsync();
    }

    private async Task<User> GetActiveUserOrThrowAsync(Guid userId, bool asNoTracking)
    {
        var query = _context.Users.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        return user;
    }

    private static UserProfileDto MapToUserProfile(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Theme = user.Theme,
            Language = user.Language,
            Timezone = user.Timezone,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PushNotificationsEnabled = user.PushNotificationsEnabled
        };
    }
}

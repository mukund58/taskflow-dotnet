namespace Backend.Services.Implementations;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSettingsDto> GetCurrentUserSettingsAsync(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        return new UserSettingsDto
        {
            Theme = user.Theme,
            Language = user.Language,
            Timezone = user.Timezone,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PushNotificationsEnabled = user.PushNotificationsEnabled
        };
    }

    public async Task<UserSettingsDto> UpdateCurrentUserSettingsAsync(Guid userId, UpdateUserSettingsDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.Theme = dto.Theme;
        user.Language = dto.Language;
        user.Timezone = dto.Timezone;
        user.EmailNotificationsEnabled = dto.EmailNotificationsEnabled;
        user.PushNotificationsEnabled = dto.PushNotificationsEnabled;

        await _context.SaveChangesAsync();

        return new UserSettingsDto
        {
            Theme = user.Theme,
            Language = user.Language,
            Timezone = user.Timezone,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PushNotificationsEnabled = user.PushNotificationsEnabled
        };
    }
}

namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto notificationDto)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Message = notificationDto.Message,
            Type = notificationDto.Type,
            UserId = notificationDto.UserId,
            TaskId = notificationDto.TaskId,
            CommentId = notificationDto.CommentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return MapToNotificationDto(notification);
    }

    public async Task<(List<NotificationDto>, int)> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var totalCount = await _context.Notifications
            .Where(n => n.UserId == userId)
            .CountAsync();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => MapToNotificationDto(n))
            .ToListAsync();

        return (notifications, totalCount);
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => MapToNotificationDto(n))
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            throw new KeyNotFoundException("Notification not found");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkMultipleAsReadAsync(List<Guid> notificationIds, Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => notificationIds.Contains(n.Id) && n.UserId == userId)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        _context.Notifications.UpdateRange(notifications);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            throw new KeyNotFoundException("Notification not found");

        notification.IsDeleted = true;
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAllNotificationsAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsDeleted = true;
        }

        _context.Notifications.UpdateRange(notifications);
        await _context.SaveChangesAsync();
    }

    private NotificationDto MapToNotificationDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Message = notification.Message,
            Type = notification.Type,
            UserId = notification.UserId,
            TaskId = notification.TaskId,
            CommentId = notification.CommentId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}

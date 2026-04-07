namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;

public interface INotificationService
{
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto notificationDto);
    Task<(List<NotificationDto>, int)> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkMultipleAsReadAsync(List<Guid> notificationIds, Guid userId);
    Task DeleteNotificationAsync(Guid notificationId, Guid userId);
    Task DeleteAllNotificationsAsync(Guid userId);
}

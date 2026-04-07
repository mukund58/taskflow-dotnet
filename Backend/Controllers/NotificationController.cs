namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get paginated notifications for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (notifications, total) = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);

            var totalPages = (total + pageSize - 1) / pageSize;
            var response = new
            {
                Items = notifications,
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(ApiResponseDto<object>.Ok(response, "Notifications retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get unread notifications for the current user
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Ok(ApiResponseDto<List<NotificationDto>>.Ok(notifications, "Unread notifications retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Mark a single notification as read
    /// </summary>
    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(notificationId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "Notification marked as read"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Mark multiple notifications as read
    /// </summary>
    [HttpPut("read-multiple")]
    public async Task<IActionResult> MarkMultipleAsRead([FromBody] List<Guid> notificationIds)
    {
        try
        {
            if (!notificationIds.Any())
                return BadRequest(ApiResponseDto<object>.Fail("No notification IDs provided"));

            var userId = GetCurrentUserId();
            await _notificationService.MarkMultipleAsReadAsync(notificationIds, userId);
            return Ok(ApiResponseDto<object>.Ok(null, $"Marked {notificationIds.Count} notifications as read"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.DeleteNotificationAsync(notificationId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "Notification deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Delete all notifications for the current user
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAllNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.DeleteAllNotificationsAsync(userId);
            return Ok(ApiResponseDto<object>.Ok(null, "All notifications deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim?.Value ?? throw new UnauthorizedAccessException("User ID not found in token"));
    }
}

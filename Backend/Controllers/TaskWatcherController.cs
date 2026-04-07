namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Route("api/tasks/{taskId}/watchers")]
[Authorize]
public class TaskWatcherController : ControllerBase
{
    private readonly ITaskWatcherService _watcherService;
    private readonly ITaskService _taskService;

    public TaskWatcherController(ITaskWatcherService watcherService, ITaskService taskService)
    {
        _watcherService = watcherService;
        _taskService = taskService;
    }

    /// <summary>
    /// Add the current user as a watcher to a task
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> AddWatcher(Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _watcherService.AddWatcherAsync(taskId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "You are now watching this task"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Add a specific user as a watcher to a task (requires ProjectWrite access)
    /// </summary>
    [HttpPost("add-user")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> AddUserAsWatcher(Guid taskId, [FromQuery] Guid userId)
    {
        try
        {
            await _watcherService.AddWatcherAsync(taskId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "User is now watching this task"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Remove the current user as a watcher from a task
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> RemoveWatcher(Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _watcherService.RemoveWatcherAsync(taskId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "You are no longer watching this task"));
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
    /// Remove a specific user as a watcher from a task
    /// </summary>
    [HttpDelete("remove-user")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> RemoveUserAsWatcher(Guid taskId, [FromQuery] Guid userId)
    {
        try
        {
            await _watcherService.RemoveWatcherAsync(taskId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "User is no longer watching this task"));
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
    /// Get all watchers for a task
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetTaskWatchers(Guid taskId)
    {
        try
        {
            var watchers = await _watcherService.GetTaskWatchersAsync(taskId);
            return Ok(ApiResponseDto<object>.Ok(watchers, "Task watchers retrieved successfully"));
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
    /// Get all tasks watched by the current user
    /// </summary>
    [HttpGet("my-watched-tasks")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetMyWatchedTasks()
    {
        try
        {
            var userId = GetCurrentUserId();
            var watchedTasks = await _watcherService.GetUserWatchedTasksAsync(userId);
            return Ok(ApiResponseDto<object>.Ok(watchedTasks, "Your watched tasks retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Check if the current user is watching a task
    /// </summary>
    [HttpGet("is-watching")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> IsWatching(Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isWatching = await _watcherService.IsWatchingAsync(taskId, userId);
            return Ok(ApiResponseDto<object>.Ok(new { IsWatching = isWatching }, "Watcher status retrieved"));
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

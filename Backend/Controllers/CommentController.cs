namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Route("api/tasks/{taskId}/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _service;
    private readonly ITaskService _taskService;

    public CommentController(ICommentService service, ITaskService taskService)
    {
        _service = service;
        _taskService = taskService;
    }

    /// <summary>
    /// Add a comment to a task
    /// Only authenticated users can add comments (will be extended with permissions)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> AddComment(Guid taskId, [FromBody] CreateCommentDto dto)
    {
        try
        {
            // Verify user has access to the task first
            await EnsureTaskAccessAsync(taskId);

            var userId = GetCurrentUserId();
            var comment = await _service.AddCommentAsync(taskId, userId, dto);

            return Ok(ApiResponseDto<CommentDto>.Ok(comment, "Comment added successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponseDto<object>.Fail("Task not found"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Get all comments for a task (including deleted tasks per requirements)
    /// Only authenticated users can view comments
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetComments(Guid taskId)
    {
        try
        {
            var comments = await _service.GetTaskCommentsAsync(taskId);
            return Ok(ApiResponseDto<List<CommentDto>>.Ok(comments, "Comments retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Update a comment (only author can edit)
    /// </summary>
    [HttpPut("{commentId}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> UpdateComment(Guid taskId, Guid commentId, [FromBody] UpdateCommentDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var comment = await _service.UpdateCommentAsync(commentId, userId, dto);

            return Ok(ApiResponseDto<CommentDto>.Ok(comment, "Comment updated successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponseDto<object>.Fail("Comment not found"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Delete a comment (only author can delete)
    /// </summary>
    [HttpDelete("{commentId}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> DeleteComment(Guid taskId, Guid commentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _service.DeleteCommentAsync(commentId, userId);

            return Ok(ApiResponseDto<object>.Ok(null, "Comment deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponseDto<object>.Fail("Comment not found"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user context");

        return userId;
    }

    private bool HasElevatedAccess()
    {
        return User.IsInRole("Admin") || User.IsInRole("Manager");
    }

    private async Task<Backend.Models.Entities.TaskItem> EnsureTaskAccessAsync(Guid taskId)
    {
        var task = await _taskService.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();

        if (task.AssignedUserId != currentUserId)
            throw new UnauthorizedAccessException("You can only access your own tasks");

        return task;
    }
}

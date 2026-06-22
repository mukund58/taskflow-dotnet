namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks/{taskId}/comments")]
[Authorize]
public class CommentController : TaskAccessController
{
    private readonly ICommentService _service;

    public CommentController(ICommentService service, ITaskService taskService, IProjectService projectService)
        : base(taskService, projectService)
    {
        _service = service;
    }

    /// <summary>
    /// Add a comment to a task
    /// Only authenticated users can add comments (will be extended with permissions)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> AddComment(Guid taskId, [FromBody] CreateCommentDto dto)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);

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
            await EnsureTaskReadAccessAsync(taskId);
            var comments = await _service.GetTaskCommentsAsync(taskId);
            return Ok(ApiResponseDto<List<CommentDto>>.Ok(comments, "Comments retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Update a comment (only author can edit)
    /// </summary>
    [HttpPut("{commentId}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> UpdateComment(Guid taskId, Guid commentId, [FromBody] UpdateCommentDto dto)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            var userId = GetCurrentUserId();
            var comment = await _service.UpdateCommentAsync(taskId, commentId, userId, dto);

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
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> DeleteComment(Guid taskId, Guid commentId)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            var userId = GetCurrentUserId();
            await _service.DeleteCommentAsync(taskId, commentId, userId);

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
}

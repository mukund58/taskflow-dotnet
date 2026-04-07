namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Route("api/tasks/{taskId}/attachments")]
[Authorize]
public class TaskAttachmentController : ControllerBase
{
    private readonly ITaskAttachmentService _attachmentService;
    private readonly ITaskService _taskService;

    public TaskAttachmentController(ITaskAttachmentService attachmentService, ITaskService taskService)
    {
        _attachmentService = attachmentService;
        _taskService = taskService;
    }

    /// <summary>
    /// Get all attachments for a task
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetAttachments(Guid taskId)
    {
        try
        {
            var attachments = await _attachmentService.GetTaskAttachmentsAsync(taskId);
            return Ok(ApiResponseDto<List<TaskAttachmentDto>>.Ok(attachments, "Attachments retrieved successfully"));
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
    /// Get a specific attachment by ID
    /// </summary>
    [HttpGet("{attachmentId}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetAttachmentById(Guid taskId, Guid attachmentId)
    {
        try
        {
            var attachment = await _attachmentService.GetAttachmentByIdAsync(attachmentId);
            if (attachment.TaskId != taskId)
                return NotFound(ApiResponseDto<object>.Fail("Attachment not found for this task"));

            return Ok(ApiResponseDto<TaskAttachmentDto>.Ok(attachment, "Attachment retrieved successfully"));
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
    /// Create a new attachment for a task
    /// For now, accepts metadata. In production, would handle multipart/form-data for actual file uploads
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> CreateAttachment(Guid taskId, [FromBody] CreateTaskAttachmentDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var attachment = await _attachmentService.CreateAttachmentAsync(taskId, dto, userId);
            return CreatedAtAction(nameof(GetAttachmentById), new { taskId, attachmentId = attachment.Id },
                ApiResponseDto<TaskAttachmentDto>.Ok(attachment, "Attachment created successfully"));
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
    /// Delete an attachment (only the uploader or admin can delete)
    /// </summary>
    [HttpDelete("{attachmentId}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> DeleteAttachment(Guid taskId, Guid attachmentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _attachmentService.DeleteAttachmentAsync(attachmentId, userId);
            return Ok(ApiResponseDto<object>.Ok(null, "Attachment deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
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

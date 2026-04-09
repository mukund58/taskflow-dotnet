namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.StaticFiles;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks/{taskId}/attachments")]
[Route("api/tasks/{taskId}/attachments")]
[Authorize]
public class TaskAttachmentController : ControllerBase
{
    private readonly ITaskAttachmentService _attachmentService;
    private readonly ITaskService _taskService;
    private readonly IProjectService? _projectService;
    private readonly IWebHostEnvironment? _hostEnvironment;
    private const long MaxUploadFileSizeBytes = 10 * 1024 * 1024;

    public TaskAttachmentController(ITaskAttachmentService attachmentService, ITaskService taskService)
        : this(attachmentService, taskService, null, null)
    {
    }

    [ActivatorUtilitiesConstructor]
    public TaskAttachmentController(
        ITaskAttachmentService attachmentService,
        ITaskService taskService,
        IProjectService? projectService,
        IWebHostEnvironment? hostEnvironment)
    {
        _attachmentService = attachmentService;
        _taskService = taskService;
        _projectService = projectService;
        _hostEnvironment = hostEnvironment;
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
            await EnsureTaskReadAccessAsync(taskId);
            var attachments = await _attachmentService.GetTaskAttachmentsAsync(taskId);
            return Ok(ApiResponseDto<List<TaskAttachmentDto>>.Ok(attachments, "Attachments retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
            await EnsureTaskReadAccessAsync(taskId);
            var attachment = await _attachmentService.GetAttachmentByIdAsync(attachmentId);
            if (attachment.TaskId != taskId)
                return NotFound(ApiResponseDto<object>.Fail("Attachment not found for this task"));

            return Ok(ApiResponseDto<TaskAttachmentDto>.Ok(attachment, "Attachment retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Download an attachment file stream by attachment ID
    /// </summary>
    [HttpGet("{attachmentId}/download")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> DownloadAttachment(Guid taskId, Guid attachmentId)
    {
        try
        {
            await EnsureTaskReadAccessAsync(taskId);
            var attachment = await _attachmentService.GetAttachmentByIdAsync(attachmentId);

            if (attachment.TaskId != taskId)
                return NotFound(ApiResponseDto<object>.Fail("Attachment not found for this task"));

            var absolutePath = ResolveAttachmentAbsolutePath(attachment.StoragePath);
            if (string.IsNullOrWhiteSpace(absolutePath) || !System.IO.File.Exists(absolutePath))
                return NotFound(ApiResponseDto<object>.Fail("Attachment file not found"));

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(attachment.FileName, out var contentType))
                contentType = "application/octet-stream";

            var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, contentType, attachment.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
            await EnsureTaskWriteAccessAsync(taskId);
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Upload a file and create attachment metadata
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> UploadAttachment(Guid taskId, [FromForm] IFormFile file)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);

            if (file == null || file.Length == 0)
                return BadRequest(ApiResponseDto<object>.Fail("File is required"));

            if (file.Length > MaxUploadFileSizeBytes)
                return BadRequest(ApiResponseDto<object>.Fail($"File size exceeds maximum allowed size of {MaxUploadFileSizeBytes / 1024 / 1024} MB"));

            var originalFileName = Path.GetFileName(file.FileName);
            var fileExtension = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid():N}{fileExtension}";

            var uploadDirectory = GetTaskAttachmentDirectoryPath(taskId);
            Directory.CreateDirectory(uploadDirectory);

            var absolutePath = Path.Combine(uploadDirectory, storedFileName);

            await using (var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            var userId = GetCurrentUserId();
            var created = await _attachmentService.CreateAttachmentAsync(taskId, new CreateTaskAttachmentDto
            {
                FileName = originalFileName,
                FileSizeBytes = file.Length,
                FileExtension = fileExtension,
                StoragePath = absolutePath
            }, userId);

            return CreatedAtAction(nameof(GetAttachmentById), new { taskId, attachmentId = created.Id },
                ApiResponseDto<TaskAttachmentDto>.Ok(created, "Attachment uploaded successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
            await EnsureTaskWriteAccessAsync(taskId);

            var attachment = await _attachmentService.GetAttachmentByIdAsync(attachmentId);
            if (attachment.TaskId != taskId)
                return NotFound(ApiResponseDto<object>.Fail("Attachment not found for this task"));

            var userId = GetCurrentUserId();
            await _attachmentService.DeleteAttachmentAsync(attachmentId, userId);
            TryDeletePhysicalFile(attachment.StoragePath);
            return Ok(ApiResponseDto<object>.Ok(null, "Attachment deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponseDto<object>.Fail($"Internal server error: {ex.Message}"));
        }
    }

    private bool HasElevatedAccess()
    {
        return User.IsInRole("Admin") || User.IsInRole("Manager");
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user context");

        return userId;
    }

    private string GetTaskAttachmentDirectoryPath(Guid taskId)
    {
        var basePath = _hostEnvironment?.ContentRootPath ?? Directory.GetCurrentDirectory();
        return Path.Combine(basePath, "Uploads", "task-attachments", taskId.ToString("N"));
    }

    private string? ResolveAttachmentAbsolutePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return null;

        if (Uri.TryCreate(storagePath, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
            return null;

        if (Path.IsPathRooted(storagePath))
            return storagePath;

        var basePath = _hostEnvironment?.ContentRootPath ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(basePath, storagePath));
    }

    private void TryDeletePhysicalFile(string storagePath)
    {
        try
        {
            var absolutePath = ResolveAttachmentAbsolutePath(storagePath);
            if (!string.IsNullOrWhiteSpace(absolutePath) && System.IO.File.Exists(absolutePath))
                System.IO.File.Delete(absolutePath);
        }
        catch
        {
            // Physical file cleanup should not fail the API flow.
        }
    }

    private async Task<Backend.Models.Entities.TaskItem> EnsureTaskReadAccessAsync(Guid taskId)
    {
        var task = await _taskService.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();

        if (_projectService != null)
        {
            var canRead = await _projectService.HasReadAccess(task.ProjectId, currentUserId, elevatedAccess: false);

            if (!canRead)
                throw new UnauthorizedAccessException("You do not have read access to this task");

            return task;
        }

        if (task.AssignedUserId != currentUserId)
            throw new UnauthorizedAccessException("You can only access your own tasks");

        return task;
    }

    private async Task<Backend.Models.Entities.TaskItem> EnsureTaskWriteAccessAsync(Guid taskId)
    {
        var task = await _taskService.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();

        if (_projectService != null)
        {
            var canWrite = await _projectService.HasWriteAccess(task.ProjectId, currentUserId, elevatedAccess: false);

            if (!canWrite)
                throw new UnauthorizedAccessException("You do not have write access to this task");

            return task;
        }

        if (task.AssignedUserId != currentUserId)
            throw new UnauthorizedAccessException("You can only access your own tasks");

        return task;
    }
}

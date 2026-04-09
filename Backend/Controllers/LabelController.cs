namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId}/labels")]
[Route("api/projects/{projectId}/labels")]
[Authorize]
public class LabelController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    public LabelController(ILabelService labelService, IProjectService projectService, ITaskService taskService)
    {
        _labelService = labelService;
        _projectService = projectService;
        _taskService = taskService;
    }

    /// <summary>
    /// Get all labels for a project
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ProjectRead")]
    public async Task<IActionResult> GetProjectLabels(Guid projectId)
    {
        try
        {
            await EnsureProjectReadAccessAsync(projectId);
            var labels = await _labelService.GetLabelsByProjectAsync(projectId);
            return Ok(ApiResponseDto<List<LabelDto>>.Ok(labels, "Labels retrieved successfully"));
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
    /// Create a new label in a project
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> CreateLabel(Guid projectId, [FromBody] CreateLabelDto dto)
    {
        try
        {
            await EnsureProjectWriteAccessAsync(projectId);
            var label = await _labelService.CreateLabelAsync(dto, projectId);
            return CreatedAtAction(nameof(GetLabelById), new { projectId, labelId = label.Id },
                ApiResponseDto<LabelDto>.Ok(label, "Label created successfully"));
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
    /// Get a specific label by ID
    /// </summary>
    [HttpGet("{labelId}")]
    [Authorize(Policy = "ProjectRead")]
    public async Task<IActionResult> GetLabelById(Guid projectId, Guid labelId)
    {
        try
        {
            await EnsureProjectReadAccessAsync(projectId);
            var label = await _labelService.GetLabelByIdAsync(labelId);
            if (label.ProjectId != projectId)
                return NotFound(ApiResponseDto<object>.Fail("Label not found in this project"));

            return Ok(ApiResponseDto<LabelDto>.Ok(label, "Label retrieved successfully"));
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
    /// Update a label
    /// </summary>
    [HttpPut("{labelId}")]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> UpdateLabel(Guid projectId, Guid labelId, [FromBody] UpdateLabelDto dto)
    {
        try
        {
            await EnsureProjectWriteAccessAsync(projectId);
            var label = await _labelService.UpdateLabelAsync(labelId, dto);
            if (label.ProjectId != projectId)
                return NotFound(ApiResponseDto<object>.Fail("Label not found in this project"));

            return Ok(ApiResponseDto<LabelDto>.Ok(label, "Label updated successfully"));
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
    /// Delete a label
    /// </summary>
    [HttpDelete("{labelId}")]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> DeleteLabel(Guid projectId, Guid labelId)
    {
        try
        {
            await EnsureProjectWriteAccessAsync(projectId);
            var label = await _labelService.GetLabelByIdAsync(labelId);
            if (label.ProjectId != projectId)
                return NotFound(ApiResponseDto<object>.Fail("Label not found in this project"));

            await _labelService.DeleteLabelAsync(labelId);
            return Ok(ApiResponseDto<object>.Ok(null, "Label deleted successfully"));
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
    /// Add a label to a task
    /// </summary>
    [HttpPost("tasks/{taskId}/assign")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> AddLabelToTask(Guid projectId, Guid taskId, [FromQuery] Guid labelId)
    {
        try
        {
            await EnsureProjectWriteAccessAsync(projectId);
            await EnsureTaskAccessInProjectAsync(projectId, taskId);
            await _labelService.AddLabelToTaskAsync(taskId, labelId);
            return Ok(ApiResponseDto<object>.Ok(null, "Label added to task successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
    /// Remove a label from a task
    /// </summary>
    [HttpDelete("tasks/{taskId}/remove")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> RemoveLabelFromTask(Guid projectId, Guid taskId, [FromQuery] Guid labelId)
    {
        try
        {
            await EnsureProjectWriteAccessAsync(projectId);
            await EnsureTaskAccessInProjectAsync(projectId, taskId);
            await _labelService.RemoveLabelFromTaskAsync(taskId, labelId);
            return Ok(ApiResponseDto<object>.Ok(null, "Label removed from task successfully"));
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
    /// Get all labels for a specific task
    /// </summary>
    [HttpGet("tasks/{taskId}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetTaskLabels(Guid projectId, Guid taskId)
    {
        try
        {
            await EnsureProjectReadAccessAsync(projectId);
            await EnsureTaskAccessInProjectAsync(projectId, taskId);
            var labels = await _labelService.GetTaskLabelsAsync(taskId);
            return Ok(ApiResponseDto<List<LabelDto>>.Ok(labels, "Task labels retrieved successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    private async Task<Backend.Models.Entities.TaskItem> EnsureTaskAccessInProjectAsync(Guid projectId, Guid taskId)
    {
        var task = await _taskService.GetById(taskId);

        if (task.ProjectId != projectId)
            throw new KeyNotFoundException("Task not found in this project");

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();
        if (task.AssignedUserId != currentUserId)
            throw new UnauthorizedAccessException("You can only access your own tasks");

        return task;
    }

    private async Task EnsureProjectReadAccessAsync(Guid projectId)
    {
        var currentUserId = GetCurrentUserId();

        if (!await _projectService.ProjectExists(projectId))
            throw new KeyNotFoundException("Project not found");

        var canRead = await _projectService.HasReadAccess(projectId, currentUserId, HasElevatedAccess());
        if (!canRead)
            throw new UnauthorizedAccessException("You do not have access to this project");
    }

    private async Task EnsureProjectWriteAccessAsync(Guid projectId)
    {
        var currentUserId = GetCurrentUserId();

        if (!await _projectService.ProjectExists(projectId))
            throw new KeyNotFoundException("Project not found");

        var canWrite = await _projectService.HasWriteAccess(projectId, currentUserId, HasElevatedAccess());
        if (!canWrite)
            throw new UnauthorizedAccessException("You do not have write access to this project");
    }
}

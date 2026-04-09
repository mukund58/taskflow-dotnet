namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks/{taskId}/checklist")]
[Route("api/tasks/{taskId}/checklist")]
[Authorize]
public class ChecklistController : ControllerBase
{
    private readonly IChecklistService _service;
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;

    public ChecklistController(IChecklistService service, ITaskService taskService, IProjectService projectService)
    {
        _service = service;
        _taskService = taskService;
        _projectService = projectService;
    }

    /// <summary>
    /// Add a checklist item to a task
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> AddChecklistItem(Guid taskId, [FromBody] CreateChecklistItemDto dto)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            var item = await _service.AddChecklistItemAsync(taskId, dto);
            return Ok(ApiResponseDto<ChecklistItemDto>.Ok(item, "Checklist item added"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get all checklist items for a task
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetTaskChecklist(Guid taskId)
    {
        try
        {
            await EnsureTaskReadAccessAsync(taskId);
            var items = await _service.GetTaskChecklistAsync(taskId);
            return Ok(ApiResponseDto<List<ChecklistItemDto>>.Ok(items, "Checklist retrieved"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get checklist summary with completion percentage
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetChecklistSummary(Guid taskId)
    {
        try
        {
            await EnsureTaskReadAccessAsync(taskId);
            var summary = await _service.GetChecklistSummaryAsync(taskId);
            return Ok(ApiResponseDto<TaskChecklistSummaryDto>.Ok(summary, "Checklist summary retrieved"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Update a checklist item
    /// </summary>
    [HttpPut("{checklistItemId}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> UpdateChecklistItem(Guid taskId, Guid checklistItemId, [FromBody] UpdateChecklistItemDto dto)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            var item = await _service.UpdateChecklistItemAsync(checklistItemId, dto);
            return Ok(ApiResponseDto<ChecklistItemDto>.Ok(item, "Checklist item updated"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Toggle completion status of a checklist item
    /// </summary>
    [HttpPatch("{checklistItemId}/toggle")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> ToggleCompletion(Guid taskId, Guid checklistItemId)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            var item = await _service.ToggleCompletionAsync(checklistItemId);
            return Ok(ApiResponseDto<ChecklistItemDto>.Ok(item, "Checklist item toggled"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Delete a checklist item
    /// </summary>
    [HttpDelete("{checklistItemId}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> DeleteChecklistItem(Guid taskId, Guid checklistItemId)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            await _service.DeleteChecklistItemAsync(checklistItemId);
            return Ok(ApiResponseDto<object>.Ok(null, "Checklist item deleted"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Reorder checklist items
    /// </summary>
    [HttpPost("reorder")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> ReorderChecklist(Guid taskId, [FromBody] List<Guid> itemIds)
    {
        try
        {
            await EnsureTaskWriteAccessAsync(taskId);
            await _service.ReorderChecklistAsync(taskId, itemIds);
            return Ok(ApiResponseDto<object>.Ok(null, "Checklist reordered"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<object>.Fail(ex.Message));
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

    private async Task<Backend.Models.Entities.TaskItem> EnsureTaskReadAccessAsync(Guid taskId)
    {
        var task = await _taskService.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();
        var canRead = await _projectService.HasReadAccess(task.ProjectId, currentUserId, elevatedAccess: false);

        if (!canRead)
            throw new UnauthorizedAccessException("You do not have read access to this task");

        return task;
    }

    private async Task<Backend.Models.Entities.TaskItem> EnsureTaskWriteAccessAsync(Guid taskId)
    {
        var task = await _taskService.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();
        var canWrite = await _projectService.HasWriteAccess(task.ProjectId, currentUserId, elevatedAccess: false);

        if (!canWrite)
            throw new UnauthorizedAccessException("You do not have write access to this task");

        return task;
    }
}

namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using Backend.Models.Entities;
using System.Security.Claims;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks")]
[Route("api/tasks")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;
    private readonly IProjectService _projectService;

    public TaskController(ITaskService service, IProjectService projectService)
    {
        _service = service;
        _projectService = projectService;
    }

    [HttpPost]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var currentUserId = GetCurrentUserId();

        if (!await _projectService.ProjectExists(dto.ProjectId))
            return NotFound(ApiResponseDto<object>.Fail("Project not found"));

        var canWrite = await _projectService.HasWriteAccess(dto.ProjectId, currentUserId, HasElevatedAccess());
        if (!canWrite)
            return Forbid();

        var task = await _service.Create(dto, currentUserId);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task created"));
    }

    [HttpGet]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        List<Guid>? projectIds = null;

        if (!HasElevatedAccess())
        {
            var currentUserId = GetCurrentUserId();
            var accessibleProjects = await _projectService.GetAccessibleProjects(currentUserId, elevatedAccess: false);
            projectIds = accessibleProjects.Select(project => project.Id).ToList();
        }

        var query = new TaskQueryDto
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            AssignedTo = assignedTo,
            ProjectIds = projectIds,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _service.GetAllPaginatedAsync(query);
        return Ok(new ApiResponseDto<PaginatedResponseDto<Backend.Models.Entities.TaskItem>>
        {
            Success = true,
            Data = result,
            Message = "Tasks retrieved"
        });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        await EnsureTaskWriteAccessAsync(id);
        var task = await _service.Update(id, dto, GetCurrentUserId());
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task updated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await EnsureTaskWriteAccessAsync(id);
        await _service.Delete(id);
        return Ok(ApiResponseDto<object>.Ok(null, "Task deleted"));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await EnsureTaskReadAccessAsync(id);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task retrieved"));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
    {
        await EnsureTaskWriteAccessAsync(id);
        var task = await _service.UpdateStatus(id, dto.Status, GetCurrentUserId(), dto.RowVersion);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task status updated"));
    }

    [HttpPatch("{id}/assign")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTaskDto dto)
    {
        await EnsureTaskWriteAccessAsync(id);
        var task = await _service.Assign(id, dto.UserId, GetCurrentUserId(), dto.RowVersion);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task assigned"));
    }

    [HttpGet("{id}/activity")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetActivity(Guid id)
    {
        await EnsureTaskReadAccessAsync(id);
        var activity = await _service.GetActivity(id);
        return Ok(ApiResponseDto<List<TaskActivity>>.Ok(activity, "Task activity retrieved"));
    }

    [HttpPatch("{id}/checklist/{checklistItemId}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> UpdateChecklistItemCompletion(Guid id, Guid checklistItemId, [FromBody] UpdateChecklistItemCompletionDto dto)
    {
        await EnsureTaskWriteAccessAsync(id);
        var item = await _service.UpdateChecklistItemCompletion(id, checklistItemId, dto.IsCompleted ?? false);
        return Ok(ApiResponseDto<ChecklistItem>.Ok(item, "Checklist item updated"));
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

    private async Task<TaskItem> EnsureTaskReadAccessAsync(Guid taskId)
    {
        var task = await _service.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();
        var canRead = await _projectService.HasReadAccess(task.ProjectId, currentUserId, elevatedAccess: false);

        if (!canRead)
            throw new UnauthorizedAccessException("You do not have read access to this task");

        return task;
    }

    private async Task<TaskItem> EnsureTaskWriteAccessAsync(Guid taskId)
    {
        var task = await _service.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();
        var canWrite = await _projectService.HasWriteAccess(task.ProjectId, currentUserId, elevatedAccess: false);

        if (!canWrite)
            throw new UnauthorizedAccessException("You do not have write access to this task");

        return task;
    }
}

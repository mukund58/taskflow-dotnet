namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using Backend.Models.Entities;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks")]
[Authorize]
public class TaskController : TaskAccessController
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service, IProjectService projectService)
        : base(service, projectService)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var currentUserId = GetCurrentUserId();

        if (!await ProjectService.ProjectExists(dto.ProjectId))
            return NotFound(ApiResponseDto<object>.Fail("Project not found"));

        var canWrite = await ProjectService.HasWriteAccess(dto.ProjectId, currentUserId, HasElevatedAccess());
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
            var accessibleProjects = await ProjectService.GetAccessibleProjects(currentUserId, elevatedAccess: false);
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
}

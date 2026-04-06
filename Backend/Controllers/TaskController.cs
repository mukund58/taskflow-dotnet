namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using Backend.Models.Entities;
using System.Security.Claims;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var task = await _service.Create(dto);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task created"));
    }

    [HttpGet]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? assignedTo)
    {
        assignedTo = EnforceOwnershipFilter(assignedTo);
        var tasks = await _service.GetAll(status, assignedTo);
        return Ok(ApiResponseDto<List<Backend.Models.Entities.TaskItem>>.Ok(tasks, "Tasks retrieved"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        await EnsureTaskAccessAsync(id);
        var task = await _service.Update(id, dto);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task updated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await EnsureTaskAccessAsync(id);
        await _service.Delete(id);
        return Ok(ApiResponseDto<object>.Ok(null, "Task deleted"));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await EnsureTaskAccessAsync(id);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task retrieved"));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
    {
        await EnsureTaskAccessAsync(id);
        var task = await _service.UpdateStatus(id, dto.Status);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task status updated"));
    }

    [HttpPatch("{id}/assign")]
    [Authorize(Policy = "TaskWrite")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTaskDto dto)
    {
        await EnsureTaskAccessAsync(id);
        var task = await _service.Assign(id, dto.UserId);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task assigned"));
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

    private Guid? EnforceOwnershipFilter(Guid? assignedTo)
    {
        if (HasElevatedAccess())
            return assignedTo;

        return GetCurrentUserId();
    }

    private async Task<TaskItem> EnsureTaskAccessAsync(Guid taskId)
    {
        var task = await _service.GetById(taskId);

        if (HasElevatedAccess())
            return task;

        var currentUserId = GetCurrentUserId();

        if (task.AssignedUserId != currentUserId)
            throw new UnauthorizedAccessException("You can only access your own tasks");

        return task;
    }
}

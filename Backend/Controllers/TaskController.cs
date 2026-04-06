namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;

[ApiController]
[Route("api/tasks")]
// [Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var task = await _service.Create(dto);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task created"));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? assignedTo)
    {
        var tasks = await _service.GetAll(status, assignedTo);
        return Ok(ApiResponseDto<List<Backend.Models.Entities.TaskItem>>.Ok(tasks, "Tasks retrieved"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        var task = await _service.Update(id, dto);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Delete(id);
        return Ok(ApiResponseDto<object>.Ok(null, "Task deleted"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await _service.GetById(id));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
    {
        var task = await _service.UpdateStatus(id, dto.Status);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task status updated"));
    }

    [HttpPatch("{id}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTaskDto dto)
    {
        var task = await _service.Assign(id, dto.UserId);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task assigned"));
    }

    [HttpPatch("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(Guid id, [FromBody] UpdateTaskPriorityDto dto)
    {
        var task = await _service.UpdatePriority(id, dto.Priority);
        return Ok(ApiResponseDto<Backend.Models.Entities.TaskItem>.Ok(task, "Task priority updated"));
    }

}
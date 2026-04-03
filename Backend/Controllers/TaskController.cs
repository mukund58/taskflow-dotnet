using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interface;

[ApiController]
[Route("api/tasks")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        return Ok(await _service.Create(dto));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? assignedTo)
    {
        return Ok(await _service.GetAll(status, assignedTo));
    }

    [Authorize]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusDto dto)
    {
        return Ok(await _service.UpdateStatus(id, dto.Status));
    }

    [Authorize]
    [HttpPatch("{id}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignTaskDto dto)
    {
        return Ok(await _service.Assign(id, dto.UserId));
    }
}
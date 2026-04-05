namespace Backend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectController(IProjectService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _service.GetAll();
        return Ok(ApiResponseDto<List<Backend.Models.Entities.Project>>.Ok(projects, "Projects retrieved"));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectDto dto)
    {
        var project = await _service.Create(dto);
        return Ok(ApiResponseDto<Backend.Models.Entities.Project>.Ok(project, "Project created"));
    }
}

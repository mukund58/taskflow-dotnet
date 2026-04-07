namespace Backend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

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
    [Authorize(Policy = "ProjectRead")]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _service.GetAll();
        return Ok(ApiResponseDto<List<Backend.Models.Entities.Project>>.Ok(projects, "Projects retrieved"));
    }

    [HttpPost]
    [Authorize(Policy = "ProjectWrite")]
    public async Task<IActionResult> Create([FromBody] ProjectDto dto)
    {
        var project = await _service.Create(dto);
        return Ok(ApiResponseDto<Backend.Models.Entities.Project>.Ok(project, "Project created"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await _service.GetById(id);
        if (project == null)
            return NotFound(ApiResponseDto<object>.Fail("Project not found"));

        return Ok(ApiResponseDto<Backend.Models.Entities.Project>.Ok(project, "Project retrieved"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProjectDto dto)
    {
        var project = await _service.Update(id, dto);
        if (project == null)
            return NotFound(ApiResponseDto<object>.Fail("Project not found"));

        return Ok(ApiResponseDto<Backend.Models.Entities.Project>.Ok(project, "Project updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.Delete(id);
        if (!result)
            return NotFound(ApiResponseDto<object>.Fail("Project not found"));

        return Ok(ApiResponseDto<object>.Ok(null, "Project deleted"));
    }
}

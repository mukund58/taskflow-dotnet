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
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var creatorUserId))
            throw new UnauthorizedAccessException("Invalid user context");

        var project = await _service.Create(dto, creatorUserId);
        return Ok(ApiResponseDto<Backend.Models.Entities.Project>.Ok(project, "Project created"));
    }
}

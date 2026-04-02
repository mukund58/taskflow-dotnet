using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;

[ApiController]
[Route("api/projects")]
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
        return Ok(await _service.GetAll());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectDto dto)
    {
        return Ok(await _service.Create(dto));
    }
}

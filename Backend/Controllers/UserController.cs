namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UserController : BaseApiController
{
    private readonly IUserService _service;

    public UserController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _service.GetAll();
        return Ok(ApiResponseDto<List<UserDto>>.Ok(users, "Users retrieved"));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "TaskRead")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!HasElevatedAccess() && id != GetCurrentUserId())
            return Forbid();

        var user = await _service.GetById(id);
        return Ok(ApiResponseDto<UserDto>.Ok(user, "User retrieved"));
    }
}
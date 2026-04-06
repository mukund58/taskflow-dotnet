namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.Register(dto);

        if (!result.Success)
        {
            return BadRequest(ApiResponseDto<object>.Fail(result.Message));
        }

        return Ok(ApiResponseDto<AuthResponseDto>.Ok(result, result.Message));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.Login(dto);

        if (!result.Success)
        {
            return Unauthorized(ApiResponseDto<object>.Fail(result.Message));
        }

        return Ok(ApiResponseDto<AuthResponseDto>.Ok(result, result.Message));
    }
}

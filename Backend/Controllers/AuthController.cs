namespace Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.Login(dto);

        if (!result.Success)
        {
            return Unauthorized(ApiResponseDto<object>.Fail(result.Message));
        }

        return Ok(ApiResponseDto<AuthResponseDto>.Ok(result, result.Message));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);

        if (!result.Success)
        {
            return Unauthorized(ApiResponseDto<object>.Fail(result.Message));
        }

        return Ok(ApiResponseDto<AuthResponseDto>.Ok(result, result.Message));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await _authService.LogoutAsync(dto.RefreshToken);
        return Ok(ApiResponseDto<object>.Ok(null, "Logged out successfully"));
    }

    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAll()
    {
        var userId = GetCurrentUserId();
        await _authService.RevokeAllTokensAsync(userId);
        return Ok(ApiResponseDto<object>.Ok(null, "All sessions have been revoked"));
    }
}

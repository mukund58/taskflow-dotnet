namespace Backend.Controllers;

using System.Security.Claims;
using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMySettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _settingsService.GetCurrentUserSettingsAsync(userId);
        return Ok(ApiResponseDto<UserSettingsDto>.Ok(settings, "Settings retrieved"));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMySettings([FromBody] UpdateUserSettingsDto dto)
    {
        var userId = GetCurrentUserId();
        var settings = await _settingsService.UpdateCurrentUserSettingsAsync(userId, dto);
        return Ok(ApiResponseDto<UserSettingsDto>.Ok(settings, "Settings updated"));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user context");

        return userId;
    }
}

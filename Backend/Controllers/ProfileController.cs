namespace Backend.Controllers;

using Backend.Models.DTOs;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile")]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _profileService.GetCurrentUserProfileAsync(GetCurrentUserId());
        return Ok(ApiResponseDto<UserProfileDto>.Ok(profile, "Profile retrieved"));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
    {
        var profile = await _profileService.UpdateCurrentUserProfileAsync(GetCurrentUserId(), dto);
        return Ok(ApiResponseDto<UserProfileDto>.Ok(profile, "Profile updated"));
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _profileService.ChangePasswordAsync(GetCurrentUserId(), dto);
        return Ok(ApiResponseDto<object>.Ok(null, "Password changed"));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
    {
        await _profileService.DeleteAccountAsync(GetCurrentUserId(), dto);
        return Ok(ApiResponseDto<object>.Ok(null, "Account deleted"));
    }
}

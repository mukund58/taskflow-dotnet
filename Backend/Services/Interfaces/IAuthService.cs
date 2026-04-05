namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface IAuthService
{
    Task<AuthResponseDto> Register(RegisterDto dto);
    Task<AuthResponseDto> Login(LoginDto dto);
}

namespace Backend.Services.Implementations;

using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.DTOs;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> Register(RegisterDto dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return "User Registered";
    }

    public async Task<string> Login(LoginDto dto){
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null || !BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        return "Login success"; // JWT later
    }

}

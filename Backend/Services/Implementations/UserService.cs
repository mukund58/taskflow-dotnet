namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAll()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })
            .ToListAsync();
    }

    public async Task<UserDto> GetById(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }
}
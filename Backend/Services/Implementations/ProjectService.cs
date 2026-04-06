namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _context;

    public ProjectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Project>> GetAll()
    {
        return await _context.Projects
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Project> Create(ProjectDto dto, Guid creatorUserId)
    {
        var creator = await _context.Users.FirstOrDefaultAsync(x => x.Id == creatorUserId);

        if (creator == null)
            throw new UnauthorizedAccessException("User not found");

        if (string.Equals(creator.Role, "User", StringComparison.OrdinalIgnoreCase))
            creator.Role = "Manager";

        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }
}

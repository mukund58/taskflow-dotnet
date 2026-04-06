namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface IProjectService
{
    Task<List<Project>> GetAll();
    Task<Project> Create(ProjectDto dto, Guid creatorUserId);
}

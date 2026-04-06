namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface IProjectService
{
    Task<List<Project>> GetAll();
    Task<Project> Create(ProjectDto dto);
    Task<Project?> GetById(Guid id);
    Task<Project?> Update(Guid id, ProjectDto dto);
    Task<bool> Delete(Guid id);
}

namespace Backend.Services.Interfaces;

using Backend.Models.DTOs;
using Backend.Models.Entities;

public interface IProjectService
{
    Task<List<Project>> GetAll();
    Task<List<Project>> GetAccessibleProjects(Guid userId, bool elevatedAccess);
    Task<Project> Create(ProjectDto dto, Guid creatorUserId);
    Task<Project?> GetById(Guid id);
    Task<Project?> Update(Guid id, ProjectDto dto);
    Task<bool> Delete(Guid id);
    Task<bool> ProjectExists(Guid id);
    Task<bool> HasReadAccess(Guid projectId, Guid userId, bool elevatedAccess);
    Task<bool> HasWriteAccess(Guid projectId, Guid userId, bool elevatedAccess);
    Task<bool> HasManageAccess(Guid projectId, Guid userId, bool elevatedAccess);
    Task<List<ProjectMemberDto>> GetMembers(Guid projectId);
    Task<ProjectMemberDto> AddMember(Guid projectId, AddProjectMemberDto dto, Guid actorUserId);
    Task<List<ProjectInvitationDto>> GetInvitations(Guid projectId);
    Task<ProjectInvitationDto> CreateInvitation(Guid projectId, CreateProjectInvitationDto dto, Guid actorUserId);
}

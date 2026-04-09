namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ProjectService : IProjectService
{
    private const string ProjectAdminRole = "Admin";
    private const string ProjectMemberRole = "Member";
    private const string PendingInvitationStatus = "Pending";

    private readonly AppDbContext _context;
    private readonly IEmailService? _emailService;

    public ProjectService(AppDbContext context, IEmailService? emailService = null)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<List<Project>> GetAll()
    {
        return await _context.Projects
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Project>> GetAccessibleProjects(Guid userId, bool elevatedAccess)
    {
        var query = _context.Projects.AsNoTracking().AsQueryable();

        if (elevatedAccess)
            return await query.ToListAsync();

        return await query
            .Where(p =>
                p.OwnerUserId == userId ||
                _context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId))
            .ToListAsync();
    }

    public async Task<Project> Create(ProjectDto dto, Guid creatorUserId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            DueDate = dto.DueDate,
            OwnerUserId = creatorUserId
        };

        var ownerMembership = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = creatorUserId,
            Role = ProjectAdminRole,
            AddedByUserId = creatorUserId,
            AddedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        _context.ProjectMembers.Add(ownerMembership);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<Project?> GetById(Guid id)
    {
        return await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project?> Update(Guid id, ProjectDto dto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return null;

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.DueDate = dto.DueDate;

        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<bool> Delete(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ProjectExists(Guid id)
    {
        return await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == id);
    }

    public async Task<bool> HasReadAccess(Guid projectId, Guid userId, bool elevatedAccess)
    {
        if (elevatedAccess)
            return await ProjectExists(projectId);

        return await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId &&
                           (p.OwnerUserId == userId ||
                            _context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId)));
    }

    public async Task<bool> HasWriteAccess(Guid projectId, Guid userId, bool elevatedAccess)
    {
        if (elevatedAccess)
            return await ProjectExists(projectId);

        return await _context.Projects
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == projectId &&
                (p.OwnerUserId == userId ||
                 _context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId)));
    }

    public async Task<bool> HasManageAccess(Guid projectId, Guid userId, bool elevatedAccess)
    {
        if (elevatedAccess)
            return await ProjectExists(projectId);

        return await _context.Projects
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == projectId &&
                (p.OwnerUserId == userId ||
                 _context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId && pm.Role == ProjectAdminRole)));
    }

    public async Task<List<ProjectMemberDto>> GetMembers(Guid projectId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Project not found");

        var members = await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == projectId)
            .Join(
                _context.Users.AsNoTracking().Where(user => !user.IsDeleted),
                pm => pm.UserId,
                user => user.Id,
                (pm, user) => new ProjectMemberDto
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = pm.Role,
                    AddedAt = pm.AddedAt,
                    IsProjectOwner = project.OwnerUserId == user.Id
                })
            .OrderByDescending(member => member.Role == ProjectAdminRole)
            .ThenBy(member => member.Name)
            .ToListAsync();

        if (project.OwnerUserId.HasValue && members.All(member => member.UserId != project.OwnerUserId.Value))
        {
            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == project.OwnerUserId.Value && !user.IsDeleted);

            if (owner != null)
            {
                members.Add(new ProjectMemberDto
                {
                    UserId = owner.Id,
                    Name = owner.Name,
                    Email = owner.Email,
                    Role = ProjectAdminRole,
                    AddedAt = DateTime.UtcNow,
                    IsProjectOwner = true
                });
            }
        }

        return members
            .OrderByDescending(member => member.Role == ProjectAdminRole)
            .ThenBy(member => member.Name)
            .ToList();
    }

    public async Task<ProjectMemberDto> AddMember(Guid projectId, AddProjectMemberDto dto, Guid actorUserId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Project not found");

        var user = await ResolveMemberUserAsync(dto);

        var isAlreadyMember = await _context.ProjectMembers
            .AsNoTracking()
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == user.Id);

        if (isAlreadyMember || project.OwnerUserId == user.Id)
            throw new InvalidOperationException("User is already a member of this project");

        var role = NormalizeRole(dto.Role);

        var membership = new ProjectMember
        {
            ProjectId = projectId,
            UserId = user.Id,
            Role = role,
            AddedByUserId = actorUserId,
            AddedAt = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(membership);
        await _context.SaveChangesAsync();

        return new ProjectMemberDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = membership.Role,
            AddedAt = membership.AddedAt,
            IsProjectOwner = false
        };
    }

    public async Task<List<ProjectInvitationDto>> GetInvitations(Guid projectId)
    {
        return await _context.ProjectInvitations
            .AsNoTracking()
            .Where(pi => pi.ProjectId == projectId)
            .Join(
                _context.Users.AsNoTracking(),
                pi => pi.InvitedByUserId,
                user => user.Id,
                (pi, user) => new ProjectInvitationDto
                {
                    Id = pi.Id,
                    Email = pi.Email,
                    Role = pi.Role,
                    Status = pi.Status,
                    InvitedByUserId = pi.InvitedByUserId,
                    InvitedByUserName = user.Name,
                    CreatedAt = pi.CreatedAt,
                    ExpiresAt = pi.ExpiresAt
                })
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProjectInvitationDto> CreateInvitation(Guid projectId, CreateProjectInvitationDto dto, Guid actorUserId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
            throw new KeyNotFoundException("Project not found");

        var email = NormalizeEmail(dto.Email);
        var role = NormalizeRole(dto.Role);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(Math.Clamp(dto.ExpiresInDays, 1, 30));

        var invitedByName = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == actorUserId)
            .Select(user => user.Name)
            .FirstOrDefaultAsync();

        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => !user.IsDeleted && user.Email.ToLower() == email);

        if (existingUser != null)
        {
            var isExistingMember = await _context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == existingUser.Id);

            var isProjectOwner = await _context.Projects
                .AsNoTracking()
                .AnyAsync(project => project.Id == projectId && project.OwnerUserId == existingUser.Id);

            if (isExistingMember || isProjectOwner)
                throw new InvalidOperationException("User is already a member of this project");
        }

        var hasPendingInvite = await _context.ProjectInvitations
            .AsNoTracking()
            .AnyAsync(pi =>
                pi.ProjectId == projectId &&
                pi.Email == email &&
                pi.Status == PendingInvitationStatus &&
                (!pi.ExpiresAt.HasValue || pi.ExpiresAt.Value > now));

        if (hasPendingInvite)
            throw new InvalidOperationException("A pending invitation already exists for this email");

        if (_emailService != null)
        {
            await _emailService.SendProjectInvitationEmail(
                email,
                project.Name,
                invitedByName ?? "Project Admin",
                role,
                expiresAt);
        }

        var invitation = new ProjectInvitation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Email = email,
            Role = role,
            Status = PendingInvitationStatus,
            InvitedByUserId = actorUserId,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        _context.ProjectInvitations.Add(invitation);

        if (existingUser != null && existingUser.Id != actorUserId)
        {
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = existingUser.Id,
                Message = $"You were invited to join a project.",
                Type = "ProjectInvitation",
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        return new ProjectInvitationDto
        {
            Id = invitation.Id,
            Email = invitation.Email,
            Role = invitation.Role,
            Status = invitation.Status,
            InvitedByUserId = actorUserId,
            InvitedByUserName = invitedByName ?? string.Empty,
            CreatedAt = invitation.CreatedAt,
            ExpiresAt = invitation.ExpiresAt
        };
    }

    private async Task<User> ResolveMemberUserAsync(AddProjectMemberDto dto)
    {
        if (dto.UserId.HasValue)
        {
            var byId = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == dto.UserId.Value && !user.IsDeleted);

            if (byId == null)
                throw new KeyNotFoundException("User not found");

            return byId;
        }

        var email = NormalizeEmail(dto.Email);
        var byEmail = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => !user.IsDeleted && user.Email.ToLower() == email);

        if (byEmail == null)
            throw new KeyNotFoundException("User not found. Send an invitation instead");

        return byEmail;
    }

    private static string NormalizeRole(string role)
    {
        var candidate = role?.Trim() ?? string.Empty;

        if (candidate.Equals(ProjectAdminRole, StringComparison.OrdinalIgnoreCase))
            return ProjectAdminRole;

        if (candidate.Equals(ProjectMemberRole, StringComparison.OrdinalIgnoreCase))
            return ProjectMemberRole;

        throw new InvalidOperationException("Role must be Admin or Member");
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required");

        return email.Trim().ToLowerInvariant();
    }
}

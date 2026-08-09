namespace Backend.Services.Implementations;

using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ProjectService : IProjectService
{
    private const string ProjectAdminRole = "Admin";
    private const string ProjectMemberRole = "Member";
    private const string PendingInvitationStatus = "Pending";
    private const string AcceptedInvitationStatus = "Accepted";
    private const string RevokedInvitationStatus = "Revoked";
    private const string ExpiredInvitationStatus = "Expired";
    private const string DefaultFrontendBaseUrl = "http://localhost:3001";

    private readonly AppDbContext _context;
    private readonly IEmailService? _emailService;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<ProjectService>? _logger;

    public ProjectService(
        AppDbContext context,
        IEmailService? emailService = null,
        IConfiguration? configuration = null,
        ILogger<ProjectService>? logger = null)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
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

        var invitationUrl = BuildInvitationUrl(invitation.Id);

        if (_emailService != null)
        {
            try
            {
                await _emailService.SendProjectInvitationEmail(
                    email,
                    project.Name,
                    invitedByName ?? "Project Admin",
                    role,
                    expiresAt,
                    invitationUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send project invitation email to {Email}", email);
            }
        }

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

    public async Task<List<ProjectInvitationLookupDto>> GetInvitationsForUser(Guid userId)
    {
        var userEmail = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDeleted)
            .Select(user => user.Email)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User not found");

        var normalizedEmail = NormalizeEmail(userEmail);

        var rows = await _context.ProjectInvitations
            .AsNoTracking()
            .Where(pi => pi.Email == normalizedEmail)
            .Select(pi => new
            {
                Invitation = pi,
                ProjectName = _context.Projects
                    .Where(project => project.Id == pi.ProjectId)
                    .Select(project => project.Name)
                    .FirstOrDefault(),
                InvitedByUserName = _context.Users
                    .Where(user => user.Id == pi.InvitedByUserId)
                    .Select(user => user.Name)
                    .FirstOrDefault()
            })
            .OrderByDescending(row => row.Invitation.CreatedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;

        return rows
            .Where(row => !string.IsNullOrWhiteSpace(row.ProjectName))
            .Select(row => new ProjectInvitationLookupDto
            {
                Id = row.Invitation.Id,
                ProjectId = row.Invitation.ProjectId,
                ProjectName = row.ProjectName!,
                Email = row.Invitation.Email,
                Role = row.Invitation.Role,
                Status = row.Invitation.Status,
                InvitedByUserId = row.Invitation.InvitedByUserId,
                InvitedByUserName = row.InvitedByUserName ?? "Project Admin",
                CreatedAt = row.Invitation.CreatedAt,
                ExpiresAt = row.Invitation.ExpiresAt,
                IsExpired = IsInvitationExpired(row.Invitation, now)
            })
            .ToList();
    }

    public async Task<ProjectInvitationLookupDto> GetInvitationById(Guid invitationId)
    {
        var row = await _context.ProjectInvitations
            .AsNoTracking()
            .Where(pi => pi.Id == invitationId)
            .Select(pi => new
            {
                Invitation = pi,
                ProjectName = _context.Projects
                    .Where(project => project.Id == pi.ProjectId)
                    .Select(project => project.Name)
                    .FirstOrDefault(),
                InvitedByUserName = _context.Users
                    .Where(user => user.Id == pi.InvitedByUserId)
                    .Select(user => user.Name)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (row == null)
            throw new KeyNotFoundException("Invitation not found");

        if (string.IsNullOrWhiteSpace(row.ProjectName))
            throw new KeyNotFoundException("Project not found");

        var now = DateTime.UtcNow;
        var isExpired = IsInvitationExpired(row.Invitation, now);

        return new ProjectInvitationLookupDto
        {
            Id = row.Invitation.Id,
            ProjectId = row.Invitation.ProjectId,
            ProjectName = row.ProjectName,
            Email = row.Invitation.Email,
            Role = row.Invitation.Role,
            Status = row.Invitation.Status,
            InvitedByUserId = row.Invitation.InvitedByUserId,
            InvitedByUserName = row.InvitedByUserName ?? "Project Admin",
            CreatedAt = row.Invitation.CreatedAt,
            ExpiresAt = row.Invitation.ExpiresAt,
            IsExpired = isExpired
        };
    }

    public async Task<AcceptProjectInvitationResultDto> AcceptInvitation(Guid invitationId, Guid actorUserId)
    {
        var invitation = await _context.ProjectInvitations
            .FirstOrDefaultAsync(pi => pi.Id == invitationId)
            ?? throw new KeyNotFoundException("Invitation not found");

        var now = DateTime.UtcNow;
        if (IsInvitationExpired(invitation, now))
        {
            if (string.Equals(invitation.Status, PendingInvitationStatus, StringComparison.OrdinalIgnoreCase))
            {
                invitation.Status = ExpiredInvitationStatus;
                await _context.SaveChangesAsync();
            }

            throw new InvalidOperationException("Invitation has expired");
        }

        if (string.Equals(invitation.Status, RevokedInvitationStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invitation has been revoked");

        if (!string.Equals(invitation.Status, PendingInvitationStatus, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(invitation.Status, AcceptedInvitationStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invitation is in '{invitation.Status}' state and cannot be accepted");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == actorUserId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        if (!string.Equals(user.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("This invitation belongs to a different email account");

        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == invitation.ProjectId)
            ?? throw new KeyNotFoundException("Project not found");

        var isProjectOwner = project.OwnerUserId == actorUserId;

        var existingMembership = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == invitation.ProjectId && pm.UserId == actorUserId);

        var acceptedRole = NormalizeInvitationRole(invitation.Role);

        if (!isProjectOwner && existingMembership == null)
        {
            existingMembership = new ProjectMember
            {
                ProjectId = invitation.ProjectId,
                UserId = actorUserId,
                Role = acceptedRole,
                AddedByUserId = invitation.InvitedByUserId,
                AddedAt = now
            };

            _context.ProjectMembers.Add(existingMembership);
        }

        invitation.Status = AcceptedInvitationStatus;
        await _context.SaveChangesAsync();

        return new AcceptProjectInvitationResultDto
        {
            InvitationId = invitation.Id,
            ProjectId = invitation.ProjectId,
            ProjectName = project.Name,
            Role = isProjectOwner ? ProjectAdminRole : (existingMembership?.Role ?? acceptedRole),
            Status = invitation.Status
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

    private static string NormalizeInvitationRole(string? role)
    {
        var candidate = role?.Trim() ?? string.Empty;

        if (candidate.Equals(ProjectAdminRole, StringComparison.OrdinalIgnoreCase))
            return ProjectAdminRole;

        return ProjectMemberRole;
    }

    private static bool IsInvitationExpired(ProjectInvitation invitation, DateTime now)
    {
        return invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value <= now;
    }

    private string BuildInvitationUrl(Guid invitationId)
    {
        var frontendBaseUrl = GetFrontendBaseUrl();
        return $"{frontendBaseUrl}/invitations/{invitationId}";
    }

    private string GetFrontendBaseUrl()
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")
            ?? _configuration?["App:FrontendBaseUrl"]
            ?? _configuration?["Frontend:BaseUrl"]
            ?? DefaultFrontendBaseUrl;

        var trimmed = (configuredBaseUrl ?? DefaultFrontendBaseUrl).Trim().TrimEnd('/');

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            return trimmed;

        return DefaultFrontendBaseUrl;
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

namespace Backend.Tests.Services;

using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;

public class ProjectServiceTests
{
    [Fact]
    public async Task Create_SetsOwnerUserId()
    {
        await using var context = CreateContext();
        var ownerUserId = Guid.NewGuid();

        context.Users.Add(new User
        {
            Id = ownerUserId,
            Name = "Owner",
            Email = "owner@example.com",
            PasswordHash = "hash",
            Role = "User"
        });
        await context.SaveChangesAsync();

        var service = new ProjectService(context);

        var project = await service.Create(new ProjectDto
        {
            Name = "Access Control Project",
            Description = "Project with owner"
        }, ownerUserId);

        Assert.Equal(ownerUserId, project.OwnerUserId);

        var persisted = await context.Projects.SingleAsync(x => x.Id == project.Id);
        Assert.Equal(ownerUserId, persisted.OwnerUserId);

        var ownerMembership = await context.ProjectMembers
            .SingleOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == ownerUserId);

        Assert.NotNull(ownerMembership);
        Assert.Equal("Admin", ownerMembership!.Role);
    }

    [Fact]
    public async Task GetAccessibleProjects_ForRegularUser_ReturnsOnlyOwnedOrMemberProjects()
    {
        await using var context = CreateContext();

        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        context.Users.AddRange(
            new User { Id = currentUserId, Name = "Current", Email = "current@example.com", PasswordHash = "hash", Role = "User" },
            new User { Id = otherUserId, Name = "Other", Email = "other@example.com", PasswordHash = "hash", Role = "User" });

        var ownedProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Owned",
            Description = "Owned project",
            OwnerUserId = currentUserId
        };

        var memberProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Member",
            Description = "Member project",
            OwnerUserId = otherUserId
        };

        var hiddenProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Hidden",
            Description = "No access",
            OwnerUserId = otherUserId
        };

        context.Projects.AddRange(ownedProject, memberProject, hiddenProject);

        context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = memberProject.Id,
            UserId = currentUserId,
            Role = "Member",
            AddedByUserId = otherUserId,
            AddedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProjectService(context);
        var result = await service.GetAccessibleProjects(currentUserId, elevatedAccess: false);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == ownedProject.Id);
        Assert.Contains(result, p => p.Id == memberProject.Id);
        Assert.DoesNotContain(result, p => p.Id == hiddenProject.Id);
    }

    [Fact]
    public async Task GetAccessibleProjects_ForRegularUser_DoesNotIncludeAssignedTaskOnlyProjects()
    {
        await using var context = CreateContext();

        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        context.Users.AddRange(
            new User { Id = currentUserId, Name = "Current", Email = "current@example.com", PasswordHash = "hash", Role = "User" },
            new User { Id = otherUserId, Name = "Other", Email = "other@example.com", PasswordHash = "hash", Role = "User" });

        var assignedOnlyProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Assigned Only",
            Description = "User has task assignment but no membership",
            OwnerUserId = otherUserId
        };

        context.Projects.Add(assignedOnlyProject);

        context.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Assigned Task",
            Description = "Task assigned to current user",
            Status = "Todo",
            Priority = "Medium",
            ProjectId = assignedOnlyProject.Id,
            AssignedUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProjectService(context);
        var result = await service.GetAccessibleProjects(currentUserId, elevatedAccess: false);

        Assert.DoesNotContain(result, p => p.Id == assignedOnlyProject.Id);
    }

    [Fact]
    public async Task HasWriteAccess_ForRegularUser_TrueOnlyForOwnedProject()
    {
        await using var context = CreateContext();

        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        context.Users.AddRange(
            new User { Id = currentUserId, Name = "Current", Email = "current@example.com", PasswordHash = "hash", Role = "User" },
            new User { Id = otherUserId, Name = "Other", Email = "other@example.com", PasswordHash = "hash", Role = "User" });

        var ownedProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Owned",
            Description = "Owned",
            OwnerUserId = currentUserId
        };

        var foreignProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Foreign",
            Description = "Foreign",
            OwnerUserId = otherUserId
        };

        context.Projects.AddRange(ownedProject, foreignProject);
        await context.SaveChangesAsync();

        var service = new ProjectService(context);

        var canWriteOwned = await service.HasWriteAccess(ownedProject.Id, currentUserId, elevatedAccess: false);
        var canWriteForeign = await service.HasWriteAccess(foreignProject.Id, currentUserId, elevatedAccess: false);

        Assert.True(canWriteOwned);
        Assert.False(canWriteForeign);
    }

    [Fact]
    public async Task HasReadAccess_ForElevatedUser_AllowsExistingProject()
    {
        await using var context = CreateContext();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Any Project",
            Description = "Any",
            OwnerUserId = Guid.NewGuid()
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var service = new ProjectService(context);

        var canRead = await service.HasReadAccess(project.Id, Guid.NewGuid(), elevatedAccess: true);

        Assert.True(canRead);
    }

    [Fact]
    public async Task GetAccessibleProjects_ForRegularUser_IncludesMemberProjects()
    {
        await using var context = CreateContext();

        var currentUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        context.Users.AddRange(
            new User { Id = currentUserId, Name = "Current", Email = "current@example.com", PasswordHash = "hash", Role = "User" },
            new User { Id = ownerUserId, Name = "Owner", Email = "owner@example.com", PasswordHash = "hash", Role = "User" });

        var memberProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Member Project",
            Description = "Project where current user is member",
            OwnerUserId = ownerUserId
        };

        context.Projects.Add(memberProject);
        context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = memberProject.Id,
            UserId = currentUserId,
            Role = "Member",
            AddedByUserId = ownerUserId,
            AddedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProjectService(context);
        var result = await service.GetAccessibleProjects(currentUserId, elevatedAccess: false);

        Assert.Contains(result, p => p.Id == memberProject.Id);
    }

    [Fact]
    public async Task AddMember_AddsExistingUserToProject()
    {
        await using var context = CreateContext();

        var ownerUserId = Guid.NewGuid();
        var newMemberUserId = Guid.NewGuid();

        context.Users.AddRange(
            new User { Id = ownerUserId, Name = "Owner", Email = "owner@example.com", PasswordHash = "hash", Role = "User" },
            new User { Id = newMemberUserId, Name = "Teammate", Email = "teammate@example.com", PasswordHash = "hash", Role = "User" });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Collab Project",
            Description = "Project",
            OwnerUserId = ownerUserId
        };

        context.Projects.Add(project);
        context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = ownerUserId,
            Role = "Admin",
            AddedByUserId = ownerUserId,
            AddedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProjectService(context);

        var member = await service.AddMember(project.Id, new AddProjectMemberDto
        {
            UserId = newMemberUserId,
            Role = "Member"
        }, ownerUserId);

        Assert.Equal(newMemberUserId, member.UserId);
        Assert.Equal("Member", member.Role);

        var persisted = await context.ProjectMembers
            .SingleOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == newMemberUserId);

        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task CreateInvitation_CreatesPendingInvitationRecord()
    {
        await using var context = CreateContext();

        var ownerUserId = Guid.NewGuid();

        context.Users.Add(new User
        {
            Id = ownerUserId,
            Name = "Owner",
            Email = "owner@example.com",
            PasswordHash = "hash",
            Role = "User"
        });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Invites",
            Description = "Invitation tests",
            OwnerUserId = ownerUserId
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var service = new ProjectService(context);

        var invitation = await service.CreateInvitation(project.Id, new CreateProjectInvitationDto
        {
            Email = "invitee@example.com",
            Role = "Member",
            ExpiresInDays = 7
        }, ownerUserId);

        Assert.Equal("invitee@example.com", invitation.Email);
        Assert.Equal("Pending", invitation.Status);

        var persisted = await context.ProjectInvitations
            .SingleOrDefaultAsync(pi => pi.ProjectId == project.Id && pi.Email == "invitee@example.com");

        Assert.NotNull(persisted);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
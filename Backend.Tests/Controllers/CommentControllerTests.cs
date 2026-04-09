namespace Backend.Tests.Controllers;

using System.Security.Claims;
using Backend.Controllers;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class CommentControllerTests
{
    [Fact]
    public async Task GetComments_WhenUserIsNotTaskOwner_ReturnsForbid()
    {
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(assignedUserId: Guid.NewGuid());
        var commentService = new FakeCommentService();
        var controller = CreateController(task, commentService, currentUserId, "User");

        var result = await controller.GetComments(task.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetComments_WhenUserOwnsTask_ReturnsOkWithComments()
    {
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(assignedUserId: currentUserId);
        var commentService = new FakeCommentService
        {
            CommentsToReturn =
            [
                new CommentDto
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    AuthorId = currentUserId,
                    AuthorName = "Owner",
                    AuthorEmail = "owner@example.com",
                    Content = "Looks good"
                }
            ]
        };

        var controller = CreateController(task, commentService, currentUserId, "User");

        var result = await controller.GetComments(task.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponseDto<List<CommentDto>>>(okResult.Value);

        Assert.True(response.Success);
        Assert.Single(response.Data!);
        Assert.Equal("Looks good", response.Data![0].Content);
    }

    [Fact]
    public async Task GetComments_WhenUserIsAdmin_AllowsAccessToAnyTask()
    {
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(assignedUserId: Guid.NewGuid());
        var commentService = new FakeCommentService
        {
            CommentsToReturn = []
        };

        var controller = CreateController(task, commentService, currentUserId, "Admin");

        var result = await controller.GetComments(task.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateComment_PassesTaskAndUserIdsToService()
    {
        var currentUserId = Guid.NewGuid();
        var task = CreateTask(assignedUserId: currentUserId);
        var commentService = new FakeCommentService();
        var controller = CreateController(task, commentService, currentUserId, "User");

        var commentId = Guid.NewGuid();
        var dto = new UpdateCommentDto { Content = "Updated" };

        var result = await controller.UpdateComment(task.Id, commentId, dto);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task.Id, commentService.LastUpdateTaskId);
        Assert.Equal(commentId, commentService.LastUpdateCommentId);
        Assert.Equal(currentUserId, commentService.LastUpdateUserId);
    }

    private static CommentController CreateController(TaskItem task, FakeCommentService commentService, Guid currentUserId, string role)
    {
        var controller = new CommentController(commentService, new FakeTaskService(task));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildUser(currentUserId, role)
            }
        };

        return controller;
    }

    private static ClaimsPrincipal BuildUser(Guid userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static TaskItem CreateTask(Guid assignedUserId)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Description = "Task for ownership tests",
            Status = "Todo",
            Priority = "Medium",
            ProjectId = Guid.NewGuid(),
            AssignedUserId = assignedUserId
        };
    }

    private sealed class FakeTaskService : ITaskService
    {
        private readonly TaskItem _task;

        public FakeTaskService(TaskItem task)
        {
            _task = task;
        }

        public Task<TaskItem> Create(CreateTaskDto dto, Guid actorUserId) => throw new NotImplementedException();
        public Task<List<TaskItem>> GetAll(string? status, Guid? assignedTo) => throw new NotImplementedException();
        public Task<PaginatedResponseDto<TaskItem>> GetAllPaginatedAsync(TaskQueryDto query) => throw new NotImplementedException();
        public Task<TaskItem> GetById(Guid taskId) => Task.FromResult(_task);
        public Task<TaskItem> Update(Guid taskId, UpdateTaskDto dto, Guid actorUserId) => throw new NotImplementedException();
        public Task Delete(Guid taskId) => throw new NotImplementedException();
        public Task<TaskItem> UpdateStatus(Guid taskId, string status, Guid actorUserId, long? expectedRowVersion = null) => throw new NotImplementedException();
        public Task<TaskItem> Assign(Guid taskId, Guid userId, Guid actorUserId, long? expectedRowVersion = null) => throw new NotImplementedException();
        public Task<TaskItem> UpdatePriority(Guid taskId, string priority) => throw new NotImplementedException();
        public Task<List<TaskActivity>> GetActivity(Guid taskId) => throw new NotImplementedException();
        public Task<List<ChecklistItem>> GetChecklistItems(Guid taskId) => throw new NotImplementedException();
        public Task<ChecklistItem> AddChecklistItem(Guid taskId, CreateChecklistItemDto dto) => throw new NotImplementedException();
        public Task<ChecklistItem> UpdateChecklistItemCompletion(Guid taskId, Guid checklistItemId, bool isCompleted) => throw new NotImplementedException();
    }

    private sealed class FakeCommentService : ICommentService
    {
        public List<CommentDto> CommentsToReturn { get; set; } = [];

        public Guid? LastUpdateTaskId { get; private set; }
        public Guid? LastUpdateCommentId { get; private set; }
        public Guid? LastUpdateUserId { get; private set; }

        public Task<CommentDto> AddCommentAsync(Guid taskId, Guid userId, CreateCommentDto dto)
        {
            return Task.FromResult(new CommentDto
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                AuthorId = userId,
                AuthorName = "Test",
                AuthorEmail = "test@example.com",
                Content = dto.Content
            });
        }

        public Task<List<CommentDto>> GetTaskCommentsAsync(Guid taskId)
        {
            return Task.FromResult(CommentsToReturn);
        }

        public Task<CommentDto> UpdateCommentAsync(Guid taskId, Guid commentId, Guid userId, UpdateCommentDto dto)
        {
            LastUpdateTaskId = taskId;
            LastUpdateCommentId = commentId;
            LastUpdateUserId = userId;

            return Task.FromResult(new CommentDto
            {
                Id = commentId,
                TaskId = taskId,
                AuthorId = userId,
                AuthorName = "Test",
                AuthorEmail = "test@example.com",
                Content = dto.Content
            });
        }

        public Task DeleteCommentAsync(Guid taskId, Guid commentId, Guid userId)
        {
            return Task.CompletedTask;
        }
    }
}
namespace Backend.Tests.Validation;

using System;
using Backend.Models.DTOs;
using Backend.Validation;

public class TaskDtoValidatorTests
{
    [Fact]
    public void CreateTaskDtoValidator_RejectsMissingTitleAndProjectId()
    {
        var validator = new CreateTaskDtoValidator();
        var dto = new CreateTaskDto
        {
            Title = string.Empty,
            Description = "desc",
            ProjectId = Guid.Empty
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskDto.Title));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskDto.ProjectId));
    }

    [Fact]
    public void UpdateTaskStatusDtoValidator_AllowsKnownStatuses()
    {
        var validator = new UpdateTaskStatusDtoValidator();
        var dto = new UpdateTaskStatusDto
        {
            Status = "In Progress"
        };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTaskDtoValidator_RejectsUnknownPriority()
    {
        var validator = new UpdateTaskDtoValidator();
        var dto = new UpdateTaskDto
        {
            Title = "Task",
            Description = "desc",
            Status = "Todo",
            Priority = "Urgent"
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTaskDto.Priority));
    }
}

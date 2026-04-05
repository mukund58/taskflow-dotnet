namespace Backend.Validation;

using FluentValidation;
using Backend.Models.DTOs;

public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(100)
            .WithMessage("Title must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("ProjectId is required");
    }
}

public class UpdateTaskStatusDtoValidator : AbstractValidator<UpdateTaskStatusDto>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Todo",
        "InProgress",
        "In Progress",
        "Done"
    };

    public UpdateTaskStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => AllowedStatuses.Contains(status))
            .WithMessage("Status must be one of: Todo, InProgress, In Progress, Done");
    }
}

public class AssignTaskDtoValidator : AbstractValidator<AssignTaskDto>
{
    public AssignTaskDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Todo",
        "InProgress",
        "In Progress",
        "Done"
    };

    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low",
        "Medium",
        "High"
    };

    public UpdateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(100)
            .WithMessage("Title must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => AllowedStatuses.Contains(status))
            .WithMessage("Status must be one of: Todo, InProgress, In Progress, Done");

        RuleFor(x => x.Priority)
            .NotEmpty()
            .WithMessage("Priority is required")
            .Must(priority => AllowedPriorities.Contains(priority))
            .WithMessage("Priority must be one of: Low, Medium, High");
    }
}
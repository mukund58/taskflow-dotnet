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
    public UpdateTaskStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => new[] { "Pending", "InProgress", "Completed", "Cancelled" }.Contains(status))
            .WithMessage("Status must be one of: Pending, InProgress, Completed, Cancelled");
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
namespace Backend.Validation;

using FluentValidation;
using Backend.Models.DTOs;

public class CreateNotificationDtoValidator : AbstractValidator<CreateNotificationDto>
{
    public CreateNotificationDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Notification type is required")
            .Must(x => IsValidNotificationType(x)).WithMessage("Invalid notification type");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }

    private bool IsValidNotificationType(string type)
    {
        var validTypes = new[] { "TaskCreated", "TaskAssigned", "TaskCommented", "CommentLiked", "TaskStatusChanged", "TaskDueDateChanged" };
        return validTypes.Contains(type);
    }
}

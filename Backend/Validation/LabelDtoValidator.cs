namespace Backend.Validation;

using FluentValidation;
using Backend.Models.DTOs;

public class CreateLabelDtoValidator : AbstractValidator<CreateLabelDto>
{
    public CreateLabelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required")
            .MaximumLength(50).WithMessage("Label name cannot exceed 50 characters");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex code (e.g., #FF0000)");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required");
    }
}

public class UpdateLabelDtoValidator : AbstractValidator<UpdateLabelDto>
{
    public UpdateLabelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required")
            .MaximumLength(50).WithMessage("Label name cannot exceed 50 characters");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex code (e.g., #FF0000)");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

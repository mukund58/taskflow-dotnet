namespace Backend.Validation;

using Backend.Models.DTOs;
using FluentValidation;

public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light",
        "dark",
        "system"
    };

    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Theme)
            .NotEmpty().WithMessage("Theme is required")
            .Must(theme => AllowedThemes.Contains(theme))
            .WithMessage("Theme must be one of: light, dark, system");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required")
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required")
            .MaximumLength(64).WithMessage("Timezone must not exceed 64 characters");
    }
}

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required")
            .Equal(x => x.NewPassword).WithMessage("Confirm new password must match new password");
    }
}

public class DeleteAccountDtoValidator : AbstractValidator<DeleteAccountDto>
{
    public DeleteAccountDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");
    }
}

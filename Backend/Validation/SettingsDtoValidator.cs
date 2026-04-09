namespace Backend.Validation;

using Backend.Models.DTOs;
using FluentValidation;

public class UpdateUserSettingsDtoValidator : AbstractValidator<UpdateUserSettingsDto>
{
    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light",
        "dark",
        "system"
    };

    public UpdateUserSettingsDtoValidator()
    {
        RuleFor(x => x.Theme)
            .NotEmpty()
            .WithMessage("Theme is required")
            .Must(theme => AllowedThemes.Contains(theme))
            .WithMessage("Theme must be one of: light, dark, system");

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language is required")
            .MaximumLength(10)
            .WithMessage("Language must not exceed 10 characters");

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .WithMessage("Timezone is required")
            .MaximumLength(64)
            .WithMessage("Timezone must not exceed 64 characters");
    }
}

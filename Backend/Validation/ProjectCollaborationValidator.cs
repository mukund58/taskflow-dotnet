namespace Backend.Validation;

using Backend.Models.DTOs;
using FluentValidation;

public class AddProjectMemberDtoValidator : AbstractValidator<AddProjectMemberDto>
{
    public AddProjectMemberDtoValidator()
    {
        RuleFor(x => x)
            .Must(dto => dto.UserId.HasValue || !string.IsNullOrWhiteSpace(dto.Email))
            .WithMessage("Either userId or email is required");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email must be valid");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Member", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be either Admin or Member");
    }
}

public class CreateProjectInvitationDtoValidator : AbstractValidator<CreateProjectInvitationDto>
{
    public CreateProjectInvitationDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid invitee email is required");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Member", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be either Admin or Member");

        RuleFor(x => x.ExpiresInDays)
            .InclusiveBetween(1, 30)
            .WithMessage("ExpiresInDays must be between 1 and 30");
    }
}

using Auth.Domain.DTOs;
using Auth.Domain.Enums;
using FluentValidation;

namespace Auth.Api.Validators;

public sealed class RegisterPublicUserRequestValidator : AbstractValidator<RegisterPublicUserRequest>
{
    public RegisterPublicUserRequestValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required")
            .Must(BeValidModule).WithMessage("Invalid module");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");
    }

    private bool BeValidModule(string module)
    {
        return Enum.TryParse<SystemModule>(module, true, out _);
    }
}
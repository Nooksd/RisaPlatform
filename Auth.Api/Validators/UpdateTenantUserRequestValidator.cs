using Auth.Domain.DTOs;
using Auth.Domain.Enums;
using FluentValidation;

namespace Auth.Api.Validators;

public sealed class UpdateTenantUserRequestValidator : AbstractValidator<UpdateTenantUserRequest>
{
    public UpdateTenantUserRequestValidator()
    {
        RuleFor(x => x.ModuleAccesses)
            .NotNull().WithMessage("Module accesses are required")
            .Must(ValidateModuleAccesses).WithMessage("Invalid module or access level");
    }

    private bool ValidateModuleAccesses(Dictionary<string, int> accesses)
    {
        var validModules = Enum.GetNames<SystemModule>();

        foreach (var (module, level) in accesses)
        {
            if (!validModules.Contains(module, StringComparer.OrdinalIgnoreCase))
                return false;

            if (level < 0 || level > 3)
                return false;
        }

        return true;
    }
}

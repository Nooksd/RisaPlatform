using Billing.Domain.DTOs.Requests;
using FluentValidation;

namespace Billing.Api.Validators;

public sealed class CreateTenantBillingValidator : AbstractValidator<CreateTenantBillingRequest>
{
    public CreateTenantBillingValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId é obrigatório");
            
        RuleFor(x => x.TenantAccountId)
            .NotEmpty().WithMessage("TenantAccountId é obrigatório");
            
        RuleFor(x => x.BillingEmail)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("Email deve ter no máximo 255 caracteres");
            
        RuleFor(x => x.TaxId)
            .MaximumLength(20).WithMessage("CPF/CNPJ deve ter no máximo 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TaxId));
            
        RuleFor(x => x.LegalName)
            .MaximumLength(255).WithMessage("Nome/Razão Social deve ter no máximo 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.LegalName));
    }
}

public sealed class UpdateBillingInfoValidator : AbstractValidator<UpdateBillingInfoRequest>
{
    public UpdateBillingInfoValidator()
    {
        RuleFor(x => x.BillingEmail)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("Email deve ter no máximo 255 caracteres");
            
        RuleFor(x => x.TaxId)
            .MaximumLength(20).WithMessage("CPF/CNPJ deve ter no máximo 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TaxId));
            
        RuleFor(x => x.LegalName)
            .MaximumLength(255).WithMessage("Nome/Razão Social deve ter no máximo 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.LegalName));
    }
}

public sealed class CalculatePriceValidator : AbstractValidator<CalculatePriceRequest>
{
    public CalculatePriceValidator()
    {
        RuleFor(x => x.ModuleCodes)
            .NotEmpty().WithMessage("Pelo menos um módulo é obrigatório")
            .Must(codes => codes.All(c => !string.IsNullOrWhiteSpace(c)))
            .WithMessage("Códigos de módulo não podem ser vazios");
            
        RuleFor(x => x.UserCount)
            .GreaterThan(0).WithMessage("Quantidade de usuários deve ser maior que 0")
            .LessThanOrEqualTo(10000).WithMessage("Quantidade máxima de usuários é 10.000");
            
        RuleFor(x => x.Duration)
            .IsInEnum().WithMessage("Duração inválida");
    }
}

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId é obrigatório");
            
        RuleFor(x => x.ModuleCodes)
            .NotEmpty().WithMessage("Pelo menos um módulo é obrigatório");
            
        RuleFor(x => x.UserCount)
            .GreaterThan(0).WithMessage("Quantidade de usuários deve ser maior que 0");
            
        RuleFor(x => x.Duration)
            .IsInEnum().WithMessage("Duração inválida");
            
        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("Método de pagamento inválido");
    }
}

public sealed class RequestGracePeriodValidator : AbstractValidator<RequestGracePeriodRequest>
{
    public RequestGracePeriodValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId é obrigatório");
    }
}

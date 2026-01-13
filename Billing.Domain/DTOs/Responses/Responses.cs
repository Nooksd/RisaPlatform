using Billing.Domain.Enums;

namespace Billing.Domain.DTOs.Responses;

public sealed record ModuleResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal PricePerUser,
    bool IsActive,
    IEnumerable<QuantityDiscountResponse> QuantityDiscounts);

public sealed record QuantityDiscountResponse(
    int MinUsers,
    decimal DiscountPercentage);

public sealed record TenantBillingResponse(
    Guid Id,
    Guid TenantId,
    Guid TenantAccountId,
    TenantStatus Status,
    DateTime StatusChangedAt,
    string BillingEmail,
    string? TaxId,
    string? LegalName,
    bool GracePeriodUsedInCurrentCycle,
    SubscriptionResponse? ActiveSubscription);

public sealed record SubscriptionResponse(
    Guid Id,
    SubscriptionDuration Duration,
    int UserCount,
    decimal TotalAmount,
    DateTime StartsAt,
    DateTime ExpiresAt,
    bool IsActive,
    IEnumerable<SubscriptionModuleResponse> Modules);

public sealed record SubscriptionModuleResponse(
    string ModuleCode,
    decimal PricePerUserAtPurchase,
    decimal QuantityDiscountApplied,
    decimal TotalBeforeTimeDiscount);

public sealed record PricingResponse(
    decimal SubtotalBeforeDiscounts,
    decimal QuantityDiscountTotal,
    decimal DurationDiscountTotal,
    decimal FinalTotal,
    decimal DurationDiscountPercentage,
    IEnumerable<ModulePricingResponse> ModuleDetails);

public sealed record ModulePricingResponse(
    string ModuleCode,
    string ModuleName,
    decimal PricePerUser,
    decimal QuantityDiscountPercentage,
    decimal PricePerUserAfterQuantityDiscount,
    decimal TotalBeforeTimeDiscount);

public sealed record PaymentResponse(
    Guid Id,
    string? GatewayPaymentId,
    PaymentMethod Method,
    PaymentStatus Status,
    decimal Amount,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? ConfirmedAt,
    // Stripe Checkout URL (para redirect)
    string? CheckoutUrl,
    // PIX (se disponível)
    string? PixCopyPaste,
    string? PixQrCode,
    DateTime? PixExpiresAt,
    // Boleto (se disponível)
    string? BoletoUrl,
    string? BoletoBarcode,
    // Card
    string? CardLastFour,
    string? CardBrand);

public sealed record GracePeriodResponse(
    bool Granted,
    DateTime? ExpiresAt,
    string? Message);

public sealed record ApiErrorResponse(
    string Message,
    string? Code,
    IDictionary<string, string[]>? Errors);

using Billing.Domain.Enums;

namespace Billing.Domain.DTOs.Requests;

public sealed record CreateTenantBillingRequest(
    Guid TenantId,
    Guid TenantAccountId,
    string BillingEmail,
    string? TaxId,
    string? LegalName);

public sealed record UpdateBillingInfoRequest(
    string BillingEmail,
    string? TaxId,
    string? LegalName);

public sealed record CalculatePriceRequest(
    string[] ModuleCodes,
    int UserCount,
    SubscriptionDuration Duration);

public sealed record CreatePaymentRequest(
    Guid TenantId,
    string[] ModuleCodes,
    int UserCount,
    SubscriptionDuration Duration,
    PaymentMethod Method);

public sealed record RequestGracePeriodRequest(
    Guid TenantId);

// Stripe Webhook Events
public sealed record StripeWebhookEvent(
    string Id,
    string Type,
    StripeWebhookData Data);

public sealed record StripeWebhookData(
    StripeWebhookObject Object);

public sealed record StripeWebhookObject(
    string Id,
    string? PaymentStatus,
    string? Customer,
    long? AmountTotal);

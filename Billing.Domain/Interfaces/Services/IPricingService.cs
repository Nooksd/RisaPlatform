using Billing.Domain.Enums;

namespace Billing.Domain.Interfaces.Services;

/// <summary>
/// Serviço de cálculo de preços
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Calcula o preço de uma assinatura
    /// </summary>
    Task<PricingResult> CalculatePriceAsync(PricingRequest request, CancellationToken ct = default);
}

public record PricingRequest(
    IEnumerable<string> ModuleCodes,
    int UserCount,
    SubscriptionDuration Duration,
    int GracePeriodDaysToDeduct = 0);

public record PricingResult(
    bool Success,
    string? ErrorMessage,
    decimal SubtotalBeforeDiscounts,
    decimal QuantityDiscountTotal,
    decimal DurationDiscountTotal,
    decimal GracePeriodDeduction,
    decimal FinalTotal,
    IEnumerable<ModulePricingDetail> ModuleDetails);

public record ModulePricingDetail(
    string ModuleCode,
    string ModuleName,
    decimal PricePerUser,
    decimal QuantityDiscountPercentage,
    decimal PricePerUserAfterQuantityDiscount,
    decimal TotalBeforeTimeDiscount);

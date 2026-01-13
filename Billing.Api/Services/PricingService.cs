using Billing.Domain.Enums;
using Billing.Domain.Interfaces.Repositories;
using Billing.Domain.Interfaces.Services;

namespace Billing.Api.Services;

public sealed class PricingService(IModuleRepository moduleRepository, ILogger<PricingService> logger) : IPricingService
{
    private readonly IModuleRepository _moduleRepository = moduleRepository;
    private readonly ILogger<PricingService> _logger = logger;

    public async Task<PricingResult> CalculatePriceAsync(PricingRequest request, CancellationToken ct = default)
    {
        var modules = (await _moduleRepository.GetByCodesAsync(request.ModuleCodes, ct)).ToList();

        if (modules.Count == 0)
        {
            return new PricingResult(
                Success: false,
                ErrorMessage: "Nenhum módulo encontrado",
                SubtotalBeforeDiscounts: 0,
                QuantityDiscountTotal: 0,
                DurationDiscountTotal: 0,
                GracePeriodDeduction: 0,
                FinalTotal: 0,
                ModuleDetails: []);
        }

        var missingModules = request.ModuleCodes
            .Except(modules.Select(m => m.Code), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingModules.Count > 0)
        {
            return new PricingResult(
                Success: false,
                ErrorMessage: $"Módulos não encontrados: {string.Join(", ", missingModules)}",
                SubtotalBeforeDiscounts: 0,
                QuantityDiscountTotal: 0,
                DurationDiscountTotal: 0,
                GracePeriodDeduction: 0,
                FinalTotal: 0,
                ModuleDetails: []);
        }

        var months = request.Duration.GetMonths();
        var durationDiscount = request.Duration.GetDiscountPercentage();
        var moduleDetails = new List<ModulePricingDetail>();
        decimal subtotalBeforeDiscounts = 0;
        decimal quantityDiscountTotal = 0;

        foreach (var module in modules)
        {
            var pricePerUser = module.PricePerUser;
            var quantityDiscount = GetQuantityDiscount(module, request.UserCount);
            var priceAfterQuantityDiscount = pricePerUser * (1 - quantityDiscount);
            var totalBeforeTimeDiscount = priceAfterQuantityDiscount * request.UserCount * months;

            moduleDetails.Add(new ModulePricingDetail(
                ModuleCode: module.Code,
                ModuleName: module.Name,
                PricePerUser: pricePerUser,
                QuantityDiscountPercentage: quantityDiscount,
                PricePerUserAfterQuantityDiscount: priceAfterQuantityDiscount,
                TotalBeforeTimeDiscount: totalBeforeTimeDiscount));

            subtotalBeforeDiscounts += pricePerUser * request.UserCount * months;
            quantityDiscountTotal += (pricePerUser - priceAfterQuantityDiscount) * request.UserCount * months;
        }

        var subtotalAfterQuantity = subtotalBeforeDiscounts - quantityDiscountTotal;
        var durationDiscountTotal = subtotalAfterQuantity * durationDiscount;
        var finalTotal = subtotalAfterQuantity - durationDiscountTotal;

        // Desconto por grace period (se aplicável)
        decimal gracePeriodDeduction = 0;
        if (request.GracePeriodDaysToDeduct > 0)
        {
            var dailyRate = finalTotal / (months * 30m);
            gracePeriodDeduction = dailyRate * request.GracePeriodDaysToDeduct;
            // Grace period não reduz o valor, apenas o tempo
            // Mas podemos mostrar quanto "vale" os dias descontados
        }

        _logger.LogInformation(
            "Price calculated: {ModuleCount} modules, {UserCount} users, {Duration} months = R${Total:N2}",
            modules.Count, request.UserCount, months, finalTotal);

        return new PricingResult(
            Success: true,
            ErrorMessage: null,
            SubtotalBeforeDiscounts: Math.Round(subtotalBeforeDiscounts, 2),
            QuantityDiscountTotal: Math.Round(quantityDiscountTotal, 2),
            DurationDiscountTotal: Math.Round(durationDiscountTotal, 2),
            GracePeriodDeduction: Math.Round(gracePeriodDeduction, 2),
            FinalTotal: Math.Round(finalTotal, 2),
            ModuleDetails: moduleDetails);
    }

    private static decimal GetQuantityDiscount(Domain.Entities.Module module, int userCount)
    {
        if (!module.QuantityDiscounts.Any())
            return 0;

        var applicableDiscount = module.QuantityDiscounts
            .Where(d => d.MinUsers <= userCount)
            .OrderByDescending(d => d.MinUsers)
            .FirstOrDefault();

        return applicableDiscount?.DiscountPercentage ?? 0;
    }
}

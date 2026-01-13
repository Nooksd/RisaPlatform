using Billing.Domain.DTOs.Responses;
using Billing.Domain.Entities;
using Mapster;

namespace Billing.Api.Configuration;

public static class MapsterConfiguration
{
    public static void Configure()
    {
        TypeAdapterConfig<Module, ModuleResponse>.NewConfig()
            .Map(dest => dest.QuantityDiscounts,
                 src => src.QuantityDiscounts.Select(d => new QuantityDiscountResponse(d.MinUsers, d.DiscountPercentage)));

        TypeAdapterConfig<Payment, PaymentResponse>.NewConfig();

        TypeAdapterConfig<Subscription, SubscriptionResponse>.NewConfig()
            .Map(dest => dest.Modules,
                 src => src.Modules.Select(m => new SubscriptionModuleResponse(
                     m.ModuleCode,
                     m.PricePerUserAtPurchase,
                     m.QuantityDiscountApplied,
                     m.TotalBeforeTimeDiscount)));
    }
}

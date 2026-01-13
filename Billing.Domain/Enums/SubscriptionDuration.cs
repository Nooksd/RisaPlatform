namespace Billing.Domain.Enums;

/// <summary>
/// Duração da assinatura com desconto aplicável
/// </summary>
public enum SubscriptionDuration
{
    /// <summary>
    /// 1 mês - sem desconto (0%)
    /// </summary>
    Monthly = 1,
    
    /// <summary>
    /// 3 meses - 25% de desconto
    /// </summary>
    Quarterly = 3,
    
    /// <summary>
    /// 12 meses - 40% de desconto
    /// </summary>
    Yearly = 12
}

public static class SubscriptionDurationExtensions
{
    public static decimal GetDiscountPercentage(this SubscriptionDuration duration) => duration switch
    {
        SubscriptionDuration.Monthly => 0m,
        SubscriptionDuration.Quarterly => 0.25m,
        SubscriptionDuration.Yearly => 0.40m,
        _ => 0m
    };
    
    public static int GetMonths(this SubscriptionDuration duration) => (int)duration;
    
    public static string GetDisplayName(this SubscriptionDuration duration) => duration switch
    {
        SubscriptionDuration.Monthly => "Mensal",
        SubscriptionDuration.Quarterly => "Trimestral",
        SubscriptionDuration.Yearly => "Anual",
        _ => "Desconhecido"
    };
}

namespace Billing.Domain.Entities;

/// <summary>
/// Módulo incluído em uma assinatura
/// </summary>
public sealed class SubscriptionModule : Entity
{
    public Guid SubscriptionId { get; private set; }
    public Subscription Subscription { get; private set; } = default!;
    
    public Guid ModuleId { get; private set; }
    public Module Module { get; private set; } = default!;
    
    /// <summary>
    /// Código do módulo (snapshot para caso o módulo mude)
    /// </summary>
    public string ModuleCode { get; private set; } = default!;
    
    /// <summary>
    /// Preço unitário por usuário no momento da compra
    /// </summary>
    public decimal PricePerUserAtPurchase { get; private set; }
    
    /// <summary>
    /// Desconto por quantidade aplicado
    /// </summary>
    public decimal QuantityDiscountApplied { get; private set; }
    
    /// <summary>
    /// Valor total deste módulo na assinatura (antes de descontos por tempo)
    /// </summary>
    public decimal TotalBeforeTimeDiscount { get; private set; }
    
    private SubscriptionModule() { }
    
    public SubscriptionModule(
        Guid subscriptionId,
        Guid moduleId,
        string moduleCode,
        decimal pricePerUserAtPurchase,
        decimal quantityDiscountApplied,
        int userCount,
        int months)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("SubscriptionId is required", nameof(subscriptionId));
        if (moduleId == Guid.Empty)
            throw new ArgumentException("ModuleId is required", nameof(moduleId));
        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("ModuleCode is required", nameof(moduleCode));
            
        SubscriptionId = subscriptionId;
        ModuleId = moduleId;
        ModuleCode = moduleCode;
        PricePerUserAtPurchase = pricePerUserAtPurchase;
        QuantityDiscountApplied = quantityDiscountApplied;
        
        // Preço com desconto por quantidade, multiplicado por usuários e meses
        var priceWithQuantityDiscount = pricePerUserAtPurchase * (1 - quantityDiscountApplied);
        TotalBeforeTimeDiscount = priceWithQuantityDiscount * userCount * months;
    }
}

using Billing.Domain.Enums;
using Billing.Domain.ValueObjects;

namespace Billing.Domain.Entities;

/// <summary>
/// Representa uma assinatura de módulos para um tenant
/// </summary>
public sealed class Subscription : Entity
{
    public Guid TenantBillingId { get; private set; }
    public TenantBilling TenantBilling { get; private set; } = default!;
    
    /// <summary>
    /// Duração escolhida (mensal, trimestral, anual)
    /// </summary>
    public SubscriptionDuration Duration { get; private set; }
    
    /// <summary>
    /// Quantidade de usuários contratados
    /// </summary>
    public int UserCount { get; private set; }
    
    /// <summary>
    /// Valor total pago (já com descontos aplicados)
    /// </summary>
    public decimal TotalAmount { get; private set; }
    
    /// <summary>
    /// Desconto por tempo aplicado
    /// </summary>
    public decimal DurationDiscountApplied { get; private set; }
    
    /// <summary>
    /// Data de início da assinatura
    /// </summary>
    public DateTime StartsAt { get; private set; }
    
    /// <summary>
    /// Data de expiração da assinatura
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// Dias descontados por uso de grace period
    /// </summary>
    public int GracePeriodDaysDeducted { get; private set; }
    
    /// <summary>
    /// Se a assinatura está ativa
    /// </summary>
    public bool IsActive { get; private set; } = true;
    
    /// <summary>
    /// ID do pagamento que ativou esta assinatura
    /// </summary>
    public Guid? PaymentId { get; private set; }
    public Payment? Payment { get; private set; }
    
    /// <summary>
    /// Módulos incluídos nesta assinatura
    /// </summary>
    public ICollection<SubscriptionModule> Modules { get; private set; } = [];
    
    private Subscription() { }
    
    public Subscription(
        Guid tenantBillingId,
        SubscriptionDuration duration,
        int userCount,
        decimal totalAmount,
        decimal durationDiscountApplied,
        int gracePeriodDaysToDeduct = 0)
    {
        if (tenantBillingId == Guid.Empty)
            throw new ArgumentException("TenantBillingId is required", nameof(tenantBillingId));
        if (userCount <= 0)
            throw new ArgumentException("UserCount must be greater than 0", nameof(userCount));
        if (totalAmount < 0)
            throw new ArgumentException("TotalAmount cannot be negative", nameof(totalAmount));
            
        TenantBillingId = tenantBillingId;
        Duration = duration;
        UserCount = userCount;
        TotalAmount = totalAmount;
        DurationDiscountApplied = durationDiscountApplied;
        GracePeriodDaysDeducted = gracePeriodDaysToDeduct;
        
        StartsAt = DateTime.UtcNow;
        
        // Calcula expiração considerando days deducted
        var months = duration.GetMonths();
        ExpiresAt = StartsAt.AddMonths(months).AddDays(-gracePeriodDaysToDeduct);
    }
    
    public void AddModule(SubscriptionModule module)
    {
        Modules.Add(module);
    }
    
    public void SetPayment(Guid paymentId)
    {
        PaymentId = paymentId;
        MarkAsUpdated();
    }
    
    public void Cancel()
    {
        IsActive = false;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Verifica se a assinatura está expirada
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    
    /// <summary>
    /// Retorna os códigos dos módulos ativos
    /// </summary>
    public IEnumerable<string> GetModuleCodes()
        => Modules.Select(m => m.ModuleCode);
}

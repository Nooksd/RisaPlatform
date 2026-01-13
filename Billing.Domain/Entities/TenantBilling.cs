using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

/// <summary>
/// Informações de billing de um tenant
/// </summary>
public sealed class TenantBilling : Entity
{
    /// <summary>
    /// ID do tenant no Auth service
    /// </summary>
    public Guid TenantId { get; private set; }
    
    /// <summary>
    /// ID do TenantAccount responsável pelo billing
    /// </summary>
    public Guid TenantAccountId { get; private set; }
    
    /// <summary>
    /// Status atual do tenant
    /// </summary>
    public TenantStatus Status { get; private set; } = TenantStatus.Active;
    
    /// <summary>
    /// Data em que o status atual foi definido
    /// </summary>
    public DateTime StatusChangedAt { get; private set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID do cliente no gateway de pagamento (Asaas customer_id)
    /// </summary>
    public string? PaymentGatewayCustomerId { get; private set; }
    
    /// <summary>
    /// Email para cobrança
    /// </summary>
    public string BillingEmail { get; private set; } = default!;
    
    /// <summary>
    /// CPF ou CNPJ para emissão de NF
    /// </summary>
    public string? TaxId { get; private set; }
    
    /// <summary>
    /// Nome/Razão Social para NF
    /// </summary>
    public string? LegalName { get; private set; }
    
    /// <summary>
    /// Se já usou o grace period no ciclo atual de atraso
    /// </summary>
    public bool GracePeriodUsedInCurrentCycle { get; private set; }
    
    /// <summary>
    /// Data em que o grace period foi solicitado (se ativo)
    /// </summary>
    public DateTime? GracePeriodRequestedAt { get; private set; }
    
    /// <summary>
    /// Dias de grace period a serem descontados do próximo pagamento
    /// </summary>
    public int GracePeriodDaysToDeduct { get; private set; }
    
    /// <summary>
    /// Navegação para assinaturas
    /// </summary>
    public ICollection<Subscription> Subscriptions { get; private set; } = [];
    
    /// <summary>
    /// Navegação para pagamentos
    /// </summary>
    public ICollection<Payment> Payments { get; private set; } = [];
    
    private TenantBilling() { }
    
    public TenantBilling(Guid tenantId, Guid tenantAccountId, string billingEmail)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (tenantAccountId == Guid.Empty)
            throw new ArgumentException("TenantAccountId is required", nameof(tenantAccountId));
        if (string.IsNullOrWhiteSpace(billingEmail))
            throw new ArgumentException("BillingEmail is required", nameof(billingEmail));
            
        TenantId = tenantId;
        TenantAccountId = tenantAccountId;
        BillingEmail = billingEmail;
    }
    
    public void SetPaymentGatewayCustomerId(string customerId)
    {
        PaymentGatewayCustomerId = customerId;
        MarkAsUpdated();
    }
    
    public void UpdateBillingInfo(string billingEmail, string? taxId, string? legalName)
    {
        if (string.IsNullOrWhiteSpace(billingEmail))
            throw new ArgumentException("BillingEmail is required", nameof(billingEmail));
            
        BillingEmail = billingEmail;
        TaxId = taxId;
        LegalName = legalName;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Solicita grace period (5 dias, 1x por ciclo de atraso)
    /// </summary>
    public bool RequestGracePeriod()
    {
        if (GracePeriodUsedInCurrentCycle)
            return false;
            
        if (Status != TenantStatus.Suspended)
            return false;
            
        GracePeriodUsedInCurrentCycle = true;
        GracePeriodRequestedAt = DateTime.UtcNow;
        GracePeriodDaysToDeduct = 5;
        ChangeStatus(TenantStatus.GracePeriod);
        
        return true;
    }
    
    /// <summary>
    /// Chamado quando um pagamento é confirmado
    /// </summary>
    public void OnPaymentConfirmed()
    {
        ChangeStatus(TenantStatus.Active);
        GracePeriodUsedInCurrentCycle = false;
        GracePeriodRequestedAt = null;
    }
    
    public void Suspend()
    {
        if (Status == TenantStatus.Active || Status == TenantStatus.GracePeriod)
        {
            ChangeStatus(TenantStatus.Suspended);
        }
    }
    
    public void MarkForDeletion()
    {
        if (Status == TenantStatus.Suspended)
        {
            ChangeStatus(TenantStatus.PendingDeletion);
        }
    }
    
    public void MarkAsDeleted()
    {
        ChangeStatus(TenantStatus.Deleted);
    }
    
    /// <summary>
    /// Reativa um tenant que estava pendente de exclusão (pagou)
    /// </summary>
    public void Reactivate()
    {
        if (Status == TenantStatus.PendingDeletion || Status == TenantStatus.Suspended)
        {
            ChangeStatus(TenantStatus.Active);
            GracePeriodUsedInCurrentCycle = false;
            GracePeriodRequestedAt = null;
        }
    }
    
    /// <summary>
    /// Consome os dias de grace period e retorna quantos dias descontar
    /// </summary>
    public int ConsumeGracePeriodDays()
    {
        var days = GracePeriodDaysToDeduct;
        GracePeriodDaysToDeduct = 0;
        MarkAsUpdated();
        return days;
    }
    
    private void ChangeStatus(TenantStatus newStatus)
    {
        Status = newStatus;
        StatusChangedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Retorna a assinatura ativa atual
    /// </summary>
    public Subscription? GetActiveSubscription()
        => Subscriptions
            .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefault();
}

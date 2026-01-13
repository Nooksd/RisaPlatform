namespace Billing.Domain.Enums;

public enum TenantStatus
{
    /// <summary>
    /// Pagamento em dia, acesso total
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Período de graça solicitado (5 dias, 1x por atraso)
    /// Os dias são descontados do próximo período pago
    /// </summary>
    GracePeriod = 2,
    
    /// <summary>
    /// 30 dias sem pagamento - acesso bloqueado, pode pagar
    /// </summary>
    Suspended = 3,
    
    /// <summary>
    /// 60 dias sem pagamento - última chance antes da exclusão
    /// </summary>
    PendingDeletion = 4,
    
    /// <summary>
    /// 90 dias sem pagamento - dados removidos de todos os serviços
    /// </summary>
    Deleted = 5
}

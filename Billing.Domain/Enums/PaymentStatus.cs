namespace Billing.Domain.Enums;

public enum PaymentStatus
{
    /// <summary>
    /// Aguardando pagamento (PIX gerado, boleto emitido)
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Pagamento confirmado
    /// </summary>
    Confirmed = 2,
    
    /// <summary>
    /// Pagamento falhou (cartão recusado, etc)
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Pagamento estornado
    /// </summary>
    Refunded = 4,
    
    /// <summary>
    /// Expirado (PIX/boleto não pago no prazo)
    /// </summary>
    Expired = 5,
    
    /// <summary>
    /// Cancelado pelo usuário
    /// </summary>
    Cancelled = 6
}

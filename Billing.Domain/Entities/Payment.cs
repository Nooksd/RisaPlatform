using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

/// <summary>
/// Representa um pagamento
/// </summary>
public sealed class Payment : Entity
{
    public Guid TenantBillingId { get; private set; }
    public TenantBilling TenantBilling { get; private set; } = default!;
    
    /// <summary>
    /// ID do pagamento no gateway (Asaas payment_id)
    /// </summary>
    public string? GatewayPaymentId { get; private set; }
    
    /// <summary>
    /// Método de pagamento escolhido
    /// </summary>
    public PaymentMethod Method { get; private set; }
    
    /// <summary>
    /// Status atual do pagamento
    /// </summary>
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    
    /// <summary>
    /// Valor do pagamento
    /// </summary>
    public decimal Amount { get; private set; }
    
    /// <summary>
    /// Data de vencimento (para boleto)
    /// </summary>
    public DateTime? DueDate { get; private set; }
    
    /// <summary>
    /// Data em que o pagamento foi confirmado
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }
    
    /// <summary>
    /// URL do boleto (se aplicável)
    /// </summary>
    public string? BoletoUrl { get; private set; }
    
    /// <summary>
    /// Código de barras do boleto (se aplicável)
    /// </summary>
    public string? BoletoBarcode { get; private set; }
    
    /// <summary>
    /// Código PIX copia-e-cola (se aplicável)
    /// </summary>
    public string? PixCopyPaste { get; private set; }
    
    /// <summary>
    /// QR Code PIX em base64 (se aplicável)
    /// </summary>
    public string? PixQrCode { get; private set; }
    
    /// <summary>
    /// Data de expiração do PIX
    /// </summary>
    public DateTime? PixExpiresAt { get; private set; }
    
    /// <summary>
    /// Últimos 4 dígitos do cartão (se aplicável)
    /// </summary>
    public string? CardLastFour { get; private set; }
    
    /// <summary>
    /// Bandeira do cartão (se aplicável)
    /// </summary>
    public string? CardBrand { get; private set; }
    
    /// <summary>
    /// Mensagem de erro (se falhou)
    /// </summary>
    public string? ErrorMessage { get; private set; }
    
    /// <summary>
    /// Dados da requisição do webhook (para auditoria)
    /// </summary>
    public string? WebhookPayload { get; private set; }
    
    /// <summary>
    /// Assinatura vinculada a este pagamento
    /// </summary>
    public Subscription? Subscription { get; private set; }
    
    private Payment() { }
    
    public Payment(
        Guid tenantBillingId,
        PaymentMethod method,
        decimal amount,
        DateTime? dueDate = null)
    {
        if (tenantBillingId == Guid.Empty)
            throw new ArgumentException("TenantBillingId is required", nameof(tenantBillingId));
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));
            
        TenantBillingId = tenantBillingId;
        Method = method;
        Amount = Math.Round(amount, 2);
        DueDate = dueDate;
    }
    
    public void SetGatewayPaymentId(string paymentId)
    {
        GatewayPaymentId = paymentId;
        MarkAsUpdated();
    }
    
    public void SetBoletoInfo(string url, string barcode, DateTime dueDate)
    {
        BoletoUrl = url;
        BoletoBarcode = barcode;
        DueDate = dueDate;
        MarkAsUpdated();
    }
    
    public void SetPixInfo(string copyPaste, string qrCode, DateTime expiresAt)
    {
        PixCopyPaste = copyPaste;
        PixQrCode = qrCode;
        PixExpiresAt = expiresAt;
        MarkAsUpdated();
    }
    
    public void SetCardInfo(string lastFour, string brand)
    {
        CardLastFour = lastFour;
        CardBrand = brand;
        MarkAsUpdated();
    }
    
    public void Confirm(string? webhookPayload = null)
    {
        Status = PaymentStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        WebhookPayload = webhookPayload;
        MarkAsUpdated();
    }
    
    public void Fail(string? errorMessage = null, string? webhookPayload = null)
    {
        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage;
        WebhookPayload = webhookPayload;
        MarkAsUpdated();
    }
    
    public void Expire()
    {
        Status = PaymentStatus.Expired;
        MarkAsUpdated();
    }
    
    public void Cancel()
    {
        Status = PaymentStatus.Cancelled;
        MarkAsUpdated();
    }
    
    public void Refund()
    {
        Status = PaymentStatus.Refunded;
        MarkAsUpdated();
    }
}

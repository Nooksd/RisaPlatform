using Billing.Domain.Enums;

namespace Billing.Domain.Interfaces.Services;

/// <summary>
/// Interface para integração com gateway de pagamento (Asaas)
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Cria um cliente no gateway
    /// </summary>
    Task<CreateCustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);

    /// <summary>
    /// Cria uma cobrança (PIX, Boleto ou Cartão)
    /// </summary>
    Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Consulta status de um pagamento
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId, CancellationToken ct = default);

    /// <summary>
    /// Cancela um pagamento pendente
    /// </summary>
    Task<bool> CancelPaymentAsync(string paymentId, CancellationToken ct = default);

    /// <summary>
    /// Valida assinatura do webhook
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature);
}

public record CreateCustomerRequest(
    string Name,
    string Email,
    string? CpfCnpj,
    string? Phone = null);

public record CreateCustomerResult(
    bool Success,
    string? CustomerId,
    string? ErrorMessage);

public record CreatePaymentRequest(
    string CustomerId,
    decimal Value,
    PaymentMethod Method,
    string Description,
    DateTime? DueDate = null,
    string? CreditCardToken = null,
    string? CreditCardHolderName = null,
    string? CreditCardHolderCpfCnpj = null);

public record CreatePaymentResult(
    bool Success,
    string? PaymentId,
    PaymentMethod Method,
    string? ErrorMessage,
    // PIX
    string? PixCopyPaste = null,
    string? PixQrCodeBase64 = null,
    DateTime? PixExpiresAt = null,
    // Boleto
    string? BoletoUrl = null,
    string? BoletoBarcode = null,
    DateTime? BoletoDueDate = null,
    // Card
    string? CardLastFour = null,
    string? CardBrand = null,
    string CheckoutUrl = null);

public record PaymentStatusResult(
    bool Success,
    string? Status,
    DateTime? ConfirmedAt,
    string? ErrorMessage);

using Billing.Domain.Interfaces.Services;
using Stripe;
using Stripe.Checkout;
using PaymentMethod = Billing.Domain.Enums.PaymentMethod;

namespace Billing.Api.Services;

public sealed class StripeSettings
{
    public string SecretKey { get; init; } = default!;
    public string PublishableKey { get; init; } = default!;
    public string WebhookSecret { get; init; } = default!;
    public string SuccessUrl { get; init; } = "https://app.risaplatform.com/billing/success";
    public string CancelUrl { get; init; } = "https://app.risaplatform.com/billing/cancel";
}

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(StripeSettings settings, ILogger<StripePaymentGateway> logger)
    {
        _settings = settings;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<CreateCustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Name = request.Name,
                Email = request.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "cpf_cnpj", request.CpfCnpj ?? "" }
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options, cancellationToken: ct);

            _logger.LogInformation("Stripe customer created: {CustomerId}", customer.Id);

            return new CreateCustomerResult(true, customer.Id, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer");
            return new CreateCustomerResult(false, null, ex.Message);
        }
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(Domain.Interfaces.Services.CreatePaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            // Stripe usa Checkout Session para PIX/Boleto/Card
            var paymentMethodTypes = request.Method switch
            {
                PaymentMethod.Pix => new List<string> { "pix" },
                PaymentMethod.Boleto => new List<string> { "boleto" },
                PaymentMethod.CreditCard => new List<string> { "card" },
                _ => ["card"]
            };

            var sessionOptions = new SessionCreateOptions
            {
                Customer = request.CustomerId,
                PaymentMethodTypes = paymentMethodTypes,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "brl",
                            UnitAmount = (long)(request.Value * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.Description
                            }
                        },
                        Quantity = 1
                    }
                ],
                Mode = "payment",
                SuccessUrl = _settings.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _settings.CancelUrl,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Metadata = new Dictionary<string, string>
                {
                    { "description", request.Description ?? "" }
                }
            };

            if (request.Method == PaymentMethod.Boleto)
            {
                sessionOptions.PaymentMethodOptions = new SessionPaymentMethodOptionsOptions
                {
                    Boleto = new SessionPaymentMethodOptionsBoletoOptions
                    {
                        ExpiresAfterDays = 3
                    }
                };
            }

            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: ct);

            _logger.LogInformation("Stripe checkout session created: {SessionId}, Method: {Method}", session.Id, request.Method);

            return new CreatePaymentResult(
                Success: true,
                PaymentId: session.Id,
                Method: request.Method,
                ErrorMessage: null,
                PixCopyPaste: request.Method == PaymentMethod.Pix ? session.Url : null,
                PixQrCodeBase64: null,
                PixExpiresAt: request.Method == PaymentMethod.Pix ? session.ExpiresAt : null,
                BoletoUrl: request.Method == PaymentMethod.Boleto ? session.Url : null,
                BoletoBarcode: null,
                BoletoDueDate: request.Method == PaymentMethod.Boleto ? DateTime.UtcNow.AddDays(3) : null,
                CardLastFour: null,
                CardBrand: null,
                CheckoutUrl: session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment");
            return new CreatePaymentResult(
                Success: false,
                PaymentId: null,
                Method: request.Method,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: ct);

            var status = session.PaymentStatus switch
            {
                "paid" => "CONFIRMED",
                "unpaid" => "PENDING",
                "no_payment_required" => "CONFIRMED",
                _ => session.Status
            };

            return new PaymentStatusResult(
                Success: true,
                Status: status,
                ConfirmedAt: session.PaymentStatus == "paid" ? DateTime.UtcNow : null,
                ErrorMessage: null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error getting Stripe payment status");
            return new PaymentStatusResult(false, null, null, ex.Message);
        }
    }

    public async Task<bool> CancelPaymentAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var service = new SessionService();
            await service.ExpireAsync(sessionId, cancellationToken: ct);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error canceling Stripe session");
            return false;
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        try
        {
            EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// Estender o CreatePaymentResult para incluir CheckoutUrl
public static class CreatePaymentResultExtensions
{
    public static string? GetCheckoutUrl(this CreatePaymentResult result)
    {
        // Para Stripe, PixCopyPaste ou BoletoUrl contém a URL do checkout
        return result.PixCopyPaste ?? result.BoletoUrl;
    }
}

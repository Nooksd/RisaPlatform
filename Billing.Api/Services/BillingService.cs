using Billing.Domain.DTOs.Requests;
using Billing.Domain.DTOs.Responses;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Domain.Interfaces.Repositories;
using Billing.Domain.Interfaces.Services;
using Shared.Contracts.Billing;
using Shared.Kernel.Primitives;

namespace Billing.Api.Services;

public interface IBillingService
{
    Task<TenantBillingResponse?> GetTenantBillingAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantBillingResponse> CreateTenantBillingAsync(CreateTenantBillingRequest request, CancellationToken ct = default);
    Task<TenantBillingResponse> UpdateBillingInfoAsync(Guid tenantId, UpdateBillingInfoRequest request, CancellationToken ct = default);
    Task<PaymentResponse> CreatePaymentAsync(Domain.DTOs.Requests.CreatePaymentRequest request, CancellationToken ct = default);
    Task<GracePeriodResponse> RequestGracePeriodAsync(Guid tenantId, CancellationToken ct = default);
    Task ProcessWebhookAsync(string gatewayPaymentId, string status, CancellationToken ct = default);
}

public sealed class BillingService : IBillingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IEmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IUnitOfWork unitOfWork,
        IPricingService pricingService,
        IPaymentGateway paymentGateway,
        IEmailService emailService,
        IEventBus eventBus,
        ILogger<BillingService> logger)
    {
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _paymentGateway = paymentGateway;
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<TenantBillingResponse?> GetTenantBillingAsync(Guid tenantId, CancellationToken ct = default)
    {
        var billing = await _unitOfWork.TenantBillings.GetWithSubscriptionsAsync(tenantId, ct);
        return billing is null ? null : MapToResponse(billing);
    }

    public async Task<TenantBillingResponse> CreateTenantBillingAsync(CreateTenantBillingRequest request, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.TenantBillings.GetByTenantIdAsync(request.TenantId, ct);
        if (existing is not null)
            throw new InvalidOperationException("TenantBilling já existe para este tenant");

        var billing = new TenantBilling(request.TenantId, request.TenantAccountId, request.BillingEmail);

        if (!string.IsNullOrEmpty(request.TaxId) || !string.IsNullOrEmpty(request.LegalName))
        {
            billing.UpdateBillingInfo(request.BillingEmail, request.TaxId, request.LegalName);
        }

        // Criar cliente no Stripe
        var customerResult = await _paymentGateway.CreateCustomerAsync(
            new CreateCustomerRequest(
                request.LegalName ?? "Cliente",
                request.BillingEmail,
                request.TaxId),
            ct);

        if (customerResult.Success && customerResult.CustomerId is not null)
        {
            billing.SetPaymentGatewayCustomerId(customerResult.CustomerId);
        }

        await _unitOfWork.TenantBillings.AddAsync(billing, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("TenantBilling created for tenant {TenantId}", request.TenantId);

        return MapToResponse(billing);
    }

    public async Task<TenantBillingResponse> UpdateBillingInfoAsync(Guid tenantId, UpdateBillingInfoRequest request, CancellationToken ct = default)
    {
        var billing = await _unitOfWork.TenantBillings.GetByTenantIdAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("TenantBilling não encontrado");

        billing.UpdateBillingInfo(request.BillingEmail, request.TaxId, request.LegalName);

        _unitOfWork.TenantBillings.Update(billing);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(billing);
    }

    public async Task<PaymentResponse> CreatePaymentAsync(Domain.DTOs.Requests.CreatePaymentRequest request, CancellationToken ct = default)
    {
        var billing = await _unitOfWork.TenantBillings.GetWithSubscriptionsAsync(request.TenantId, ct)
            ?? throw new KeyNotFoundException("TenantBilling não encontrado");

        // Calcular preço
        var graceDaysToDeduct = billing.GracePeriodDaysToDeduct;
        var pricingResult = await _pricingService.CalculatePriceAsync(
            new PricingRequest(request.ModuleCodes, request.UserCount, request.Duration, graceDaysToDeduct),
            ct);

        if (!pricingResult.Success)
            throw new InvalidOperationException(pricingResult.ErrorMessage);

        // Garantir que tem customer no Stripe
        if (string.IsNullOrEmpty(billing.PaymentGatewayCustomerId))
        {
            var customerResult = await _paymentGateway.CreateCustomerAsync(
                new CreateCustomerRequest(
                    billing.LegalName ?? "Cliente",
                    billing.BillingEmail,
                    billing.TaxId),
                ct);

            if (!customerResult.Success)
                throw new InvalidOperationException($"Erro ao criar cliente: {customerResult.ErrorMessage}");

            billing.SetPaymentGatewayCustomerId(customerResult.CustomerId!);
            _unitOfWork.TenantBillings.Update(billing);
        }

        // Criar Stripe Checkout Session
        var paymentResult = await _paymentGateway.CreatePaymentAsync(
            new Domain.Interfaces.Services.CreatePaymentRequest(
                billing.PaymentGatewayCustomerId!,
                pricingResult.FinalTotal,
                request.Method,
                $"Assinatura RisaPlatform - {request.Duration.GetDisplayName()}",
                request.Method == PaymentMethod.Boleto ? DateTime.UtcNow.AddDays(3) : null),
            ct);

        if (!paymentResult.Success)
            throw new InvalidOperationException($"Erro ao criar pagamento: {paymentResult.ErrorMessage}");

        // Criar entidade Payment
        var payment = new Payment(billing.Id, request.Method, pricingResult.FinalTotal);
        payment.SetGatewayPaymentId(paymentResult.PaymentId!);

        // Stripe retorna URL do checkout
        if (!string.IsNullOrEmpty(paymentResult.CheckoutUrl))
        {
            // Usamos PixCopyPaste para armazenar a CheckoutUrl temporariamente
            payment.SetPixInfo(
                paymentResult.CheckoutUrl,
                "",
                paymentResult.PixExpiresAt ?? DateTime.UtcNow.AddHours(24));
        }

        // Criar subscription pendente (será ativada após confirmação do webhook)
        var subscription = new Subscription(
            billing.Id,
            request.Duration,
            request.UserCount,
            pricingResult.FinalTotal,
            request.Duration.GetDiscountPercentage(),
            graceDaysToDeduct);

        var modules = await _unitOfWork.Modules.GetByCodesAsync(request.ModuleCodes, ct);
        foreach (var module in modules)
        {
            var detail = pricingResult.ModuleDetails.First(d => d.ModuleCode == module.Code);
            var subModule = new SubscriptionModule(
                subscription.Id,
                module.Id,
                module.Code,
                detail.PricePerUser,
                detail.QuantityDiscountPercentage,
                request.UserCount,
                request.Duration.GetMonths());
            subscription.AddModule(subModule);
        }

        await _unitOfWork.Payments.AddAsync(payment, ct);
        await _unitOfWork.Subscriptions.AddAsync(subscription, ct);
        _unitOfWork.TenantBillings.Update(billing);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment created for tenant {TenantId}: {PaymentId}, Method: {Method}, Amount: R${Amount:N2}",
            billing.TenantId, payment.Id, request.Method, pricingResult.FinalTotal);

        return MapToPaymentResponse(payment, paymentResult.CheckoutUrl);
    }

    public async Task<GracePeriodResponse> RequestGracePeriodAsync(Guid tenantId, CancellationToken ct = default)
    {
        var billing = await _unitOfWork.TenantBillings.GetWithSubscriptionsAsync(tenantId, ct)
            ?? throw new KeyNotFoundException("TenantBilling não encontrado");

        if (billing.GracePeriodUsedInCurrentCycle)
        {
            return new GracePeriodResponse(false, null, "Grace period já foi utilizado neste ciclo de atraso");
        }

        if (billing.Status != TenantStatus.Suspended)
        {
            return new GracePeriodResponse(false, null, "Grace period só pode ser solicitado quando a conta está suspensa");
        }

        var granted = billing.RequestGracePeriod();
        if (!granted)
        {
            return new GracePeriodResponse(false, null, "Não foi possível conceder o grace period");
        }

        _unitOfWork.TenantBillings.Update(billing);
        await _unitOfWork.SaveChangesAsync(ct);

        var expiresAt = DateTime.UtcNow.AddDays(5);
        var activeSubscription = billing.GetActiveSubscription();
        var modules = activeSubscription?.GetModuleCodes().ToArray() ?? [];

        // Emitir evento TenantGracePeriodGrantedEvent
        await _eventBus.PublishAsync(new TenantGracePeriodGrantedEvent(
            tenantId,
            DateTime.UtcNow,
            expiresAt,
            modules), ct);

        // Enviar email
        await _emailService.SendGracePeriodGrantedAsync(
            new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", tenantId),
            expiresAt,
            ct);

        _logger.LogInformation("Grace period granted for tenant {TenantId}, expires at {ExpiresAt}", tenantId, expiresAt);

        return new GracePeriodResponse(true, expiresAt, "Grace period concedido. Os 5 dias serão descontados do próximo pagamento.");
    }

    public async Task ProcessWebhookAsync(string gatewayPaymentId, string status, CancellationToken ct = default)
    {
        var payment = await _unitOfWork.Payments.GetByGatewayPaymentIdAsync(gatewayPaymentId, ct);
        if (payment is null)
        {
            _logger.LogWarning("Payment not found for gateway ID: {GatewayPaymentId}", gatewayPaymentId);
            return;
        }

        var billing = await _unitOfWork.TenantBillings.GetWithSubscriptionsAsync(payment.TenantBilling.TenantId, ct);
        if (billing is null) return;

        switch (status.ToUpperInvariant())
        {
            case "CONFIRMED":
            case "PAID":
                await HandlePaymentConfirmed(payment, billing, ct);
                break;
            case "EXPIRED":
                payment.Expire();
                break;
            case "FAILED":
                payment.Fail(status);
                break;
        }

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandlePaymentConfirmed(Payment payment, TenantBilling billing, CancellationToken ct)
    {
        if (payment.Status == PaymentStatus.Confirmed)
            return;

        payment.Confirm();

        // Encontrar subscription pendente
        var subscription = billing.Subscriptions
            .FirstOrDefault(s => s.PaymentId == null && !s.IsActive);

        if (subscription is not null)
        {
            subscription.SetPayment(payment.Id);
        }

        // Consumir grace period days
        billing.ConsumeGracePeriodDays();

        billing.OnPaymentConfirmed();
        _unitOfWork.TenantBillings.Update(billing);

        var modules = subscription?.GetModuleCodes().ToArray() ?? [];
        var userCount = subscription?.UserCount ?? 0;
        var expiresAt = subscription?.ExpiresAt ?? DateTime.UtcNow.AddMonths(1);

        // Emitir evento TenantPaymentConfirmedEvent
        await _eventBus.PublishAsync(new TenantPaymentConfirmedEvent(
            billing.TenantId,
            DateTime.UtcNow,
            expiresAt,
            userCount,
            modules), ct);

        // Enviar email
        await _emailService.SendPaymentConfirmedAsync(
            new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", billing.TenantId),
            payment.Amount,
            expiresAt,
            ct);

        _logger.LogInformation("Payment confirmed for tenant {TenantId}: {PaymentId}", billing.TenantId, payment.Id);
    }

    private static TenantBillingResponse MapToResponse(TenantBilling billing)
    {
        var activeSubscription = billing.GetActiveSubscription();

        return new TenantBillingResponse(
            billing.Id,
            billing.TenantId,
            billing.TenantAccountId,
            billing.Status,
            billing.StatusChangedAt,
            billing.BillingEmail,
            billing.TaxId,
            billing.LegalName,
            billing.GracePeriodUsedInCurrentCycle,
            activeSubscription is null ? null : new SubscriptionResponse(
                activeSubscription.Id,
                activeSubscription.Duration,
                activeSubscription.UserCount,
                activeSubscription.TotalAmount,
                activeSubscription.StartsAt,
                activeSubscription.ExpiresAt,
                activeSubscription.IsActive,
                activeSubscription.Modules.Select(m => new SubscriptionModuleResponse(
                    m.ModuleCode,
                    m.PricePerUserAtPurchase,
                    m.QuantityDiscountApplied,
                    m.TotalBeforeTimeDiscount))));
    }

    private static PaymentResponse MapToPaymentResponse(Payment payment, string? checkoutUrl = null)
    {
        return new PaymentResponse(
            payment.Id,
            payment.GatewayPaymentId,
            payment.Method,
            payment.Status,
            payment.Amount,
            payment.CreatedAt,
            payment.DueDate,
            payment.ConfirmedAt,
            checkoutUrl ?? payment.PixCopyPaste, // CheckoutUrl ou fallback
            payment.PixCopyPaste,
            payment.PixQrCode,
            payment.PixExpiresAt,
            payment.BoletoUrl,
            payment.BoletoBarcode,
            payment.CardLastFour,
            payment.CardBrand);
    }
}

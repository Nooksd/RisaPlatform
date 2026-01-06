namespace Gateway.Api.Handlers;

using Gateway.Api.Models;
using Gateway.Api.Services.Interfaces;
using Shared.Contracts.Billing;
using Shared.Kernel.Primitives;

public sealed class TenantPaymentConfirmedHandler : IIntegrationEventHandler<TenantPaymentConfirmedEvent>
{
    private readonly ISubscriptionCache _subscriptionCache;
    private readonly ILogger<TenantPaymentConfirmedHandler> _logger;

    public TenantPaymentConfirmedHandler(
        ISubscriptionCache subscriptionCache,
        ILogger<TenantPaymentConfirmedHandler> logger)
    {
        _subscriptionCache = subscriptionCache;
        _logger = logger;
    }

    public async Task HandleAsync(TenantPaymentConfirmedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processing payment confirmation for tenant {TenantId}",
            @event.TenantId);

        var subscription = new TenantSubscription
        {
            Modules = @event.AllowedModules,
            UserLimit = 0,
            ExpiresAt = @event.PayedAt.Add(@event.Duration),
            PayedAt = @event.PayedAt
        };

        await _subscriptionCache.SetAsync(@event.TenantId, subscription, ct);

        _logger.LogInformation(
            "Successfully updated subscription for tenant {TenantId}. Expires at {ExpiresAt}",
            @event.TenantId,
            subscription.ExpiresAt);
    }
}
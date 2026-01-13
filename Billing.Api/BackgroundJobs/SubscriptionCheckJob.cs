using Billing.Api.Services;
using Billing.Domain.Enums;
using Billing.Domain.Interfaces.Repositories;
using Billing.Domain.Interfaces.Services;
using Shared.Contracts.Billing;

namespace Billing.Api.BackgroundJobs;

/// <summary>
/// Job que roda diariamente para verificar status das assinaturas
/// e processar mudanças de estado (suspensão, exclusão)
/// </summary>
public sealed class SubscriptionCheckJob(
    IServiceProvider serviceProvider,
    ILogger<SubscriptionCheckJob> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<SubscriptionCheckJob> _logger = logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    private const int SuspensionDaysAfterExpiry = 30;
    private const int DeletionDaysAfterExpiry = 90;
    private const int GracePeriodDays = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionCheckJob started");

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubscriptionCheckJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessSubscriptionsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting subscription check...");

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deletionPublisher = scope.ServiceProvider.GetRequiredService<IDeletionEventPublisher>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // 1. Verificar grace periods expirados → Suspender
        await ProcessExpiredGracePeriods(unitOfWork, emailService, ct);

        // 2. Verificar assinaturas expiradas que devem ser suspensas
        await ProcessExpiredSubscriptions(unitOfWork, emailService, ct);

        // 3. Verificar tenants suspensos que devem ser marcados para exclusão
        await ProcessSuspendedTenants(unitOfWork, emailService, ct);

        // 4. Verificar tenants pendentes de exclusão → Deletar dados
        await ProcessPendingDeletions(unitOfWork, deletionPublisher, emailService, ct);

        // 5. Enviar lembretes de vencimento (3 dias antes)
        await SendExpirationReminders(unitOfWork, emailService, ct);

        _logger.LogInformation("Subscription check completed");
    }

    private async Task ProcessExpiredGracePeriods(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        CancellationToken ct)
    {
        var expiredGracePeriods = await unitOfWork.TenantBillings.GetPendingGracePeriodExpirationAsync(ct);

        foreach (var billing in expiredGracePeriods)
        {
            _logger.LogInformation("Grace period expired for tenant {TenantId}", billing.TenantId);

            billing.Suspend();
            unitOfWork.TenantBillings.Update(billing);

            await emailService.SendSuspensionNoticeAsync(
                new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", billing.TenantId),
                ct);
        }

        if (expiredGracePeriods.Any())
        {
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Processed {Count} expired grace periods", expiredGracePeriods.Count());
        }
    }

    private async Task ProcessExpiredSubscriptions(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        CancellationToken ct)
    {
        var pendingSuspension = await unitOfWork.TenantBillings.GetPendingSuspensionAsync(SuspensionDaysAfterExpiry, ct);

        foreach (var billing in pendingSuspension)
        {
            _logger.LogInformation("Suspending tenant {TenantId} - subscription expired", billing.TenantId);

            billing.Suspend();
            unitOfWork.TenantBillings.Update(billing);

            await emailService.SendSuspensionNoticeAsync(
                new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", billing.TenantId),
                ct);
        }

        if (pendingSuspension.Any())
        {
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Suspended {Count} tenants", pendingSuspension.Count());
        }
    }

    private async Task ProcessSuspendedTenants(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        CancellationToken ct)
    {
        var pendingDeletion = await unitOfWork.TenantBillings.GetPendingDeletionAsync(60, ct);

        foreach (var billing in pendingDeletion.Where(b => b.Status == TenantStatus.Suspended))
        {
            _logger.LogInformation("Marking tenant {TenantId} for deletion", billing.TenantId);

            billing.MarkForDeletion();
            unitOfWork.TenantBillings.Update(billing);

            await emailService.SendDeletionWarningAsync(
                new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", billing.TenantId),
                30,
                ct);
        }

        if (pendingDeletion.Any())
        {
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Marked {Count} tenants for deletion", pendingDeletion.Count());
        }
    }

    private async Task ProcessPendingDeletions(
        IUnitOfWork unitOfWork,
        IDeletionEventPublisher deletionPublisher,
        IEmailService emailService,
        CancellationToken ct)
    {
        var readyForDeletion = await unitOfWork.TenantBillings.GetPendingDeletionAsync(DeletionDaysAfterExpiry, ct);

        foreach (var billing in readyForDeletion.Where(b => b.Status == TenantStatus.PendingDeletion))
        {
            _logger.LogWarning("Initiating data deletion for tenant {TenantId}", billing.TenantId);

            billing.MarkAsDeleted();
            unitOfWork.TenantBillings.Update(billing);

            await deletionPublisher.PublishDeletionCommandAsync(new TenantDataDeletionCommand(
                billing.TenantId,
                DateTime.UtcNow,
                "Non-payment for 90+ days"), ct);

            await emailService.SendDeletionConfirmationAsync(
                new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", billing.TenantId),
                ct);
        }

        if (readyForDeletion.Any())
        {
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Initiated deletion for {Count} tenants", readyForDeletion.Count());
        }
    }

    private async Task SendExpirationReminders(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        CancellationToken ct)
    {
        var expiringSoon = await unitOfWork.Subscriptions.GetExpiringInDaysAsync(3, ct);

        foreach (var subscription in expiringSoon)
        {
            var billing = subscription.TenantBilling;
            if (billing.Status != TenantStatus.Active)
                continue;

            _logger.LogInformation(
                "Sending expiration reminder for tenant {TenantId}, expires at {ExpiresAt}",
                billing.TenantId, subscription.ExpiresAt);

            await emailService.SendPaymentDueReminderAsync(
                new EmailRecipient(billing.BillingEmail, billing.LegalName ?? "", billing.TenantId),
                subscription.ExpiresAt,
                subscription.TotalAmount,
                ct);
        }
    }
}

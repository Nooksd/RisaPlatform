using Billing.Domain.Interfaces.Services;

namespace Billing.Api.Services;

/// <summary>
/// Mock de serviço de email - apenas loga as chamadas
/// Será substituído por implementação real futuramente
/// </summary>
public sealed class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    private readonly ILogger<MockEmailService> _logger = logger;

    public Task<bool> SendPaymentDueReminderAsync(EmailRecipient recipient, DateTime dueDate, decimal amount, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] PaymentDueReminder to {Email} ({Name}) - TenantId: {TenantId}, DueDate: {DueDate}, Amount: R${Amount:N2}",
            recipient.Email, recipient.Name, recipient.TenantId, dueDate, amount);
        return Task.FromResult(true);
    }

    public Task<bool> SendPaymentOverdueAsync(EmailRecipient recipient, int daysOverdue, decimal amount, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] PaymentOverdue to {Email} ({Name}) - TenantId: {TenantId}, DaysOverdue: {Days}, Amount: R${Amount:N2}",
            recipient.Email, recipient.Name, recipient.TenantId, daysOverdue, amount);
        return Task.FromResult(true);
    }

    public Task<bool> SendSuspensionNoticeAsync(EmailRecipient recipient, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] SuspensionNotice to {Email} ({Name}) - TenantId: {TenantId}",
            recipient.Email, recipient.Name, recipient.TenantId);
        return Task.FromResult(true);
    }

    public Task<bool> SendDeletionWarningAsync(EmailRecipient recipient, int daysUntilDeletion, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] DeletionWarning to {Email} ({Name}) - TenantId: {TenantId}, DaysUntilDeletion: {Days}",
            recipient.Email, recipient.Name, recipient.TenantId, daysUntilDeletion);
        return Task.FromResult(true);
    }

    public Task<bool> SendDeletionConfirmationAsync(EmailRecipient recipient, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] DeletionConfirmation to {Email} ({Name}) - TenantId: {TenantId}",
            recipient.Email, recipient.Name, recipient.TenantId);
        return Task.FromResult(true);
    }

    public Task<bool> SendGracePeriodGrantedAsync(EmailRecipient recipient, DateTime expiresAt, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] GracePeriodGranted to {Email} ({Name}) - TenantId: {TenantId}, ExpiresAt: {ExpiresAt}",
            recipient.Email, recipient.Name, recipient.TenantId, expiresAt);
        return Task.FromResult(true);
    }

    public Task<bool> SendPaymentConfirmedAsync(EmailRecipient recipient, decimal amount, DateTime validUntil, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] PaymentConfirmed to {Email} ({Name}) - TenantId: {TenantId}, Amount: R${Amount:N2}, ValidUntil: {ValidUntil}",
            recipient.Email, recipient.Name, recipient.TenantId, amount, validUntil);
        return Task.FromResult(true);
    }

    public Task<bool> SendPaymentFailedAsync(EmailRecipient recipient, string reason, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] PaymentFailed to {Email} ({Name}) - TenantId: {TenantId}, Reason: {Reason}",
            recipient.Email, recipient.Name, recipient.TenantId, reason);
        return Task.FromResult(true);
    }
}

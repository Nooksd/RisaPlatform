namespace Billing.Domain.Interfaces.Services;

/// <summary>
/// Interface para envio de emails (mock por enquanto)
/// </summary>
public interface IEmailService
{
    Task<bool> SendPaymentDueReminderAsync(EmailRecipient recipient, DateTime dueDate, decimal amount, CancellationToken ct = default);
    Task<bool> SendPaymentOverdueAsync(EmailRecipient recipient, int daysOverdue, decimal amount, CancellationToken ct = default);
    Task<bool> SendSuspensionNoticeAsync(EmailRecipient recipient, CancellationToken ct = default);
    Task<bool> SendDeletionWarningAsync(EmailRecipient recipient, int daysUntilDeletion, CancellationToken ct = default);
    Task<bool> SendDeletionConfirmationAsync(EmailRecipient recipient, CancellationToken ct = default);
    Task<bool> SendGracePeriodGrantedAsync(EmailRecipient recipient, DateTime expiresAt, CancellationToken ct = default);
    Task<bool> SendPaymentConfirmedAsync(EmailRecipient recipient, decimal amount, DateTime validUntil, CancellationToken ct = default);
    Task<bool> SendPaymentFailedAsync(EmailRecipient recipient, string reason, CancellationToken ct = default);
}

public record EmailRecipient(
    string Email,
    string Name,
    Guid TenantId);

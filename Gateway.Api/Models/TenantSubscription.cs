namespace Gateway.Api.Models;

public sealed class TenantSubscription
{
    public string[] Modules { get; set; } = [];
    public int UserLimit { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime PayedAt { get; set; }
}
using Auth.Domain.Enums;

namespace Auth.Domain.Entities;

/// <summary>
/// Refresh token para controle de sessões
/// Usado para invalidar sessões e forçar logout
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public AccountType AccountType { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    // Metadata
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private RefreshToken() { } // EF Core

    public static RefreshToken Create(
        string token,
        Guid userId,
        AccountType accountType,
        Guid tenantId,
        TimeSpan expiresIn,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            AccountType = accountType,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiresIn),
            IsRevoked = false,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsValid => !IsRevoked && !IsExpired;

    public void Revoke(string reason)
    {
        if (IsRevoked)
            throw new InvalidOperationException("Token already revoked");

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
    }
}

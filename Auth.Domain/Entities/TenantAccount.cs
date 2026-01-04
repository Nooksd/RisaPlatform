using Auth.Domain.Enums;
using Auth.Domain.ValueObjects;

namespace Auth.Domain.Entities;

/// <summary>
/// Conta principal do tenant - sempre tem acesso total
/// </summary>
public sealed class TenantAccount
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Email Email { get; private set; } = default!;
    public PasswordHash? PasswordHash { get; private set; }
    public string? OAuthId { get; private set; }
    public OAuthProvider OAuthProvider { get; private set; }
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private TenantAccount() { } // EF Core

    public static TenantAccount CreateWithPassword(
        Guid tenantId,
        Email email,
        PasswordHash passwordHash,
        string name)
    {
        return new TenantAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            OAuthProvider = OAuthProvider.None,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static TenantAccount CreateWithOAuth(
        Guid tenantId,
        Email email,
        string oauthId,
        OAuthProvider provider,
        string name)
    {
        return new TenantAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            OAuthId = oauthId,
            OAuthProvider = provider,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdatePassword(PasswordHash newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
    }
}
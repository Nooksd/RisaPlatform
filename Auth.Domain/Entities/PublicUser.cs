using Auth.Domain.Enums;
using Auth.Domain.ValueObjects;

namespace Auth.Domain.Entities;

/// <summary>
/// Usuário público de um módulo específico (clientes do cliente)
/// Isolado por tenant e módulo
/// </summary>
public sealed class PublicUser
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Module { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PasswordHash? PasswordHash { get; private set; }
    public string? OAuthId { get; private set; }
    public OAuthProvider OAuthProvider { get; private set; }
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private PublicUser() { }

    public static PublicUser CreateWithPassword(
        Guid tenantId,
        string module,
        Email email,
        PasswordHash passwordHash,
        string name)
    {
        ValidateModule(module);

        return new PublicUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Module = module,
            Email = email,
            PasswordHash = passwordHash,
            OAuthProvider = OAuthProvider.None,
            Name = name,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PublicUser CreateWithOAuth(
        Guid tenantId,
        string module,
        Email email,
        string oauthId,
        OAuthProvider provider,
        string name)
    {
        ValidateModule(module);

        return new PublicUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Module = module,
            Email = email,
            OAuthId = oauthId,
            OAuthProvider = provider,
            Name = name,
            IsActive = true,
            IsDeleted = false,
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
        if (IsDeleted)
            throw new InvalidOperationException("Cannot activate deleted user");

        IsActive = true;
    }

    public void UpdatePassword(PasswordHash newPasswordHash)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update password of deleted user");

        PasswordHash = newPasswordHash;
    }

    public void UpdateName(string name)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update name of deleted user");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
    }

    public void Delete(Guid deletedBy)
    {
        if (IsDeleted)
            throw new InvalidOperationException("User already deleted");

        IsDeleted = true;
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    private static void ValidateModule(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module cannot be empty", nameof(module));

        if (!Enum.TryParse<SystemModule>(module, true, out _))
            throw new ArgumentException($"Invalid module: {module}", nameof(module));
    }
}
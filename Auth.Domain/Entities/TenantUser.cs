using Auth.Domain.ValueObjects;

namespace Auth.Domain.Entities;

/// <summary>
/// Usuário interno do tenant com permissões granulares por módulo
/// </summary>
public sealed class TenantUser
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Email Email { get; private set; } = default!;
    public PasswordHash PasswordHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public Guid CreatedBy { get; private set; } // ID de quem criou (TenantAccount ou outro TenantUser)

    // Navigation
    public ICollection<ModuleAccess> ModuleAccesses { get; private set; } = new List<ModuleAccess>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private TenantUser() { } // EF Core

    public static TenantUser Create(
        Guid tenantId,
        Email email,
        PasswordHash passwordHash,
        string name,
        Guid createdBy,
        ModuleAccessCollection moduleAccesses)
    {
        var user = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        foreach (var (module, level) in moduleAccesses.AccessLevels)
        {
            if (level > 0) // Só adiciona se tiver algum acesso
            {
                user.ModuleAccesses.Add(ModuleAccess.Create(user.Id, module, level));
            }
        }

        return user;
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

    public void UpdateModuleAccesses(ModuleAccessCollection newAccesses)
    {
        ModuleAccesses.Clear();

        foreach (var (module, level) in newAccesses.AccessLevels)
        {
            if (level > 0)
            {
                ModuleAccesses.Add(ModuleAccess.Create(Id, module, level));
            }
        }
    }

    public ModuleAccessCollection GetModuleAccesses()
    {
        var dict = ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel);
        return ModuleAccessCollection.Create(dict);
    }
}
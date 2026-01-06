namespace Auth.Domain.Entities;

/// <summary>
/// Representa o nível de acesso de um usuário a um módulo
/// </summary>
public sealed class ModuleAccess
{
    public Guid Id { get; private set; }
    public Guid TenantUserId { get; private set; }
    public string Module { get; private set; } = default!;
    public int AccessLevel { get; private set; }

    public TenantUser TenantUser { get; private set; } = default!;

    private ModuleAccess() { }

    public static ModuleAccess Create(Guid tenantUserId, string module, int accessLevel)
    {
        if (accessLevel < 0 || accessLevel > 3)
            throw new ArgumentException($"Invalid access level: {accessLevel}", nameof(accessLevel));

        return new ModuleAccess
        {
            Id = Guid.NewGuid(),
            TenantUserId = tenantUserId,
            Module = module,
            AccessLevel = accessLevel
        };
    }

    public void UpdateAccessLevel(int newLevel)
    {
        if (newLevel < 0 || newLevel > 3)
            throw new ArgumentException($"Invalid access level: {newLevel}", nameof(newLevel));

        AccessLevel = newLevel;
    }
}


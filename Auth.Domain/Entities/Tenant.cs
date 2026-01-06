namespace Auth.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; private set; }
    public ValueObjects.Domain Domain { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public TenantAccount TenantAccount { get; private set; } = default!;
    public ICollection<TenantUser> Users { get; private set; } = [];
    public ICollection<PublicUser> PublicUsers { get; private set; } = [];

    private Tenant() { }

    public static Tenant Create(
        ValueObjects.Domain domain,
        string name,
        Guid createdBy)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Domain = domain,
            Name = name,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        return tenant;
    }

    public void UpdateDomain(ValueObjects.Domain newDomain)
    {
        Domain = newDomain;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
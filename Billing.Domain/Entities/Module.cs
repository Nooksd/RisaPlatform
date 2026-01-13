namespace Billing.Domain.Entities;

/// <summary>
/// Representa um módulo do sistema (RH, CRM, TI, etc.)
/// </summary>
public sealed class Module : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    /// <summary>
    /// Preço base por usuário/mês (sem descontos)
    /// </summary>
    public decimal PricePerUser { get; private set; }
    
    /// <summary>
    /// Navegação para preços especiais por quantidade
    /// </summary>
    public ICollection<ModuleQuantityDiscount> QuantityDiscounts { get; private set; } = [];
    
    private Module() { }
    
    public Module(string code, string name, decimal pricePerUser, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (pricePerUser < 0)
            throw new ArgumentException("Price cannot be negative", nameof(pricePerUser));
            
        Code = code.ToUpperInvariant();
        Name = name;
        PricePerUser = pricePerUser;
        Description = description;
    }
    
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));
            
        PricePerUser = newPrice;
        MarkAsUpdated();
    }
    
    public void UpdateInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
            
        Name = name;
        Description = description;
        MarkAsUpdated();
    }
    
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }
    
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
    
    /// <summary>
    /// Calcula o preço com desconto por quantidade de usuários
    /// </summary>
    public decimal GetPriceForQuantity(int userCount)
    {
        if (userCount <= 0) return 0;
        
        var applicableDiscount = QuantityDiscounts
            .Where(d => d.MinUsers <= userCount)
            .OrderByDescending(d => d.MinUsers)
            .FirstOrDefault();
            
        if (applicableDiscount is null)
            return PricePerUser;
            
        return PricePerUser * (1 - applicableDiscount.DiscountPercentage);
    }
}

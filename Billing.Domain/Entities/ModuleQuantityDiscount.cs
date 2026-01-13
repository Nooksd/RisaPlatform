namespace Billing.Domain.Entities;

/// <summary>
/// Desconto por quantidade de usuários em um módulo
/// Ex: 10+ usuários = 10% desconto, 50+ = 20%, 100+ = 30%
/// </summary>
public sealed class ModuleQuantityDiscount : Entity
{
    public Guid ModuleId { get; private set; }
    public Module Module { get; private set; } = default!;
    
    /// <summary>
    /// Quantidade mínima de usuários para aplicar o desconto
    /// </summary>
    public int MinUsers { get; private set; }
    
    /// <summary>
    /// Percentual de desconto (0.10 = 10%)
    /// </summary>
    public decimal DiscountPercentage { get; private set; }
    
    private ModuleQuantityDiscount() { }
    
    public ModuleQuantityDiscount(Guid moduleId, int minUsers, decimal discountPercentage)
    {
        if (minUsers <= 0)
            throw new ArgumentException("MinUsers must be greater than 0", nameof(minUsers));
        if (discountPercentage < 0 || discountPercentage > 1)
            throw new ArgumentException("Discount must be between 0 and 1", nameof(discountPercentage));
            
        ModuleId = moduleId;
        MinUsers = minUsers;
        DiscountPercentage = discountPercentage;
    }
    
    public void Update(int minUsers, decimal discountPercentage)
    {
        if (minUsers <= 0)
            throw new ArgumentException("MinUsers must be greater than 0", nameof(minUsers));
        if (discountPercentage < 0 || discountPercentage > 1)
            throw new ArgumentException("Discount must be between 0 and 1", nameof(discountPercentage));
            
        MinUsers = minUsers;
        DiscountPercentage = discountPercentage;
        MarkAsUpdated();
    }
}

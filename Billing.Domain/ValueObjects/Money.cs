namespace Billing.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "BRL";
    
    private Money() { }
    
    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }
    
    public static Money Zero => new(0);
    
    public static Money FromCents(long cents, string currency = "BRL")
        => new(cents / 100m, currency);
    
    public long ToCents() => (long)(Amount * 100);
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
            
        return new Money(Amount + other.Amount, Currency);
    }
    
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");
            
        return new Money(Amount - other.Amount, Currency);
    }
    
    public Money Multiply(decimal multiplier)
        => new(Amount * multiplier, Currency);
    
    public Money ApplyDiscount(decimal percentage)
    {
        if (percentage < 0 || percentage > 1)
            throw new ArgumentException("Percentage must be between 0 and 1", nameof(percentage));
            
        return new Money(Amount * (1 - percentage), Currency);
    }
    
    public override string ToString()
        => Currency == "BRL" 
            ? $"R$ {Amount:N2}" 
            : $"{Currency} {Amount:N2}";
            
    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static Money operator *(Money a, decimal b) => a.Multiply(b);
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;
}

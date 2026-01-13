using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Data.Configurations;

public sealed class SubscriptionModuleConfiguration : IEntityTypeConfiguration<SubscriptionModule>
{
    public void Configure(EntityTypeBuilder<SubscriptionModule> builder)
    {
        builder.ToTable("subscription_modules");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Id)
            .HasColumnName("id");
            
        builder.Property(m => m.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();
            
        builder.Property(m => m.ModuleId)
            .HasColumnName("module_id")
            .IsRequired();
            
        builder.Property(m => m.ModuleCode)
            .HasColumnName("module_code")
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(m => m.PricePerUserAtPurchase)
            .HasColumnName("price_per_user_at_purchase")
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(m => m.QuantityDiscountApplied)
            .HasColumnName("quantity_discount_applied")
            .HasPrecision(5, 4);
            
        builder.Property(m => m.TotalBeforeTimeDiscount)
            .HasColumnName("total_before_time_discount")
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at");
            
        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.HasIndex(m => new { m.SubscriptionId, m.ModuleId })
            .IsUnique();
            
        builder.HasOne(m => m.Module)
            .WithMany()
            .HasForeignKey(m => m.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Data.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .HasColumnName("id");
            
        builder.Property(s => s.TenantBillingId)
            .HasColumnName("tenant_billing_id")
            .IsRequired();
            
        builder.Property(s => s.Duration)
            .HasColumnName("duration")
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(s => s.UserCount)
            .HasColumnName("user_count")
            .IsRequired();
            
        builder.Property(s => s.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(s => s.DurationDiscountApplied)
            .HasColumnName("duration_discount_applied")
            .HasPrecision(5, 4);
            
        builder.Property(s => s.StartsAt)
            .HasColumnName("starts_at")
            .IsRequired();
            
        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
            
        builder.Property(s => s.GracePeriodDaysDeducted)
            .HasColumnName("grace_period_days_deducted")
            .HasDefaultValue(0);
            
        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
            
        builder.Property(s => s.PaymentId)
            .HasColumnName("payment_id");
            
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at");
            
        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.HasIndex(s => s.TenantBillingId);
        
        builder.HasIndex(s => s.ExpiresAt);
        
        builder.HasIndex(s => s.IsActive);
        
        builder.HasOne(s => s.Payment)
            .WithOne(p => p.Subscription)
            .HasForeignKey<Subscription>(s => s.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasMany(s => s.Modules)
            .WithOne(m => m.Subscription)
            .HasForeignKey(m => m.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

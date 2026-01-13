using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Data.Configurations;

public sealed class TenantBillingConfiguration : IEntityTypeConfiguration<TenantBilling>
{
    public void Configure(EntityTypeBuilder<TenantBilling> builder)
    {
        builder.ToTable("tenant_billings");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Id)
            .HasColumnName("id");
            
        builder.Property(t => t.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();
            
        builder.Property(t => t.TenantAccountId)
            .HasColumnName("tenant_account_id")
            .IsRequired();
            
        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(t => t.StatusChangedAt)
            .HasColumnName("status_changed_at");
            
        builder.Property(t => t.PaymentGatewayCustomerId)
            .HasColumnName("payment_gateway_customer_id")
            .HasMaxLength(100);
            
        builder.Property(t => t.BillingEmail)
            .HasColumnName("billing_email")
            .HasMaxLength(255)
            .IsRequired();
            
        builder.Property(t => t.TaxId)
            .HasColumnName("tax_id")
            .HasMaxLength(20);
            
        builder.Property(t => t.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(255);
            
        builder.Property(t => t.GracePeriodUsedInCurrentCycle)
            .HasColumnName("grace_period_used_in_current_cycle")
            .HasDefaultValue(false);
            
        builder.Property(t => t.GracePeriodRequestedAt)
            .HasColumnName("grace_period_requested_at");
            
        builder.Property(t => t.GracePeriodDaysToDeduct)
            .HasColumnName("grace_period_days_to_deduct")
            .HasDefaultValue(0);
            
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");
            
        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.HasIndex(t => t.TenantId)
            .IsUnique();
            
        builder.HasIndex(t => t.TenantAccountId);
        
        builder.HasIndex(t => t.Status);
        
        builder.HasIndex(t => t.PaymentGatewayCustomerId);
        
        builder.HasMany(t => t.Subscriptions)
            .WithOne(s => s.TenantBilling)
            .HasForeignKey(s => s.TenantBillingId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(t => t.Payments)
            .WithOne(p => p.TenantBilling)
            .HasForeignKey(p => p.TenantBillingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasColumnName("id");
            
        builder.Property(p => p.TenantBillingId)
            .HasColumnName("tenant_billing_id")
            .IsRequired();
            
        builder.Property(p => p.GatewayPaymentId)
            .HasColumnName("gateway_payment_id")
            .HasMaxLength(100);
            
        builder.Property(p => p.Method)
            .HasColumnName("method")
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(p => p.DueDate)
            .HasColumnName("due_date");
            
        builder.Property(p => p.ConfirmedAt)
            .HasColumnName("confirmed_at");
            
        builder.Property(p => p.BoletoUrl)
            .HasColumnName("boleto_url")
            .HasMaxLength(500);
            
        builder.Property(p => p.BoletoBarcode)
            .HasColumnName("boleto_barcode")
            .HasMaxLength(100);
            
        builder.Property(p => p.PixCopyPaste)
            .HasColumnName("pix_copy_paste")
            .HasMaxLength(500);
            
        builder.Property(p => p.PixQrCode)
            .HasColumnName("pix_qr_code");
            
        builder.Property(p => p.PixExpiresAt)
            .HasColumnName("pix_expires_at");
            
        builder.Property(p => p.CardLastFour)
            .HasColumnName("card_last_four")
            .HasMaxLength(4);
            
        builder.Property(p => p.CardBrand)
            .HasColumnName("card_brand")
            .HasMaxLength(50);
            
        builder.Property(p => p.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);
            
        builder.Property(p => p.WebhookPayload)
            .HasColumnName("webhook_payload");
            
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");
            
        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.HasIndex(p => p.TenantBillingId);
        
        builder.HasIndex(p => p.GatewayPaymentId)
            .IsUnique()
            .HasFilter("gateway_payment_id IS NOT NULL");
            
        builder.HasIndex(p => p.Status);
        
        builder.HasIndex(p => p.CreatedAt);
    }
}

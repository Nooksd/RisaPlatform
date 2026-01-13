using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Data.Configurations;

public sealed class ModuleQuantityDiscountConfiguration : IEntityTypeConfiguration<ModuleQuantityDiscount>
{
    public void Configure(EntityTypeBuilder<ModuleQuantityDiscount> builder)
    {
        builder.ToTable("module_quantity_discounts");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id)
            .HasColumnName("id");
            
        builder.Property(d => d.ModuleId)
            .HasColumnName("module_id")
            .IsRequired();
            
        builder.Property(d => d.MinUsers)
            .HasColumnName("min_users")
            .IsRequired();
            
        builder.Property(d => d.DiscountPercentage)
            .HasColumnName("discount_percentage")
            .HasPrecision(5, 4)
            .IsRequired();
            
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at");
            
        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.HasIndex(d => new { d.ModuleId, d.MinUsers })
            .IsUnique();
    }
}

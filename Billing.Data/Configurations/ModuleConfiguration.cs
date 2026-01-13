using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Data.Configurations;

public sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("modules");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Id)
            .HasColumnName("id");
            
        builder.Property(m => m.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(m => m.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(m => m.Description)
            .HasColumnName("description")
            .HasMaxLength(500);
            
        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
            
        builder.Property(m => m.PricePerUser)
            .HasColumnName("price_per_user")
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at");
            
        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.HasIndex(m => m.Code)
            .IsUnique();
            
        builder.HasMany(m => m.QuantityDiscounts)
            .WithOne(d => d.Module)
            .HasForeignKey(d => d.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

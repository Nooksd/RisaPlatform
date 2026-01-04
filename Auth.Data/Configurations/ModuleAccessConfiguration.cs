using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Data.Configurations;

public sealed class ModuleAccessConfiguration : IEntityTypeConfiguration<ModuleAccess>
{
    public void Configure(EntityTypeBuilder<ModuleAccess> builder)
    {
        builder.ToTable("module_accesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantUserId)
            .HasColumnName("tenant_user_id")
            .IsRequired();

        builder.Property(x => x.Module)
            .HasColumnName("module")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AccessLevel)
            .HasColumnName("access_level")
            .IsRequired();

        // Índices
        builder.HasIndex(x => new { x.TenantUserId, x.Module })
            .IsUnique()
            .HasDatabaseName("ix_module_accesses_user_module");
    }
}
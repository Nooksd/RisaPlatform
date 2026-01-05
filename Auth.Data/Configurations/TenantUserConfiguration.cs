using Auth.Domain.Entities;
using Auth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Data.Configurations;

public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("tenant_users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired()
            .HasConversion(
                hash => hash.Value,
                value => PasswordHash.Create(value));

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique()
            .HasDatabaseName("ix_tenant_users_tenant_email");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_tenant_users_tenant_id");

        builder.HasIndex(x => x.CreatedBy)
            .HasDatabaseName("ix_tenant_users_created_by");

        builder.HasMany(x => x.ModuleAccesses)
            .WithOne(x => x.TenantUser)
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.RefreshTokens);
    }
}

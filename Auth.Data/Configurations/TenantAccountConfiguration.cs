using Auth.Domain.Entities;
using Auth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Data.Configurations;

public sealed class TenantAccountConfiguration : IEntityTypeConfiguration<TenantAccount>
{
    public void Configure(EntityTypeBuilder<TenantAccount> builder)
    {
        builder.ToTable("tenant_accounts");

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
            .HasConversion(
                hash => hash != null ? hash.Value : null,
                value => value != null ? PasswordHash.Create(value) : null);

        builder.Property(x => x.OAuthId)
            .HasColumnName("oauth_id")
            .HasMaxLength(255);

        builder.Property(x => x.OAuthProvider)
            .HasColumnName("oauth_provider")
            .IsRequired();

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

        // Índices
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ix_tenant_accounts_email");

        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasDatabaseName("ix_tenant_accounts_tenant_id");

        builder.HasIndex(x => new { x.OAuthId, x.OAuthProvider })
            .HasDatabaseName("ix_tenant_accounts_oauth");

        // Relacionamentos
        builder.HasMany(x => x.RefreshTokens)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

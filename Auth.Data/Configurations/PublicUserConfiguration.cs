using Auth.Domain.Entities;
using Auth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Data.Configurations;

public sealed class PublicUserConfiguration : IEntityTypeConfiguration<PublicUser>
{
    public void Configure(EntityTypeBuilder<PublicUser> builder)
    {
        builder.ToTable("public_users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.Module)
            .HasColumnName("module")
            .HasMaxLength(50)
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

        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.Module, x.Email })
            .IsUnique()
            .HasDatabaseName("ix_public_users_tenant_module_email");

        builder.HasIndex(x => new { x.TenantId, x.Module })
            .HasDatabaseName("ix_public_users_tenant_module");

        builder.HasIndex(x => new { x.OAuthId, x.OAuthProvider, x.Module })
            .HasDatabaseName("ix_public_users_oauth_module");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("ix_public_users_is_deleted");

        builder.Ignore(x => x.RefreshTokens);
    }
}

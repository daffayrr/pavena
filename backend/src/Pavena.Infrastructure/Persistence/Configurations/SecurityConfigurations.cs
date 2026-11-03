using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Identity;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("user_refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(255);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_user_refresh_tokens_hash");

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActionType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.TargetResourceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TargetResourceId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ReasonCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.HasIndex(a => a.TimestampUtc)
            .HasDatabaseName("idx_admin_audit_logs_timestamp");

        builder.HasOne(a => a.AdminUser)
            .WithMany()
            .HasForeignKey(a => a.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

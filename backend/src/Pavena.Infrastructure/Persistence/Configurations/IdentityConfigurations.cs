using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Identity;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.UserName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.SecurityStamp)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("uq_users_normalized_email");

        builder.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("uq_users_normalized_username");

        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<Domain.Entities.Profiles.UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        builder.ToTable("platform_roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(255);

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("uq_platform_roles_normalized_name");
    }
}

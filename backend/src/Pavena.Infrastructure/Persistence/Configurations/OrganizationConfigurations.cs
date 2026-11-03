using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Organizations;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.WebsiteUrl)
            .HasMaxLength(255);

        builder.Property(o => o.LogoUrl)
            .HasMaxLength(500);

        builder.Property(o => o.CountryCode)
            .HasMaxLength(2);

        builder.Property(o => o.City)
            .HasMaxLength(100);

        builder.Property(o => o.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasIndex(o => o.Slug)
            .IsUnique()
            .HasDatabaseName("uq_organizations_slug");
    }
}

public class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("organization_memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Department)
            .HasMaxLength(100);

        builder.Property(m => m.Title)
            .HasMaxLength(100);

        builder.HasIndex(m => new { m.OrganizationId, m.UserId })
            .IsUnique()
            .HasDatabaseName("uq_org_membership_user");

        builder.HasOne(m => m.Organization)
            .WithMany(o => o.Memberships)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrganizationDepartmentConfiguration : IEntityTypeConfiguration<OrganizationDepartment>
{
    public void Configure(EntityTypeBuilder<OrganizationDepartment> builder)
    {
        builder.ToTable("organization_departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Code)
            .HasMaxLength(50);

        builder.HasOne(d => d.Organization)
            .WithMany(o => o.Departments)
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

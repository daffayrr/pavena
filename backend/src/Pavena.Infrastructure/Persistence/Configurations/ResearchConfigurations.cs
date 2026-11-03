using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Research;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class ResearchProjectConfiguration : IEntityTypeConfiguration<ResearchProject>
{
    public void Configure(EntityTypeBuilder<ResearchProject> builder)
    {
        builder.ToTable("research_projects", t =>
        {
            t.HasCheckConstraint("chk_rp_single_owner",
                "(`OwnerUserId` IS NOT NULL AND `OwnerOrganizationId` IS NULL) OR (`OwnerUserId` IS NULL AND `OwnerOrganizationId` IS NOT NULL)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.ResearchField)
            .HasMaxLength(100);

        builder.Property(p => p.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("uq_research_projects_slug");

        builder.HasOne(p => p.OwnerUser)
            .WithMany()
            .HasForeignKey(p => p.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.OwnerOrganization)
            .WithMany()
            .HasForeignKey(p => p.OwnerOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ResearchMemberConfiguration : IEntityTypeConfiguration<ResearchMember>
{
    public void Configure(EntityTypeBuilder<ResearchMember> builder)
    {
        builder.ToTable("research_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(m => new { m.ProjectId, m.UserId })
            .IsUnique()
            .HasDatabaseName("uq_research_members_user");

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.AddedByUser)
            .WithMany()
            .HasForeignKey(m => m.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ResearchTaskConfiguration : IEntityTypeConfiguration<ResearchTask>
{
    public void Configure(EntityTypeBuilder<ResearchTask> builder)
    {
        builder.ToTable("research_tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.AssigneeUser)
            .WithMany()
            .HasForeignKey(t => t.AssigneeUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

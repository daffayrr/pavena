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

        builder.Property(p => p.OriginType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Stage)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("uq_research_projects_slug");

        builder.HasIndex(p => new { p.Visibility, p.Status, p.CreatedAtUtc })
            .HasDatabaseName("idx_research_projects_vis_status_created");

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

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
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

public class ResearchMilestoneConfiguration : IEntityTypeConfiguration<ResearchMilestone>
{
    public void Configure(EntityTypeBuilder<ResearchMilestone> builder)
    {
        builder.ToTable("research_milestones");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(m => new { m.ProjectId, m.TargetDateUtc })
            .HasDatabaseName("idx_milestones_proj_target_date");

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.CreatedByUser)
            .WithMany()
            .HasForeignKey(m => m.CreatedByUserId)
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

        builder.HasIndex(t => new { t.ProjectId, t.Status, t.DueDateUtc })
            .HasDatabaseName("idx_tasks_proj_status_due");

        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Milestone)
            .WithMany(m => m.Tasks)
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.SetNull);

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

public class ProjectInvitationConfiguration : IEntityTypeConfiguration<ProjectInvitation>
{
    public void Configure(EntityTypeBuilder<ProjectInvitation> builder)
    {
        builder.ToTable("project_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.RoleOffered)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Message)
            .HasMaxLength(500);

        builder.HasIndex(i => new { i.ProjectId, i.InvitedUserId, i.Status })
            .HasDatabaseName("idx_invitations_proj_user_status");

        builder.HasOne(i => i.Project)
            .WithMany(p => p.Invitations)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedByUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProjectActivityLogConfiguration : IEntityTypeConfiguration<ProjectActivityLog>
{
    public void Configure(EntityTypeBuilder<ProjectActivityLog> builder)
    {
        builder.ToTable("project_activity_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(a => a.Details)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(a => new { a.ProjectId, a.TimestampUtc })
            .HasDatabaseName("idx_activity_proj_timestamp");

        builder.HasOne(a => a.Project)
            .WithMany(p => p.ActivityLogs)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ActorUser)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Challenges;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class IndustryChallengeConfiguration : IEntityTypeConfiguration<IndustryChallenge>
{
    public void Configure(EntityTypeBuilder<IndustryChallenge> builder)
    {
        builder.ToTable("industry_challenges");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Summary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.ProblemStatement)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(c => c.ResearchScope)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(c => c.ExpectedOutcomes)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Visibility)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Eligibility)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(c => c.MinBudget)
            .HasPrecision(18, 2);

        builder.Property(c => c.MaxBudget)
            .HasPrecision(18, 2);

        builder.Property(c => c.RequiresManualNdaApproval)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(c => c.NdaTermsVersion)
            .HasMaxLength(20)
            .HasDefaultValue("v1.0")
            .IsRequired();

        builder.Property(c => c.NdaTermsText)
            .HasColumnType("text");

        builder.Property(c => c.CancellationReason)
            .HasMaxLength(1000);

        builder.Property(c => c.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        // Foreign Keys
        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedByUser)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Collections
        builder.HasMany(c => c.Attachments)
            .WithOne(a => a.Challenge)
            .HasForeignKey(a => a.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Domains)
            .WithOne(d => d.Challenge)
            .HasForeignKey(d => d.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.RequiredExpertise)
            .WithOne(e => e.Challenge)
            .HasForeignKey(e => e.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Approvals)
            .WithOne(a => a.Challenge)
            .HasForeignKey(a => a.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ActivityLogs)
            .WithOne(l => l.Challenge)
            .HasForeignKey(l => l.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.NdaAgreements)
            .WithOne(n => n.Challenge)
            .HasForeignKey(n => n.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => new { c.OrganizationId, c.Status }).HasDatabaseName("idx_challenges_org_status");
        builder.HasIndex(c => new { c.Visibility, c.Status, c.SubmissionDeadlineUtc }).HasDatabaseName("idx_challenges_vis_status_deadline");
    }
}

public class ChallengeAttachmentConfiguration : IEntityTypeConfiguration<ChallengeAttachment>
{
    public void Configure(EntityTypeBuilder<ChallengeAttachment> builder)
    {
        builder.ToTable("challenge_attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(a => a.FileExtension)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.ConfidentialityLevel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ChallengeId, a.ConfidentialityLevel }).HasDatabaseName("idx_ch_att_confidentiality");
    }
}

public class ChallengeResearchDomainConfiguration : IEntityTypeConfiguration<ChallengeResearchDomain>
{
    public void Configure(EntityTypeBuilder<ChallengeResearchDomain> builder)
    {
        builder.ToTable("challenge_research_domains");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DomainName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.SubDomain)
            .HasMaxLength(100);

        builder.HasIndex(d => new { d.ChallengeId, d.DomainName }).HasDatabaseName("idx_ch_domain");
    }
}

public class ChallengeExpertiseRequirementConfiguration : IEntityTypeConfiguration<ChallengeExpertiseRequirement>
{
    public void Configure(EntityTypeBuilder<ChallengeExpertiseRequirement> builder)
    {
        builder.ToTable("challenge_expertise_requirements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SkillOrDiscipline)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.ImportanceLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.ChallengeId, e.SkillOrDiscipline }).HasDatabaseName("idx_ch_expertise");
    }
}

public class ChallengeApprovalConfiguration : IEntityTypeConfiguration<ChallengeApproval>
{
    public void Configure(EntityTypeBuilder<ChallengeApproval> builder)
    {
        builder.ToTable("challenge_approvals");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Decision)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.ReviewComments)
            .HasMaxLength(2000);

        builder.HasOne(a => a.ReviewerUser)
            .WithMany()
            .HasForeignKey(a => a.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ChallengeId, a.Decision }).HasDatabaseName("idx_ch_approvals_decision");
    }
}

public class ChallengeActivityLogConfiguration : IEntityTypeConfiguration<ChallengeActivityLog>
{
    public void Configure(EntityTypeBuilder<ChallengeActivityLog> builder)
    {
        builder.ToTable("challenge_activity_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ActionType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.PreviousStatus)
            .HasMaxLength(30);

        builder.Property(l => l.NewStatus)
            .HasMaxLength(30);

        builder.Property(l => l.Notes)
            .HasMaxLength(2000);

        builder.HasOne(l => l.ActorUser)
            .WithMany()
            .HasForeignKey(l => l.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.ChallengeId, l.TimestampUtc }).HasDatabaseName("idx_ch_activity_time");
    }
}

public class ChallengeNdaAgreementConfiguration : IEntityTypeConfiguration<ChallengeNdaAgreement>
{
    public void Configure(EntityTypeBuilder<ChallengeNdaAgreement> builder)
    {
        builder.ToTable("challenge_nda_agreements");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.NdaVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.TermsContentHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(n => n.IpAddress)
            .HasMaxLength(50);

        builder.HasOne(n => n.ResearcherUser)
            .WithMany()
            .HasForeignKey(n => n.ResearcherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.ApprovedByUser)
            .WithMany()
            .HasForeignKey(n => n.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.ChallengeId, n.ResearcherUserId }).IsUnique().HasDatabaseName("idx_ch_nda_unique");
        builder.HasIndex(n => new { n.ChallengeId, n.IsApprovedBySponsor }).HasDatabaseName("idx_ch_nda_approved");
    }
}

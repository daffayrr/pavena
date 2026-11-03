using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Proposals;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class ResearchProposalConfiguration : IEntityTypeConfiguration<ResearchProposal>
{
    public void Configure(EntityTypeBuilder<ResearchProposal> builder)
    {
        builder.ToTable("research_proposals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.Abstract)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ProblemStatement)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ResearchObjectives)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ProposedMethodology)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ExpectedOutcomes)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.ProposedTimelineMonths)
            .IsRequired();

        builder.Property(p => p.ProposedBudget)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("USD")
            .IsRequired();

        builder.Property(p => p.FundingJustification)
            .HasColumnType("text");

        builder.Property(p => p.Confidentiality)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.SubmissionVersion)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(p => p.DecisionNotes)
            .HasColumnType("text");

        builder.Property(p => p.EligibilityRejectionReason)
            .HasColumnType("text");

        builder.Property(p => p.RevisionGuidance)
            .HasColumnType("text");

        builder.Property(p => p.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        // Foreign keys
        builder.HasOne(p => p.Challenge)
            .WithMany()
            .HasForeignKey(p => p.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LeadResearcher)
            .WithMany()
            .HasForeignKey(p => p.LeadResearcherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SubmittingOrganization)
            .WithMany()
            .HasForeignKey(p => p.SubmittingOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ConvertedProject)
            .WithMany()
            .HasForeignKey(p => p.ConvertedProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.DecisionByUser)
            .WithMany()
            .HasForeignKey(p => p.DecisionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.EligibilityReviewedByUser)
            .WithMany()
            .HasForeignKey(p => p.EligibilityReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Collections
        builder.HasMany(p => p.Investigators)
            .WithOne(i => i.Proposal)
            .HasForeignKey(i => i.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Domains)
            .WithOne(d => d.Proposal)
            .HasForeignKey(d => d.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ExpertiseTags)
            .WithOne(e => e.Proposal)
            .HasForeignKey(e => e.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
            .WithOne(a => a.Proposal)
            .HasForeignKey(a => a.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.VersionSnapshots)
            .WithOne(v => v.Proposal)
            .HasForeignKey(v => v.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ReviewAssignments)
            .WithOne(r => r.Proposal)
            .HasForeignKey(r => r.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ActivityLogs)
            .WithOne(l => l.Proposal)
            .HasForeignKey(l => l.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(p => new { p.ChallengeId, p.Status });
        builder.HasIndex(p => new { p.LeadResearcherId, p.Status });
        builder.HasIndex(p => p.SubmittingOrganizationId);
        builder.HasIndex(p => p.Slug);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ConvertedProjectId);
    }
}

public class ProposalInvestigatorConfiguration : IEntityTypeConfiguration<ProposalInvestigator>
{
    public void Configure(EntityTypeBuilder<ProposalInvestigator> builder)
    {
        builder.ToTable("proposal_investigators");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Role)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(i => i.ContributionDescription)
            .HasMaxLength(500);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.ProposalId, i.UserId }).IsUnique();
    }
}

public class ProposalResearchDomainConfiguration : IEntityTypeConfiguration<ProposalResearchDomain>
{
    public void Configure(EntityTypeBuilder<ProposalResearchDomain> builder)
    {
        builder.ToTable("proposal_research_domains");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Domain)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(d => new { d.ProposalId, d.Domain });
    }
}

public class ProposalExpertiseConfiguration : IEntityTypeConfiguration<ProposalExpertise>
{
    public void Configure(EntityTypeBuilder<ProposalExpertise> builder)
    {
        builder.ToTable("proposal_expertise");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpertiseTag)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.ProposalId, e.ExpertiseTag });
    }
}

public class ProposalAttachmentConfiguration : IEntityTypeConfiguration<ProposalAttachment>
{
    public void Configure(EntityTypeBuilder<ProposalAttachment> builder)
    {
        builder.ToTable("proposal_attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AttachmentType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(a => a.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.StoragePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.FileHashSha256)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ProposalId, a.AttachmentType });
    }
}

public class ProposalVersionSnapshotConfiguration : IEntityTypeConfiguration<ProposalVersionSnapshot>
{
    public void Configure(EntityTypeBuilder<ProposalVersionSnapshot> builder)
    {
        builder.ToTable("proposal_version_snapshots");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SnapshotDataJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(v => v.ChangeSummary)
            .HasMaxLength(1000);

        builder.HasOne(v => v.CreatedByUser)
            .WithMany()
            .HasForeignKey(v => v.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ProposalId, v.VersionNumber }).IsUnique();
    }
}

public class ProposalReviewAssignmentConfiguration : IEntityTypeConfiguration<ProposalReviewAssignment>
{
    public void Configure(EntityTypeBuilder<ProposalReviewAssignment> builder)
    {
        builder.ToTable("proposal_review_assignments");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.DeclineReason)
            .HasMaxLength(500);

        builder.Property(r => r.HasCoiDeclaration)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.CoiType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(r => r.CoiDescription)
            .HasColumnType("text");

        builder.HasOne(r => r.ReviewerUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AssignedByUser)
            .WithMany()
            .HasForeignKey(r => r.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Assessment)
            .WithOne(a => a.Assignment)
            .HasForeignKey<ProposalAssessment>(a => a.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.ProposalId, r.ReviewerUserId }).IsUnique();
    }
}

public class ProposalAssessmentConfiguration : IEntityTypeConfiguration<ProposalAssessment>
{
    public void Configure(EntityTypeBuilder<ProposalAssessment> builder)
    {
        builder.ToTable("proposal_assessments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.TechnicalMeritScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.MethodologyScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.FeasibilityScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.InnovationScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.BudgetAdequacyScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.TeamQualificationScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.ImpactPotentialScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.AlignmentWithChallengeScore).HasPrecision(5, 2).IsRequired();
        builder.Property(a => a.OverallScore).HasPrecision(5, 2).IsRequired();

        builder.Property(a => a.Strengths)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.Weaknesses)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.RecommendationsForImprovement)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.ConfidentialRemarksForSponsor)
            .HasColumnType("text");

        builder.Property(a => a.Recommendation)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(a => a.Proposal)
            .WithMany()
            .HasForeignKey(a => a.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ReviewerUser)
            .WithMany()
            .HasForeignKey(a => a.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.AssignmentId).IsUnique();
        builder.HasIndex(a => new { a.ProposalId, a.ReviewerUserId });
    }
}

public class ProposalActivityLogConfiguration : IEntityTypeConfiguration<ProposalActivityLog>
{
    public void Configure(EntityTypeBuilder<ProposalActivityLog> builder)
    {
        builder.ToTable("proposal_activity_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ActivityType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(l => l.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.MetadataJson)
            .HasColumnType("text");

        builder.HasOne(l => l.ActorUser)
            .WithMany()
            .HasForeignKey(l => l.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.ProposalId, l.CreatedAtUtc });
    }
}

using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Proposals;

public class ProposalInvestigator : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid UserId { get; set; }
    public InvestigatorRole Role { get; set; } = InvestigatorRole.CoInvestigator;
    public string? ContributionDescription { get; set; }

    // Navigation properties
    public virtual ResearchProposal? Proposal { get; set; }
    public virtual User? User { get; set; }
}

public class ProposalResearchDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProposalId { get; set; }
    public string Domain { get; set; } = string.Empty;

    public virtual ResearchProposal? Proposal { get; set; }
}

public class ProposalExpertise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProposalId { get; set; }
    public string ExpertiseTag { get; set; } = string.Empty;

    public virtual ResearchProposal? Proposal { get; set; }
}

public class ProposalAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProposalId { get; set; }
    public ProposalAttachmentType AttachmentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public string FileHashSha256 { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ResearchProposal? Proposal { get; set; }
    public virtual User? UploadedByUser { get; set; }
}

public class ProposalVersionSnapshot : BaseEntity
{
    public Guid ProposalId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotDataJson { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public Guid CreatedByUserId { get; set; }

    public virtual ResearchProposal? Proposal { get; set; }
    public virtual User? CreatedByUser { get; set; }
}

public class ProposalReviewAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProposalId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public ReviewAssignmentStatus Status { get; set; } = ReviewAssignmentStatus.Assigned;
    public DateTime? DueAtUtc { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? DeclinedAtUtc { get; set; }
    public string? DeclineReason { get; set; }

    public bool HasCoiDeclaration { get; set; } = false;
    public CoiDeclarationType CoiType { get; set; } = CoiDeclarationType.None;
    public string? CoiDescription { get; set; }
    public DateTime? CoiDeclaredAtUtc { get; set; }

    public virtual ResearchProposal? Proposal { get; set; }
    public virtual User? ReviewerUser { get; set; }
    public virtual User? AssignedByUser { get; set; }
    public virtual ProposalAssessment? Assessment { get; set; }

    public void AcceptAssignment()
    {
        if (Status != ReviewAssignmentStatus.Assigned)
            throw new DomainException($"Cannot accept review assignment in status '{Status}'.");

        Status = ReviewAssignmentStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
    }

    public void DeclineAssignment(string reason)
    {
        if (Status != ReviewAssignmentStatus.Assigned && Status != ReviewAssignmentStatus.Accepted)
            throw new DomainException($"Cannot decline review assignment in status '{Status}'.");

        Status = ReviewAssignmentStatus.Declined;
        DeclinedAtUtc = DateTime.UtcNow;
        DeclineReason = reason?.Trim();
    }

    public void RecuseDueToCoi(CoiDeclarationType coiType, string description)
    {
        if (Status == ReviewAssignmentStatus.Completed)
            throw new DomainException("Cannot recuse after assessment has been completed.");

        Status = ReviewAssignmentStatus.Recused;
        HasCoiDeclaration = true;
        CoiType = coiType;
        CoiDescription = description.Trim();
        CoiDeclaredAtUtc = DateTime.UtcNow;
    }

    public void DeclareNoCoi()
    {
        HasCoiDeclaration = true;
        CoiType = CoiDeclarationType.None;
        CoiDescription = "Reviewer explicitly declared absence of conflict of interest.";
        CoiDeclaredAtUtc = DateTime.UtcNow;
    }
}

public class ProposalAssessment : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Guid ProposalId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;

    // Multi-criteria Rubric Scores (0.00 to 10.00)
    public decimal TechnicalMeritScore { get; set; }
    public decimal MethodologyScore { get; set; }
    public decimal FeasibilityScore { get; set; }
    public decimal InnovationScore { get; set; }
    public decimal BudgetAdequacyScore { get; set; }
    public decimal TeamQualificationScore { get; set; }
    public decimal ImpactPotentialScore { get; set; }
    public decimal AlignmentWithChallengeScore { get; set; }

    // Composite overall score (0.00 to 100.00)
    public decimal OverallScore { get; set; }

    // Qualitative Feedback
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    public string RecommendationsForImprovement { get; set; } = string.Empty;
    public string? ConfidentialRemarksForSponsor { get; set; }
    public AssessmentRecommendation Recommendation { get; set; } = AssessmentRecommendation.RecommendRevision;

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ProposalReviewAssignment? Assignment { get; set; }
    public virtual ResearchProposal? Proposal { get; set; }
    public virtual User? ReviewerUser { get; set; }

    public void CalculateOverallScore()
    {
        decimal sum = TechnicalMeritScore + MethodologyScore + FeasibilityScore + InnovationScore +
                      BudgetAdequacyScore + TeamQualificationScore + ImpactPotentialScore + AlignmentWithChallengeScore;
        // 8 criteria * 10 max = 80 max sum. Scaling to 100: (sum / 80) * 100 = (sum / 8) * 10
        OverallScore = Math.Round((sum / 8.0m) * 10.0m, 2);
    }

    public void SubmitAssessment()
    {
        if (Status == AssessmentStatus.Submitted)
            throw new DomainException("Assessment has already been submitted.");

        ValidateRubrics();
        CalculateOverallScore();

        Status = AssessmentStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;

        if (Assignment != null)
        {
            Assignment.Status = ReviewAssignmentStatus.Completed;
        }
    }

    public void UpdateRubricScores(
        decimal technicalMerit,
        decimal methodology,
        decimal feasibility,
        decimal innovation,
        decimal budgetAdequacy,
        decimal teamQualification,
        decimal impactPotential,
        decimal alignment,
        string strengths,
        string weaknesses,
        string recommendations,
        string? confidentialRemarks,
        AssessmentRecommendation recommendation)
    {
        if (Status == AssessmentStatus.Submitted)
            throw new DomainException("Cannot edit assessment once submitted.");

        TechnicalMeritScore = technicalMerit;
        MethodologyScore = methodology;
        FeasibilityScore = feasibility;
        InnovationScore = innovation;
        BudgetAdequacyScore = budgetAdequacy;
        TeamQualificationScore = teamQualification;
        ImpactPotentialScore = impactPotential;
        AlignmentWithChallengeScore = alignment;

        Strengths = strengths?.Trim() ?? string.Empty;
        Weaknesses = weaknesses?.Trim() ?? string.Empty;
        RecommendationsForImprovement = recommendations?.Trim() ?? string.Empty;
        ConfidentialRemarksForSponsor = confidentialRemarks?.Trim();
        Recommendation = recommendation;

        CalculateOverallScore();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ValidateRubrics()
    {
        ValidateScoreRange(TechnicalMeritScore, nameof(TechnicalMeritScore));
        ValidateScoreRange(MethodologyScore, nameof(MethodologyScore));
        ValidateScoreRange(FeasibilityScore, nameof(FeasibilityScore));
        ValidateScoreRange(InnovationScore, nameof(InnovationScore));
        ValidateScoreRange(BudgetAdequacyScore, nameof(BudgetAdequacyScore));
        ValidateScoreRange(TeamQualificationScore, nameof(TeamQualificationScore));
        ValidateScoreRange(ImpactPotentialScore, nameof(ImpactPotentialScore));
        ValidateScoreRange(AlignmentWithChallengeScore, nameof(AlignmentWithChallengeScore));

        if (string.IsNullOrWhiteSpace(Strengths))
            throw new DomainException("Strengths commentary is required before submitting assessment.");

        if (string.IsNullOrWhiteSpace(Weaknesses))
            throw new DomainException("Weaknesses commentary is required before submitting assessment.");
    }

    private static void ValidateScoreRange(decimal score, string paramName)
    {
        if (score < 0.0m || score > 10.0m)
            throw new DomainException($"Score for {paramName} must be between 0.0 and 10.0. Provided: {score}");
    }
}

public class ProposalActivityLog : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid ActorUserId { get; set; }
    public ProposalActivityType ActivityType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }

    public virtual ResearchProposal? Proposal { get; set; }
    public virtual User? ActorUser { get; set; }
}

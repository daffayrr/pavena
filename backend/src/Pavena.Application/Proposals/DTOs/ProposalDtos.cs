using Pavena.Domain.Enums;

namespace Pavena.Application.Proposals.DTOs;

public record CreateProposalDraftRequest(
    Guid ChallengeId,
    string Title,
    string Abstract,
    string ProblemStatement,
    string ResearchObjectives,
    string ProposedMethodology,
    string ExpectedOutcomes,
    int ProposedTimelineMonths,
    decimal ProposedBudget,
    string? Currency = "USD",
    string? FundingJustification = null,
    ProposalConfidentialityLevel Confidentiality = ProposalConfidentialityLevel.Confidential,
    Guid? SubmittingOrganizationId = null,
    List<string>? Domains = null,
    List<string>? ExpertiseTags = null
);

public record UpdateProposalDraftRequest(
    string Title,
    string Abstract,
    string ProblemStatement,
    string ResearchObjectives,
    string ProposedMethodology,
    string ExpectedOutcomes,
    int ProposedTimelineMonths,
    decimal ProposedBudget,
    string? Currency = "USD",
    string? FundingJustification = null,
    ProposalConfidentialityLevel Confidentiality = ProposalConfidentialityLevel.Confidential,
    Guid? SubmittingOrganizationId = null,
    List<string>? Domains = null,
    List<string>? ExpertiseTags = null
);

public record AddInvestigatorRequest(
    Guid UserId,
    InvestigatorRole Role,
    string? ContributionDescription = null
);

public record ResubmitProposalRequest(
    string ChangeSummary,
    string Title,
    string Abstract,
    string ProblemStatement,
    string ResearchObjectives,
    string ProposedMethodology,
    string ExpectedOutcomes,
    int ProposedTimelineMonths,
    decimal ProposedBudget,
    string? Currency = "USD",
    string? FundingJustification = null
);

public record ReviewEligibilityRequest(
    bool Passed,
    string? ReasonOrNotes = null
);

public record AssignReviewerRequest(
    Guid ReviewerUserId,
    DateTime? DueAtUtc = null
);

public record DeclareCoiRequest(
    bool HasConflict,
    CoiDeclarationType CoiType,
    string? Description = null
);

public record SubmitAssessmentRequest(
    decimal TechnicalMeritScore,
    decimal MethodologyScore,
    decimal FeasibilityScore,
    decimal InnovationScore,
    decimal BudgetAdequacyScore,
    decimal TeamQualificationScore,
    decimal ImpactPotentialScore,
    decimal AlignmentWithChallengeScore,
    string Strengths,
    string Weaknesses,
    string RecommendationsForImprovement,
    string? ConfidentialRemarksForSponsor,
    AssessmentRecommendation Recommendation
);

public record SponsorDecisionRequest(
    string Decision, // "Accept", "Reject", "RequestRevision"
    string? NotesOrGuidance = null
);

public record ConvertProposalRequest(
    string? CustomProjectTitle = null
);

public record ProposalSummaryResponse(
    Guid Id,
    Guid ChallengeId,
    string ChallengeTitle,
    Guid LeadResearcherId,
    string LeadResearcherName,
    string? LeadResearcherAvatarUrl,
    Guid? SubmittingOrganizationId,
    string? SubmittingOrganizationName,
    string Title,
    string Slug,
    decimal ProposedBudget,
    string Currency,
    int ProposedTimelineMonths,
    ProposalStatus Status,
    int SubmissionVersion,
    Guid? ConvertedProjectId,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? DecisionAtUtc,
    decimal? AverageScore,
    int ReviewCount
);

public record ProposalDetailResponse(
    Guid Id,
    Guid ChallengeId,
    string ChallengeTitle,
    Guid ChallengeOrganizationId,
    string ChallengeOrganizationName,
    Guid LeadResearcherId,
    string LeadResearcherName,
    string? LeadResearcherAvatarUrl,
    Guid? SubmittingOrganizationId,
    string? SubmittingOrganizationName,
    string Title,
    string Slug,
    string Abstract,
    string ProblemStatement,
    string ResearchObjectives,
    string ProposedMethodology,
    string ExpectedOutcomes,
    int ProposedTimelineMonths,
    decimal ProposedBudget,
    string Currency,
    string? FundingJustification,
    ProposalConfidentialityLevel Confidentiality,
    ProposalStatus Status,
    int SubmissionVersion,
    Guid? ConvertedProjectId,
    string? DecisionNotes,
    Guid? DecisionByUserId,
    DateTime? DecisionAtUtc,
    string? EligibilityRejectionReason,
    string? RevisionGuidance,
    DateTime? SubmittedAtUtc,
    DateTime? UnderReviewAtUtc,
    DateTime? RevisionRequestedAtUtc,
    DateTime? ResubmittedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<InvestigatorResponse> Investigators,
    List<string> Domains,
    List<string> ExpertiseTags,
    List<ProposalAttachmentResponse> Attachments,
    List<ProposalVersionSnapshotResponse> VersionSnapshots,
    List<ReviewAssignmentResponse> ReviewAssignments,
    List<ProposalActivityLogResponse> ActivityLogs,
    bool CanEdit,
    bool CanSubmit,
    bool CanResubmit,
    bool CanWithdraw,
    bool CanReview,
    bool CanDecide,
    bool CanConvert
);

public record InvestigatorResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    InvestigatorRole Role,
    string? ContributionDescription,
    string? InstitutionName,
    string? AvatarUrl
);

public record ProposalAttachmentResponse(
    Guid Id,
    Guid ProposalId,
    ProposalAttachmentType AttachmentType,
    string FileName,
    string StoragePath,
    string ContentType,
    long FileSizeBytes,
    string FileHashSha256,
    Guid UploadedByUserId,
    string UploadedByName,
    DateTime UploadedAtUtc
);

public record ProposalVersionSnapshotResponse(
    Guid Id,
    Guid ProposalId,
    int VersionNumber,
    string SnapshotDataJson,
    string? ChangeSummary,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAtUtc
);

public record ReviewAssignmentResponse(
    Guid Id,
    Guid ProposalId,
    Guid ReviewerUserId,
    string ReviewerName,
    string ReviewerEmail,
    string? ReviewerAvatarUrl,
    string? ReviewerInstitution,
    ReviewAssignmentStatus Status,
    DateTime? DueAtUtc,
    DateTime AssignedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? DeclinedAtUtc,
    string? DeclineReason,
    bool HasCoiDeclaration,
    CoiDeclarationType CoiType,
    string? CoiDescription,
    DateTime? CoiDeclaredAtUtc,
    ProposalAssessmentResponse? Assessment
);

public record ProposalAssessmentResponse(
    Guid Id,
    Guid AssignmentId,
    Guid ProposalId,
    Guid ReviewerUserId,
    string ReviewerName,
    AssessmentStatus Status,
    decimal TechnicalMeritScore,
    decimal MethodologyScore,
    decimal FeasibilityScore,
    decimal InnovationScore,
    decimal BudgetAdequacyScore,
    decimal TeamQualificationScore,
    decimal ImpactPotentialScore,
    decimal AlignmentWithChallengeScore,
    decimal OverallScore,
    string Strengths,
    string Weaknesses,
    string RecommendationsForImprovement,
    string? ConfidentialRemarksForSponsor,
    AssessmentRecommendation Recommendation,
    DateTime? SubmittedAtUtc,
    DateTime CreatedAtUtc
);

public record ProposalActivityLogResponse(
    Guid Id,
    Guid ProposalId,
    Guid ActorUserId,
    string ActorName,
    ProposalActivityType ActivityType,
    string Description,
    string? MetadataJson,
    DateTime CreatedAtUtc
);

public record ProposalConversionResponse(
    Guid ProposalId,
    Guid ProjectId,
    string ProjectSlug,
    string ProjectTitle,
    DateTime ConvertedAtUtc,
    bool AlreadyConverted
);

public record EligibilityCheckResult(
    bool IsEligible,
    List<string> FailureReasons,
    bool HasAgreedNda,
    bool MeetsInstitutionalAffiliation,
    bool IsProfileComplete
);

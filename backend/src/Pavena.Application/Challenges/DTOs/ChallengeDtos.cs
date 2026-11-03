using Pavena.Domain.Enums;

namespace Pavena.Application.Challenges.DTOs;

public record ChallengeSummaryDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string? OrganizationLogoUrl,
    OrganizationType OrganizationType,
    string Title,
    string Slug,
    string Summary,
    ChallengeStatus Status,
    ChallengeVisibility Visibility,
    InstitutionalEligibility Eligibility,
    string? Currency,
    decimal? MinBudget,
    decimal? MaxBudget,
    DateTime SubmissionDeadlineUtc,
    int? EstimatedDurationMonths,
    List<string> PrimaryDomains,
    int PublicAttachmentCount,
    int ConfidentialAttachmentCount,
    DateTime CreatedAtUtc
);

public record ChallengeDetailDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string? OrganizationLogoUrl,
    OrganizationType OrganizationType,
    bool IsOrganizationVerified,
    Guid CreatedByUserId,
    string CreatorName,
    string Title,
    string Slug,
    string Summary,
    string ProblemStatement,
    string ResearchScope,
    string ExpectedOutcomes,
    ChallengeStatus Status,
    ChallengeVisibility Visibility,
    InstitutionalEligibility Eligibility,
    string? Currency,
    decimal? MinBudget,
    decimal? MaxBudget,
    DateTime SubmissionDeadlineUtc,
    int? EstimatedDurationMonths,
    bool RequiresManualNdaApproval,
    string NdaTermsVersion,
    string? NdaTermsText,
    bool HasSignedNda,
    bool IsNdaApproved,
    DateTime? PublishedAtUtc,
    DateTime? ClosedAtUtc,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    List<ChallengeDomainDto> Domains,
    List<ChallengeExpertiseDto> RequiredExpertise,
    List<ChallengeAttachmentDto> Attachments,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record ChallengeAttachmentDto(
    Guid Id,
    Guid ChallengeId,
    string FileName,
    string FileExtension,
    string MimeType,
    long SizeBytes,
    AttachmentConfidentialityLevel ConfidentialityLevel,
    string? Description,
    bool CanDownload,
    DateTime CreatedAtUtc
);

public record ChallengeDomainDto(
    Guid Id,
    string DomainName,
    string? SubDomain,
    bool IsPrimary
);

public record ChallengeExpertiseDto(
    Guid Id,
    string SkillOrDiscipline,
    RequirementImportance ImportanceLevel,
    string? Description
);

public record ChallengeApprovalDto(
    Guid Id,
    Guid ReviewerUserId,
    string ReviewerName,
    ChallengeApprovalDecision Decision,
    string? ReviewComments,
    DateTime? DecidedAtUtc,
    DateTime CreatedAtUtc
);

public record ChallengeActivityLogDto(
    Guid Id,
    Guid ActorUserId,
    string ActorName,
    string ActionType,
    string? PreviousStatus,
    string? NewStatus,
    string? Notes,
    DateTime TimestampUtc
);

public record ChallengeNdaStatusDto(
    Guid ChallengeId,
    bool HasSignedNda,
    bool IsApproved,
    string? SignedNdaVersion,
    DateTime? AgreedAtUtc,
    bool RequiresReagreement
);

public record EligibilityCheckResultDto(
    Guid ChallengeId,
    bool IsEligible,
    InstitutionalEligibility RequiredEligibility,
    string? ResearcherAffiliationName,
    bool HasVerifiedAffiliation,
    List<string> MissingCriteria,
    List<string> MatchedExpertise
);

// Requests
public record CreateChallengeRequest(
    Guid OrganizationId,
    string Title,
    string Summary,
    string ProblemStatement,
    string ResearchScope,
    string ExpectedOutcomes,
    DateTime SubmissionDeadlineUtc,
    ChallengeVisibility Visibility = ChallengeVisibility.Public,
    InstitutionalEligibility Eligibility = InstitutionalEligibility.OpenToAll,
    string? Currency = "USD",
    decimal? MinBudget = null,
    decimal? MaxBudget = null,
    int? EstimatedDurationMonths = null,
    bool RequiresManualNdaApproval = false,
    string? NdaTermsText = null,
    List<CreateDomainRequest>? Domains = null,
    List<CreateExpertiseRequest>? Expertise = null
);

public record UpdateChallengeRequest(
    string Title,
    string Summary,
    string ProblemStatement,
    string ResearchScope,
    string ExpectedOutcomes,
    DateTime SubmissionDeadlineUtc,
    ChallengeVisibility Visibility,
    InstitutionalEligibility Eligibility,
    string? Currency,
    decimal? MinBudget,
    decimal? MaxBudget,
    int? EstimatedDurationMonths,
    bool RequiresManualNdaApproval,
    string? NdaTermsText,
    List<CreateDomainRequest>? Domains = null,
    List<CreateExpertiseRequest>? Expertise = null
);

public record CreateDomainRequest(
    string DomainName,
    string? SubDomain = null,
    bool IsPrimary = false
);

public record CreateExpertiseRequest(
    string SkillOrDiscipline,
    RequirementImportance ImportanceLevel = RequirementImportance.Required,
    string? Description = null
);

public record ChallengeDecisionRequest(
    ChallengeApprovalDecision Decision,
    string? Comments = null
);

public record CancelChallengeRequest(
    string Reason
);

public record SignNdaRequest(
    string NdaVersion,
    string TermsContentHash
);

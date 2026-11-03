using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Challenges;

public class IndustryChallenge : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string ResearchScope { get; set; } = string.Empty;
    public string ExpectedOutcomes { get; set; } = string.Empty;

    public ChallengeStatus Status { get; set; } = ChallengeStatus.Draft;
    public ChallengeVisibility Visibility { get; set; } = ChallengeVisibility.Public;
    public InstitutionalEligibility Eligibility { get; set; } = InstitutionalEligibility.OpenToAll;

    public string? Currency { get; set; } = "USD";
    public decimal? MinBudget { get; set; }
    public decimal? MaxBudget { get; set; }

    public DateTime SubmissionDeadlineUtc { get; set; }
    public int? EstimatedProjectDurationMonths { get; set; }

    public bool RequiresManualNdaApproval { get; set; } = false;
    public string NdaTermsVersion { get; set; } = "v1.0";
    public string? NdaTermsText { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // Navigation properties
    public virtual Organization? Organization { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<ChallengeAttachment> Attachments { get; set; } = new List<ChallengeAttachment>();
    public virtual ICollection<ChallengeResearchDomain> Domains { get; set; } = new List<ChallengeResearchDomain>();
    public virtual ICollection<ChallengeExpertiseRequirement> RequiredExpertise { get; set; } = new List<ChallengeExpertiseRequirement>();
    public virtual ICollection<ChallengeApproval> Approvals { get; set; } = new List<ChallengeApproval>();
    public virtual ICollection<ChallengeActivityLog> ActivityLogs { get; set; } = new List<ChallengeActivityLog>();
    public virtual ICollection<ChallengeNdaAgreement> NdaAgreements { get; set; } = new List<ChallengeNdaAgreement>();

    public IndustryChallenge()
    {
    }

    public static IndustryChallenge CreateDraft(
        Guid organizationId,
        Guid createdByUserId,
        string title,
        string slug,
        string summary,
        string problemStatement,
        string researchScope,
        string expectedOutcomes,
        DateTime submissionDeadlineUtc,
        ChallengeVisibility visibility = ChallengeVisibility.Public,
        InstitutionalEligibility eligibility = InstitutionalEligibility.OpenToAll,
        string? currency = "USD",
        decimal? minBudget = null,
        decimal? maxBudget = null,
        int? estimatedDurationMonths = null,
        bool requiresManualNdaApproval = false)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("Organization ID is required.");

        if (createdByUserId == Guid.Empty)
            throw new DomainException("Creator user ID is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Challenge title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Challenge slug is required.");

        if (string.IsNullOrWhiteSpace(summary))
            throw new DomainException("Challenge summary is required.");

        if (string.IsNullOrWhiteSpace(problemStatement))
            throw new DomainException("Problem statement is required.");

        if (string.IsNullOrWhiteSpace(researchScope))
            throw new DomainException("Research scope is required.");

        if (string.IsNullOrWhiteSpace(expectedOutcomes))
            throw new DomainException("Expected outcomes are required.");

        if (submissionDeadlineUtc <= DateTime.UtcNow)
            throw new DomainException("Submission deadline must be in the future.");

        if (minBudget.HasValue && minBudget.Value < 0)
            throw new DomainException("Minimum budget cannot be negative.");

        if (maxBudget.HasValue && maxBudget.Value < 0)
            throw new DomainException("Maximum budget cannot be negative.");

        if (minBudget.HasValue && maxBudget.HasValue && maxBudget.Value < minBudget.Value)
            throw new DomainException("Maximum budget cannot be less than minimum budget.");

        var challenge = new IndustryChallenge
        {
            OrganizationId = organizationId,
            CreatedByUserId = createdByUserId,
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Summary = summary.Trim(),
            ProblemStatement = problemStatement.Trim(),
            ResearchScope = researchScope.Trim(),
            ExpectedOutcomes = expectedOutcomes.Trim(),
            SubmissionDeadlineUtc = submissionDeadlineUtc,
            Visibility = visibility,
            Eligibility = eligibility,
            Currency = currency?.ToUpperInvariant().Trim(),
            MinBudget = minBudget,
            MaxBudget = maxBudget,
            EstimatedProjectDurationMonths = estimatedDurationMonths,
            RequiresManualNdaApproval = requiresManualNdaApproval,
            Status = ChallengeStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        challenge.LogActivity(createdByUserId, "DraftCreated", "Industry challenge draft created.");
        return challenge;
    }

    public void UpdateDraft(
        string title,
        string summary,
        string problemStatement,
        string researchScope,
        string expectedOutcomes,
        DateTime submissionDeadlineUtc,
        ChallengeVisibility visibility,
        InstitutionalEligibility eligibility,
        string? currency,
        decimal? minBudget,
        decimal? maxBudget,
        int? estimatedDurationMonths,
        bool requiresManualNdaApproval,
        Guid updatedByUserId)
    {
        if (Status != ChallengeStatus.Draft && Status != ChallengeStatus.ChangesRequested)
            throw new DomainException($"Challenge in status '{Status}' cannot be edited as a draft.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Challenge title is required.");

        if (submissionDeadlineUtc <= DateTime.UtcNow)
            throw new DomainException("Submission deadline must be in the future.");

        if (minBudget.HasValue && maxBudget.HasValue && maxBudget.Value < minBudget.Value)
            throw new DomainException("Maximum budget cannot be less than minimum budget.");

        Title = title.Trim();
        Summary = summary.Trim();
        ProblemStatement = problemStatement.Trim();
        ResearchScope = researchScope.Trim();
        ExpectedOutcomes = expectedOutcomes.Trim();
        SubmissionDeadlineUtc = submissionDeadlineUtc;
        Visibility = visibility;
        Eligibility = eligibility;
        Currency = currency?.ToUpperInvariant().Trim();
        MinBudget = minBudget;
        MaxBudget = maxBudget;
        EstimatedProjectDurationMonths = estimatedDurationMonths;
        RequiresManualNdaApproval = requiresManualNdaApproval;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(updatedByUserId, "DraftUpdated", "Challenge details updated.");
    }

    public void SubmitForApproval(Guid userId, string? notes = null)
    {
        if (Status != ChallengeStatus.Draft && Status != ChallengeStatus.ChangesRequested)
            throw new DomainException($"Cannot submit challenge for approval from status '{Status}'.");

        if (SubmissionDeadlineUtc <= DateTime.UtcNow)
            throw new DomainException("Submission deadline must be set to a future date before submission.");

        var previousStatus = Status;
        Status = ChallengeStatus.SubmittedForApproval;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(userId, "SubmittedForApproval", notes ?? "Challenge submitted for organizational / platform review.", previousStatus.ToString(), Status.ToString());
    }

    public void Approve(Guid reviewerUserId, string? notes = null)
    {
        if (Status != ChallengeStatus.SubmittedForApproval)
            throw new DomainException($"Only challenges in 'SubmittedForApproval' status can be approved. Current status: '{Status}'.");

        var previousStatus = Status;
        Status = ChallengeStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        Approvals.Add(new ChallengeApproval
        {
            ChallengeId = Id,
            ReviewerUserId = reviewerUserId,
            Decision = ChallengeApprovalDecision.Approved,
            ReviewComments = notes,
            DecidedAtUtc = DateTime.UtcNow
        });

        LogActivity(reviewerUserId, "Approved", notes ?? "Challenge approved for publication.", previousStatus.ToString(), Status.ToString());
    }

    public void RequestChanges(Guid reviewerUserId, string feedback)
    {
        if (Status != ChallengeStatus.SubmittedForApproval)
            throw new DomainException($"Cannot request changes on challenge with status '{Status}'.");

        if (string.IsNullOrWhiteSpace(feedback))
            throw new DomainException("Feedback notes are mandatory when requesting revisions.");

        var previousStatus = Status;
        Status = ChallengeStatus.ChangesRequested;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        Approvals.Add(new ChallengeApproval
        {
            ChallengeId = Id,
            ReviewerUserId = reviewerUserId,
            Decision = ChallengeApprovalDecision.ChangesRequested,
            ReviewComments = feedback.Trim(),
            DecidedAtUtc = DateTime.UtcNow
        });

        LogActivity(reviewerUserId, "ChangesRequested", feedback, previousStatus.ToString(), Status.ToString());
    }

    public void Publish(Guid publisherUserId)
    {
        if (Status != ChallengeStatus.Approved)
            throw new DomainException($"Challenge must be 'Approved' before publishing. Current status: '{Status}'.");

        if (SubmissionDeadlineUtc <= DateTime.UtcNow)
            throw new DomainException("Cannot publish challenge with an elapsed deadline.");

        var previousStatus = Status;
        Status = ChallengeStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(publisherUserId, "Published", "Challenge published to discovery network.", previousStatus.ToString(), Status.ToString());
    }

    public void OpenForProposals(Guid? actorUserId = null)
    {
        if (Status != ChallengeStatus.Published)
            throw new DomainException($"Cannot open proposals from status '{Status}'. Must be Published.");

        var previousStatus = Status;
        Status = ChallengeStatus.OpenForProposals;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId ?? CreatedByUserId, "OpenedForProposals", "Challenge is now actively accepting proposals.", previousStatus.ToString(), Status.ToString());
    }

    public void Close(Guid userId, string? notes = null)
    {
        if (Status != ChallengeStatus.Published && Status != ChallengeStatus.OpenForProposals && Status != ChallengeStatus.UnderReview)
            throw new DomainException($"Cannot close challenge from status '{Status}'.");

        var previousStatus = Status;
        Status = ChallengeStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(userId, "Closed", notes ?? "Challenge closed to further submissions.", previousStatus.ToString(), Status.ToString());
    }

    public void Cancel(Guid userId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A cancellation reason is required.");

        var previousStatus = Status;
        Status = ChallengeStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(userId, "Cancelled", $"Challenge cancelled. Reason: {reason.Trim()}", previousStatus.ToString(), Status.ToString());
    }

    public void LogActivity(Guid actorUserId, string actionType, string notes, string? previousStatus = null, string? newStatus = null)
    {
        ActivityLogs.Add(new ChallengeActivityLog
        {
            ChallengeId = Id,
            ActorUserId = actorUserId,
            ActionType = actionType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Notes = notes,
            TimestampUtc = DateTime.UtcNow
        });
    }
}

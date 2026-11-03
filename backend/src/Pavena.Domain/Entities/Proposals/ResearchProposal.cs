using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Proposals;

public class ResearchProposal : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Guid LeadResearcherId { get; set; }
    public Guid? SubmittingOrganizationId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string ResearchObjectives { get; set; } = string.Empty;
    public string ProposedMethodology { get; set; } = string.Empty;
    public string ExpectedOutcomes { get; set; } = string.Empty;

    public int ProposedTimelineMonths { get; set; }
    public decimal ProposedBudget { get; set; }
    public string Currency { get; set; } = "USD";
    public string? FundingJustification { get; set; }

    public ProposalConfidentialityLevel Confidentiality { get; set; } = ProposalConfidentialityLevel.Confidential;
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    public int SubmissionVersion { get; set; } = 1;

    public Guid? ConvertedProjectId { get; set; }

    public string? DecisionNotes { get; set; }
    public Guid? DecisionByUserId { get; set; }
    public DateTime? DecisionAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? EligibilityReviewedAtUtc { get; set; }
    public Guid? EligibilityReviewedByUserId { get; set; }
    public string? EligibilityRejectionReason { get; set; }

    public DateTime? UnderReviewAtUtc { get; set; }
    public DateTime? RevisionRequestedAtUtc { get; set; }
    public string? RevisionGuidance { get; set; }
    public DateTime? ResubmittedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // Navigation properties
    public virtual IndustryChallenge? Challenge { get; set; }
    public virtual User? LeadResearcher { get; set; }
    public virtual Organization? SubmittingOrganization { get; set; }
    public virtual ResearchProject? ConvertedProject { get; set; }
    public virtual User? DecisionByUser { get; set; }
    public virtual User? EligibilityReviewedByUser { get; set; }

    public virtual ICollection<ProposalInvestigator> Investigators { get; set; } = new List<ProposalInvestigator>();
    public virtual ICollection<ProposalResearchDomain> Domains { get; set; } = new List<ProposalResearchDomain>();
    public virtual ICollection<ProposalExpertise> ExpertiseTags { get; set; } = new List<ProposalExpertise>();
    public virtual ICollection<ProposalAttachment> Attachments { get; set; } = new List<ProposalAttachment>();
    public virtual ICollection<ProposalVersionSnapshot> VersionSnapshots { get; set; } = new List<ProposalVersionSnapshot>();
    public virtual ICollection<ProposalReviewAssignment> ReviewAssignments { get; set; } = new List<ProposalReviewAssignment>();
    public virtual ICollection<ProposalActivityLog> ActivityLogs { get; set; } = new List<ProposalActivityLog>();

    public ResearchProposal()
    {
    }

    public static ResearchProposal CreateDraft(
        Guid challengeId,
        Guid leadResearcherId,
        Guid? submittingOrganizationId,
        string title,
        string slug,
        string abstractText,
        string problemStatement,
        string researchObjectives,
        string proposedMethodology,
        string expectedOutcomes,
        int proposedTimelineMonths,
        decimal proposedBudget,
        string currency = "USD",
        string? fundingJustification = null,
        ProposalConfidentialityLevel confidentiality = ProposalConfidentialityLevel.Confidential)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("Challenge ID is required.");

        if (leadResearcherId == Guid.Empty)
            throw new DomainException("Lead researcher user ID is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Proposal title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Proposal slug is required.");

        if (proposedTimelineMonths <= 0)
            throw new DomainException("Proposed timeline must be at least 1 month.");

        if (proposedBudget < 0)
            throw new DomainException("Proposed budget cannot be negative.");

        var proposal = new ResearchProposal
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            LeadResearcherId = leadResearcherId,
            SubmittingOrganizationId = submittingOrganizationId,
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Abstract = abstractText.Trim(),
            ProblemStatement = problemStatement.Trim(),
            ResearchObjectives = researchObjectives.Trim(),
            ProposedMethodology = proposedMethodology.Trim(),
            ExpectedOutcomes = expectedOutcomes.Trim(),
            ProposedTimelineMonths = proposedTimelineMonths,
            ProposedBudget = proposedBudget,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(),
            FundingJustification = fundingJustification?.Trim(),
            Confidentiality = confidentiality,
            Status = ProposalStatus.Draft,
            SubmissionVersion = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        // Add lead researcher as lead investigator
        proposal.Investigators.Add(new ProposalInvestigator
        {
            Id = Guid.NewGuid(),
            ProposalId = proposal.Id,
            UserId = leadResearcherId,
            Role = InvestigatorRole.LeadInvestigator,
            ContributionDescription = "Principal / Lead Investigator",
            CreatedAtUtc = DateTime.UtcNow
        });

        proposal.LogActivity(leadResearcherId, ProposalActivityType.Created, $"Draft proposal '{proposal.Title}' created.");
        return proposal;
    }

    public void UpdateDraft(
        Guid actorUserId,
        string title,
        string slug,
        string abstractText,
        string problemStatement,
        string researchObjectives,
        string proposedMethodology,
        string expectedOutcomes,
        int proposedTimelineMonths,
        decimal proposedBudget,
        string currency,
        string? fundingJustification,
        ProposalConfidentialityLevel confidentiality,
        Guid? submittingOrganizationId)
    {
        EnsureLeadResearcher(actorUserId);

        if (Status != ProposalStatus.Draft && Status != ProposalStatus.RevisionRequested)
            throw new DomainException($"Cannot update proposal in '{Status}' status. Only Draft or RevisionRequested proposals can be modified.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Proposal title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Proposal slug is required.");

        if (proposedTimelineMonths <= 0)
            throw new DomainException("Proposed timeline must be at least 1 month.");

        if (proposedBudget < 0)
            throw new DomainException("Proposed budget cannot be negative.");

        Title = title.Trim();
        Slug = slug.ToLowerInvariant().Trim();
        Abstract = abstractText.Trim();
        ProblemStatement = problemStatement.Trim();
        ResearchObjectives = researchObjectives.Trim();
        ProposedMethodology = proposedMethodology.Trim();
        ExpectedOutcomes = expectedOutcomes.Trim();
        ProposedTimelineMonths = proposedTimelineMonths;
        ProposedBudget = proposedBudget;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        FundingJustification = fundingJustification?.Trim();
        Confidentiality = confidentiality;
        SubmittingOrganizationId = submittingOrganizationId;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Updated, "Proposal draft content updated.");
    }

    public void Submit(Guid actorUserId)
    {
        EnsureLeadResearcher(actorUserId);

        if (Status != ProposalStatus.Draft)
            throw new DomainException($"Only Draft proposals can be submitted. Current status: '{Status}'.");

        ValidateSubmissionContent();

        Status = ProposalStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Submitted, $"Proposal version {SubmissionVersion} submitted for review.");
    }

    public void PassEligibility(Guid actorUserId, string? notes = null)
    {
        if (Status != ProposalStatus.Submitted && Status != ProposalStatus.EligibilityReview)
            throw new DomainException($"Cannot pass eligibility from status '{Status}'. Proposal must be Submitted or in EligibilityReview.");

        Status = ProposalStatus.UnderReview;
        EligibilityReviewedAtUtc = DateTime.UtcNow;
        EligibilityReviewedByUserId = actorUserId;
        UnderReviewAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.EligibilityPassed, "Proposal passed institutional eligibility checks and entered review stage.", notes);
    }

    public void FailEligibility(Guid actorUserId, string reason)
    {
        if (Status != ProposalStatus.Submitted && Status != ProposalStatus.EligibilityReview)
            throw new DomainException($"Cannot fail eligibility from status '{Status}'.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Eligibility rejection reason is required.");

        Status = ProposalStatus.Rejected;
        EligibilityReviewedAtUtc = DateTime.UtcNow;
        EligibilityReviewedByUserId = actorUserId;
        EligibilityRejectionReason = reason.Trim();
        DecisionAtUtc = DateTime.UtcNow;
        DecisionByUserId = actorUserId;
        DecisionNotes = $"Eligibility check failed: {reason.Trim()}";
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.EligibilityFailed, $"Proposal failed eligibility review: {reason.Trim()}");
    }

    public void StartReview(Guid actorUserId)
    {
        if (Status != ProposalStatus.Submitted && Status != ProposalStatus.Resubmitted)
            throw new DomainException($"Cannot move proposal to UnderReview from '{Status}'.");

        Status = ProposalStatus.UnderReview;
        UnderReviewAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Updated, "Proposal placed into active review stage.");
    }

    public void RequestRevision(Guid actorUserId, string guidance)
    {
        if (Status != ProposalStatus.UnderReview && Status != ProposalStatus.Submitted && Status != ProposalStatus.Resubmitted)
            throw new DomainException($"Cannot request revision when proposal is in '{Status}' status.");

        if (string.IsNullOrWhiteSpace(guidance))
            throw new DomainException("Revision guidance is required.");

        Status = ProposalStatus.RevisionRequested;
        RevisionGuidance = guidance.Trim();
        RevisionRequestedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.RevisionRequested, "Revision requested by sponsor/review coordinator.");
    }

    public void Resubmit(Guid actorUserId, string changeSummary)
    {
        EnsureLeadResearcher(actorUserId);

        if (Status != ProposalStatus.RevisionRequested)
            throw new DomainException($"Only proposals in RevisionRequested status can be resubmitted. Current status: '{Status}'.");

        if (string.IsNullOrWhiteSpace(changeSummary))
            throw new DomainException("Summary of changes is required for resubmission.");

        ValidateSubmissionContent();

        SubmissionVersion += 1;
        Status = ProposalStatus.Resubmitted;
        ResubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Resubmitted, $"Proposal resubmitted as version {SubmissionVersion}. Summary: {changeSummary.Trim()}");
    }

    public void Accept(Guid actorUserId, string? decisionNotes = null)
    {
        if (Status != ProposalStatus.UnderReview && Status != ProposalStatus.Resubmitted && Status != ProposalStatus.Submitted)
            throw new DomainException($"Cannot accept proposal in '{Status}' status.");

        Status = ProposalStatus.Accepted;
        DecisionByUserId = actorUserId;
        DecisionNotes = decisionNotes?.Trim();
        DecisionAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Accepted, "Proposal approved and accepted for project award.");
    }

    public void Reject(Guid actorUserId, string? decisionNotes = null)
    {
        if (Status != ProposalStatus.UnderReview && Status != ProposalStatus.Resubmitted && Status != ProposalStatus.Submitted && Status != ProposalStatus.RevisionRequested)
            throw new DomainException($"Cannot reject proposal in '{Status}' status.");

        Status = ProposalStatus.Rejected;
        DecisionByUserId = actorUserId;
        DecisionNotes = decisionNotes?.Trim();
        DecisionAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Rejected, "Proposal rejected by challenge sponsor.");
    }

    public void Withdraw(Guid actorUserId, string? reason = null)
    {
        EnsureLeadResearcher(actorUserId);

        if (Status == ProposalStatus.Accepted || Status == ProposalStatus.ConvertedToProject)
            throw new DomainException($"Cannot withdraw proposal after it has been accepted or converted. Current status: '{Status}'.");

        Status = ProposalStatus.Withdrawn;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.Withdrawn, $"Proposal withdrawn by lead researcher. Reason: {reason?.Trim() ?? "None provided"}");
    }

    public void MarkConverted(Guid projectId, Guid actorUserId)
    {
        if (Status != ProposalStatus.Accepted && Status != ProposalStatus.ConvertedToProject)
            throw new DomainException($"Proposal must be in Accepted status to be converted into a project. Current status: '{Status}'.");

        if (projectId == Guid.Empty)
            throw new DomainException("Converted project ID is required.");

        ConvertedProjectId = projectId;
        Status = ProposalStatus.ConvertedToProject;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, ProposalActivityType.ConvertedToProject, $"Proposal successfully converted to ResearchProject ID {projectId}.");
    }

    public void AddInvestigator(Guid userId, InvestigatorRole role, string? contributionDescription = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Investigator user ID cannot be empty.");

        if (role == InvestigatorRole.LeadInvestigator && userId != LeadResearcherId)
            throw new DomainException("Only the primary submitter can be the Lead Investigator.");

        if (Investigators.Any(i => i.UserId == userId))
            throw new DomainException("This researcher is already an investigator on this proposal.");

        Investigators.Add(new ProposalInvestigator
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            UserId = userId,
            Role = role,
            ContributionDescription = contributionDescription?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveInvestigator(Guid userId)
    {
        if (userId == LeadResearcherId)
            throw new DomainException("Cannot remove the Lead Investigator from the proposal.");

        var investigator = Investigators.FirstOrDefault(i => i.UserId == userId);
        if (investigator == null)
            throw new DomainException("Investigator not found on this proposal.");

        Investigators.Remove(investigator);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddAttachment(ProposalAttachmentType type, string fileName, string storagePath, string contentType, long sizeBytes, string hashSha256, Guid uploadedByUserId)
    {
        Attachments.Add(new ProposalAttachment
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            AttachmentType = type,
            FileName = fileName.Trim(),
            StoragePath = storagePath.Trim(),
            ContentType = contentType.Trim(),
            FileSizeBytes = sizeBytes,
            FileHashSha256 = hashSha256.Trim(),
            UploadedByUserId = uploadedByUserId,
            UploadedAtUtc = DateTime.UtcNow
        });

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveAttachment(Guid attachmentId)
    {
        var att = Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (att == null)
            throw new DomainException("Attachment not found on this proposal.");

        Attachments.Remove(att);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddSnapshot(int versionNumber, string snapshotDataJson, string? changeSummary, Guid createdByUserId)
    {
        VersionSnapshots.Add(new ProposalVersionSnapshot
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            VersionNumber = versionNumber,
            SnapshotDataJson = snapshotDataJson,
            ChangeSummary = changeSummary?.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public void LogActivity(Guid actorUserId, ProposalActivityType type, string description, string? metadataJson = null)
    {
        ActivityLogs.Add(new ProposalActivityLog
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            ActorUserId = actorUserId,
            ActivityType = type,
            Description = description,
            MetadataJson = metadataJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public void EnsureLeadResearcher(Guid userId)
    {
        if (LeadResearcherId != userId)
            throw new DomainException("Only the Lead Researcher is authorized to perform this proposal action.");
    }

    private void ValidateSubmissionContent()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new DomainException("Proposal title is required before submission.");

        if (string.IsNullOrWhiteSpace(Abstract))
            throw new DomainException("Proposal abstract is required before submission.");

        if (string.IsNullOrWhiteSpace(ProblemStatement))
            throw new DomainException("Problem statement is required before submission.");

        if (string.IsNullOrWhiteSpace(ResearchObjectives))
            throw new DomainException("Research objectives are required before submission.");

        if (string.IsNullOrWhiteSpace(ProposedMethodology))
            throw new DomainException("Proposed methodology is required before submission.");

        if (string.IsNullOrWhiteSpace(ExpectedOutcomes))
            throw new DomainException("Expected outcomes are required before submission.");

        if (ProposedTimelineMonths <= 0)
            throw new DomainException("Valid proposed timeline in months is required.");

        if (ProposedBudget <= 0)
            throw new DomainException("Proposed budget must be greater than zero.");
    }
}

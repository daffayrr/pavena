using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Challenges;

public class ChallengeAttachment : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;

    public AttachmentConfidentialityLevel ConfidentialityLevel { get; set; } = AttachmentConfidentialityLevel.PublicBriefing;
    public string? Description { get; set; }
    public Guid UploadedByUserId { get; set; }

    public virtual IndustryChallenge? Challenge { get; set; }
    public virtual User? UploadedByUser { get; set; }

    public ChallengeAttachment()
    {
    }

    public static ChallengeAttachment Create(
        Guid challengeId,
        string fileName,
        string fileExtension,
        string mimeType,
        long sizeBytes,
        string storageKey,
        AttachmentConfidentialityLevel confidentialityLevel,
        Guid uploadedByUserId,
        string? description = null)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("Challenge ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("Storage key cannot be empty.");

        if (sizeBytes <= 0)
            throw new DomainException("Attachment size must be greater than zero.");

        return new ChallengeAttachment
        {
            ChallengeId = challengeId,
            FileName = fileName.Trim(),
            FileExtension = fileExtension.ToLowerInvariant().Trim(),
            MimeType = mimeType.ToLowerInvariant().Trim(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey.Trim(),
            ConfidentialityLevel = confidentialityLevel,
            UploadedByUserId = uploadedByUserId,
            Description = description?.Trim()
        };
    }
}

public class ChallengeResearchDomain : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public string? SubDomain { get; set; }
    public bool IsPrimary { get; set; } = false;

    public virtual IndustryChallenge? Challenge { get; set; }

    public ChallengeResearchDomain()
    {
    }

    public ChallengeResearchDomain(Guid challengeId, string domainName, string? subDomain = null, bool isPrimary = false)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("Challenge ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(domainName))
            throw new DomainException("Research domain name cannot be empty.");

        ChallengeId = challengeId;
        DomainName = domainName.Trim();
        SubDomain = subDomain?.Trim();
        IsPrimary = isPrimary;
    }
}

public class ChallengeExpertiseRequirement : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public string SkillOrDiscipline { get; set; } = string.Empty;
    public RequirementImportance ImportanceLevel { get; set; } = RequirementImportance.Required;
    public string? Description { get; set; }

    public virtual IndustryChallenge? Challenge { get; set; }

    public ChallengeExpertiseRequirement()
    {
    }

    public ChallengeExpertiseRequirement(Guid challengeId, string skillOrDiscipline, RequirementImportance importance = RequirementImportance.Required, string? description = null)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("Challenge ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(skillOrDiscipline))
            throw new DomainException("Skill or discipline name cannot be empty.");

        ChallengeId = challengeId;
        SkillOrDiscipline = skillOrDiscipline.Trim();
        ImportanceLevel = importance;
        Description = description?.Trim();
    }
}

public class ChallengeApproval : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public ChallengeApprovalDecision Decision { get; set; } = ChallengeApprovalDecision.Pending;
    public string? ReviewComments { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public virtual IndustryChallenge? Challenge { get; set; }
    public virtual User? ReviewerUser { get; set; }

    public ChallengeApproval()
    {
    }
}

public class ChallengeActivityLog : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public virtual IndustryChallenge? Challenge { get; set; }
    public virtual User? ActorUser { get; set; }

    public ChallengeActivityLog()
    {
    }
}

public class ChallengeNdaAgreement : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Guid ResearcherUserId { get; set; }
    public Guid? ResearcherOrganizationId { get; set; }

    public string NdaVersion { get; set; } = "v1.0";
    public string TermsContentHash { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime AgreedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsApprovedBySponsor { get; set; } = true;
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public virtual IndustryChallenge? Challenge { get; set; }
    public virtual User? ResearcherUser { get; set; }
    public virtual User? ApprovedByUser { get; set; }

    public ChallengeNdaAgreement()
    {
    }

    public static ChallengeNdaAgreement Create(
        Guid challengeId,
        Guid researcherUserId,
        string ndaVersion,
        string termsContentHash,
        string? ipAddress,
        bool requiresManualApproval,
        Guid? researcherOrgId = null)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("Challenge ID cannot be empty.");

        if (researcherUserId == Guid.Empty)
            throw new DomainException("Researcher user ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(termsContentHash))
            throw new DomainException("NDA terms content hash is required.");

        return new ChallengeNdaAgreement
        {
            ChallengeId = challengeId,
            ResearcherUserId = researcherUserId,
            ResearcherOrganizationId = researcherOrgId,
            NdaVersion = ndaVersion,
            TermsContentHash = termsContentHash,
            IpAddress = ipAddress,
            AgreedAtUtc = DateTime.UtcNow,
            IsApprovedBySponsor = !requiresManualApproval,
            ApprovedAtUtc = requiresManualApproval ? null : DateTime.UtcNow
        };
    }

    public void Approve(Guid sponsorUserId)
    {
        IsApprovedBySponsor = true;
        ApprovedAtUtc = DateTime.UtcNow;
        ApprovedByUserId = sponsorUserId;
    }
}

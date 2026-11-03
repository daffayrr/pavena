using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Challenges.Interfaces;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Challenges;

public class ChallengeService : IChallengeService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IChallengeNdaService _ndaService;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        IPavenaDbContext dbContext,
        IChallengeNdaService ndaService,
        ILogger<ChallengeService> logger)
    {
        _dbContext = dbContext;
        _ndaService = ndaService;
        _logger = logger;
    }

    public async Task<ChallengeDetailDto> CreateChallengeAsync(Guid currentUserId, CreateChallengeRequest request, CancellationToken ct = default)
    {
        var org = await _dbContext.Organizations
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, ct);

        if (org == null)
            throw new KeyNotFoundException("Organization not found.");

        if (org.VerificationStatus != OrganizationVerificationStatus.Verified)
            throw new DomainException("Only verified organizations can create research challenges.");

        var membership = org.Memberships.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive);
        var isPlatformAdmin = await IsPlatformAdminAsync(currentUserId, ct);

        if (!isPlatformAdmin && (membership == null || (membership.Role != OrgMembershipRole.OrganizationAdmin && membership.Role != OrgMembershipRole.Employee)))
        {
            throw new UnauthorizedAccessException("You must be an active administrator or employee of this organization to create challenges.");
        }

        var baseSlug = GenerateSlug(request.Title);
        var slug = await EnsureUniqueSlugAsync(baseSlug, ct);

        var challenge = IndustryChallenge.CreateDraft(
            request.OrganizationId,
            currentUserId,
            request.Title,
            slug,
            request.Summary,
            request.ProblemStatement,
            request.ResearchScope,
            request.ExpectedOutcomes,
            request.SubmissionDeadlineUtc,
            request.Visibility,
            request.Eligibility,
            request.Currency,
            request.MinBudget,
            request.MaxBudget,
            request.EstimatedDurationMonths,
            request.RequiresManualNdaApproval
        );

        challenge.NdaTermsText = request.NdaTermsText;

        if (request.Domains != null && request.Domains.Count > 0)
        {
            foreach (var d in request.Domains)
            {
                challenge.Domains.Add(new ChallengeResearchDomain(challenge.Id, d.DomainName, d.SubDomain, d.IsPrimary));
            }
        }

        if (request.Expertise != null && request.Expertise.Count > 0)
        {
            foreach (var e in request.Expertise)
            {
                challenge.RequiredExpertise.Add(new ChallengeExpertiseRequirement(challenge.Id, e.SkillOrDiscipline, e.ImportanceLevel, e.Description));
            }
        }

        _dbContext.IndustryChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Challenge {ChallengeId} '{Title}' created by user {UserId} for org {OrgId}.",
            challenge.Id, challenge.Title, currentUserId, challenge.OrganizationId);

        return await MapToDetailDtoAsync(challenge, currentUserId, ct);
    }

    public async Task<ChallengeDetailDto> UpdateChallengeAsync(Guid currentUserId, Guid challengeId, UpdateChallengeRequest request, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Domains)
            .Include(c => c.RequiredExpertise)
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanEditChallengeAsync(currentUserId, challenge, ct);

        challenge.UpdateDraft(
            request.Title,
            request.Summary,
            request.ProblemStatement,
            request.ResearchScope,
            request.ExpectedOutcomes,
            request.SubmissionDeadlineUtc,
            request.Visibility,
            request.Eligibility,
            request.Currency,
            request.MinBudget,
            request.MaxBudget,
            request.EstimatedDurationMonths,
            request.RequiresManualNdaApproval,
            currentUserId
        );

        challenge.NdaTermsText = request.NdaTermsText;

        // Synchronize domains
        if (request.Domains != null)
        {
            challenge.Domains.Clear();
            foreach (var d in request.Domains)
            {
                challenge.Domains.Add(new ChallengeResearchDomain(challenge.Id, d.DomainName, d.SubDomain, d.IsPrimary));
            }
        }

        // Synchronize expertise
        if (request.Expertise != null)
        {
            challenge.RequiredExpertise.Clear();
            foreach (var e in request.Expertise)
            {
                challenge.RequiredExpertise.Add(new ChallengeExpertiseRequirement(challenge.Id, e.SkillOrDiscipline, e.ImportanceLevel, e.Description));
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return await MapToDetailDtoAsync(challenge, currentUserId, ct);
    }

    public async Task<ChallengeDetailDto> GetChallengeByIdAsync(Guid? currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .Include(c => c.CreatedByUser).ThenInclude(u => u!.Profile)
            .Include(c => c.Domains)
            .Include(c => c.RequiredExpertise)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanViewChallengeAsync(currentUserId, challenge, ct);

        return await MapToDetailDtoAsync(challenge, currentUserId, ct);
    }

    public async Task<ChallengeDetailDto> GetChallengeBySlugAsync(Guid? currentUserId, string slug, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .Include(c => c.CreatedByUser).ThenInclude(u => u!.Profile)
            .Include(c => c.Domains)
            .Include(c => c.RequiredExpertise)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant().Trim(), ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanViewChallengeAsync(currentUserId, challenge, ct);

        return await MapToDetailDtoAsync(challenge, currentUserId, ct);
    }

    public async Task<List<ChallengeSummaryDto>> SearchChallengesAsync(
        Guid? currentUserId,
        string? query = null,
        string? domain = null,
        ChallengeStatus? status = null,
        InstitutionalEligibility? eligibility = null,
        decimal? maxBudget = null,
        Guid? organizationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var queryable = _dbContext.IndustryChallenges
            .AsNoTracking()
            .Include(c => c.Organization)
            .Include(c => c.Domains)
            .Include(c => c.Attachments)
            .AsQueryable();

        // Status filter: Public queries show only Published and OpenForProposals by default
        if (organizationId.HasValue)
        {
            queryable = queryable.Where(c => c.OrganizationId == organizationId.Value);
            if (status.HasValue)
            {
                queryable = queryable.Where(c => c.Status == status.Value);
            }
        }
        else
        {
            if (status.HasValue)
            {
                queryable = queryable.Where(c => c.Status == status.Value);
            }
            else
            {
                queryable = queryable.Where(c => c.Status == ChallengeStatus.Published || c.Status == ChallengeStatus.OpenForProposals);
            }

            // Visibility filter
            if (!currentUserId.HasValue)
            {
                queryable = queryable.Where(c => c.Visibility == ChallengeVisibility.Public);
            }
            else
            {
                queryable = queryable.Where(c => c.Visibility == ChallengeVisibility.Public || c.Visibility == ChallengeVisibility.AuthenticatedUsersOnly);
            }
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            queryable = queryable.Where(c => c.Title.ToLower().Contains(q) || c.Summary.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(domain))
        {
            var d = domain.Trim().ToLower();
            queryable = queryable.Where(c => c.Domains.Any(dm => dm.DomainName.ToLower().Contains(d)));
        }

        if (eligibility.HasValue)
        {
            queryable = queryable.Where(c => c.Eligibility == eligibility.Value);
        }

        if (maxBudget.HasValue)
        {
            queryable = queryable.Where(c => c.MinBudget == null || c.MinBudget <= maxBudget.Value);
        }

        var results = await queryable
            .OrderByDescending(c => c.PublishedAtUtc ?? c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return results.Select(c => new ChallengeSummaryDto(
            c.Id,
            c.OrganizationId,
            c.Organization?.Name ?? "Organization",
            c.Organization?.LogoUrl,
            c.Organization?.Type ?? OrganizationType.Company,
            c.Title,
            c.Slug,
            c.Summary,
            c.Status,
            c.Visibility,
            c.Eligibility,
            c.Currency,
            c.MinBudget,
            c.MaxBudget,
            c.SubmissionDeadlineUtc,
            c.EstimatedProjectDurationMonths,
            c.Domains.Where(d => d.IsPrimary).Select(d => d.DomainName).ToList(),
            c.Attachments.Count(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.PublicBriefing),
            c.Attachments.Count(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.ConfidentialNdaRequired),
            c.CreatedAtUtc
        )).ToList();
    }

    public async Task SubmitForApprovalAsync(Guid currentUserId, Guid challengeId, string? notes = null, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .Include(c => c.Domains)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanEditChallengeAsync(currentUserId, challenge, ct);

        if (!challenge.Domains.Any(d => d.IsPrimary))
            throw new DomainException("At least one primary research domain is required before submitting for approval.");

        challenge.SubmitForApproval(currentUserId, notes);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ProcessApprovalDecisionAsync(Guid currentUserId, Guid challengeId, ChallengeDecisionRequest request, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanApproveChallengeAsync(currentUserId, challenge, ct);

        if (request.Decision == ChallengeApprovalDecision.Approved)
        {
            challenge.Approve(currentUserId, request.Comments);
        }
        else if (request.Decision == ChallengeApprovalDecision.ChangesRequested)
        {
            challenge.RequestChanges(currentUserId, request.Comments ?? "Revisions requested.");
        }
        else if (request.Decision == ChallengeApprovalDecision.Rejected)
        {
            challenge.Cancel(currentUserId, request.Comments ?? "Challenge proposal rejected.");
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task PublishChallengeAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanPublishChallengeAsync(currentUserId, challenge, ct);

        challenge.Publish(currentUserId);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task OpenForProposalsAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanPublishChallengeAsync(currentUserId, challenge, ct);

        challenge.OpenForProposals(currentUserId);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task CloseChallengeAsync(Guid currentUserId, Guid challengeId, string? notes = null, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanPublishChallengeAsync(currentUserId, challenge, ct);

        challenge.Close(currentUserId, notes);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task CancelChallengeAsync(Guid currentUserId, Guid challengeId, string reason, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanPublishChallengeAsync(currentUserId, challenge, ct);

        challenge.Cancel(currentUserId, reason);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteDraftChallengeAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanEditChallengeAsync(currentUserId, challenge, ct);

        if (challenge.Status != ChallengeStatus.Draft && challenge.Status != ChallengeStatus.Cancelled)
        {
            throw new DomainException($"Cannot delete challenge with status '{challenge.Status}'. Only Drafts or Cancelled challenges can be permanently removed.");
        }

        _dbContext.IndustryChallenges.Remove(challenge);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<EligibilityCheckResultDto> CheckEligibilityAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.RequiredExpertise)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        var userExists = await _dbContext.Users.AnyAsync(u => u.Id == currentUserId, ct);
        if (!userExists)
            throw new KeyNotFoundException("User not found.");

        // Check active verified affiliations
        var verifiedMemberships = await _dbContext.OrganizationMemberships
            .Include(m => m.Organization)
            .Where(m => m.UserId == currentUserId && m.IsActive && m.Organization!.VerificationStatus == OrganizationVerificationStatus.Verified)
            .ToListAsync(ct);

        var primaryAffiliation = verifiedMemberships.FirstOrDefault();
        var affiliationName = primaryAffiliation?.Organization?.Name;
        var hasVerifiedAffiliation = verifiedMemberships.Count > 0;

        var missing = new List<string>();
        var isEligible = true;

        switch (challenge.Eligibility)
        {
            case InstitutionalEligibility.UniversitiesOnly:
                var hasUniversity = verifiedMemberships.Any(m => m.Organization!.Type == OrganizationType.University);
                if (!hasUniversity)
                {
                    isEligible = false;
                    missing.Add("Requires verified academic affiliation with a recognized University.");
                }
                break;

            case InstitutionalEligibility.AccreditedResearchInstitutesOnly:
                var hasResearchOrUni = verifiedMemberships.Any(m =>
                    m.Organization!.Type == OrganizationType.University ||
                    m.Organization!.Type == OrganizationType.ResearchInstitution ||
                    m.Organization!.Type == OrganizationType.IndependentResearchOrganization);
                if (!hasResearchOrUni)
                {
                    isEligible = false;
                    missing.Add("Requires verified affiliation with an accredited Research Institute or University.");
                }
                break;

            case InstitutionalEligibility.ConsortiumRequired:
                // Lead researcher can start drafting, but proposal submission requires multi-org team
                break;

            case InstitutionalEligibility.OpenToAll:
            default:
                // All verified individual researchers are eligible
                break;
        }

        // Matched expertise
        var userSkills = await _dbContext.UserSkills
            .AsNoTracking()
            .Where(s => s.UserId == currentUserId)
            .Select(s => s.SkillName.ToLower().Trim())
            .ToListAsync(ct);
        var userSkillNames = userSkills.ToHashSet();
        var matched = new List<string>();

        foreach (var req in challenge.RequiredExpertise)
        {
            if (userSkillNames.Contains(req.SkillOrDiscipline.ToLower().Trim()))
            {
                matched.Add(req.SkillOrDiscipline);
            }
            else if (req.ImportanceLevel == RequirementImportance.Required)
            {
                missing.Add($"Recommended skill: {req.SkillOrDiscipline}");
            }
        }

        return new EligibilityCheckResultDto(
            challenge.Id,
            isEligible,
            challenge.Eligibility,
            affiliationName,
            hasVerifiedAffiliation,
            missing,
            matched
        );
    }

    public async Task<List<ChallengeActivityLogDto>> GetActivityLogsAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization).ThenInclude(o => o!.Memberships)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanEditChallengeAsync(currentUserId, challenge, ct);

        var logs = await _dbContext.ChallengeActivityLogs
            .AsNoTracking()
            .Include(l => l.ActorUser).ThenInclude(u => u!.Profile)
            .Where(l => l.ChallengeId == challengeId)
            .OrderByDescending(l => l.TimestampUtc)
            .ToListAsync(ct);

        return logs.Select(l => new ChallengeActivityLogDto(
            l.Id,
            l.ActorUserId,
            l.ActorUser?.Profile?.DisplayName ?? l.ActorUser?.UserName ?? "User",
            l.ActionType,
            l.PreviousStatus,
            l.NewStatus,
            l.Notes,
            l.TimestampUtc
        )).ToList();
    }

    // Authorization & Visibility Helpers
    private async Task AssertCanViewChallengeAsync(Guid? currentUserId, IndustryChallenge challenge, CancellationToken ct)
    {
        // Public or Authenticated challenges in published states
        if (challenge.Status == ChallengeStatus.Published ||
            challenge.Status == ChallengeStatus.OpenForProposals ||
            challenge.Status == ChallengeStatus.UnderReview ||
            challenge.Status == ChallengeStatus.Awarded ||
            challenge.Status == ChallengeStatus.Closed)
        {
            if (challenge.Visibility == ChallengeVisibility.Public)
                return;

            if (challenge.Visibility == ChallengeVisibility.AuthenticatedUsersOnly && currentUserId.HasValue)
                return;
        }

        // Otherwise requires sponsor membership or admin privileges; unauthorized receives 404 (concealment)
        if (!currentUserId.HasValue)
            throw new KeyNotFoundException("Challenge not found.");

        var isPlatformAdmin = await IsPlatformAdminAsync(currentUserId.Value, ct);
        if (isPlatformAdmin) return;

        var isMember = await _dbContext.OrganizationMemberships
            .AnyAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == currentUserId.Value && m.IsActive, ct);

        if (!isMember)
            throw new KeyNotFoundException("Challenge not found.");
    }

    private async Task AssertCanEditChallengeAsync(Guid currentUserId, IndustryChallenge challenge, CancellationToken ct)
    {
        var isPlatformAdmin = await IsPlatformAdminAsync(currentUserId, ct);
        if (isPlatformAdmin) return;

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == currentUserId && m.IsActive, ct);

        if (membership == null)
            throw new UnauthorizedAccessException("You are not an active member of the owning organization.");

        var isOwnerOrAdmin = membership.Role == OrgMembershipRole.OrganizationAdmin;
        var isCreator = challenge.CreatedByUserId == currentUserId;

        if (!isOwnerOrAdmin && !isCreator)
            throw new UnauthorizedAccessException("You do not have permission to edit this challenge.");
    }

    private async Task AssertCanApproveChallengeAsync(Guid currentUserId, IndustryChallenge challenge, CancellationToken ct)
    {
        var isPlatformAdmin = await IsPlatformAdminAsync(currentUserId, ct);
        if (isPlatformAdmin) return;

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == currentUserId && m.IsActive, ct);

        if (membership == null || membership.Role != OrgMembershipRole.OrganizationAdmin)
            throw new UnauthorizedAccessException("Only organization administrators or platform administrators can approve challenges.");
    }

    private async Task AssertCanPublishChallengeAsync(Guid currentUserId, IndustryChallenge challenge, CancellationToken ct)
    {
        var isPlatformAdmin = await IsPlatformAdminAsync(currentUserId, ct);
        if (isPlatformAdmin) return;

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == currentUserId && m.IsActive, ct);

        if (membership == null || membership.Role != OrgMembershipRole.OrganizationAdmin)
            throw new UnauthorizedAccessException("Only organization administrators can publish or modify published challenge lifecycles.");
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        var roles = await (from ur in _dbContext.UserPlatformRoles
                           join r in _dbContext.PlatformRoles on ur.RoleId equals r.Id
                           where ur.UserId == userId
                           select r.Name).ToListAsync(ct);

        return roles.Contains(PlatformRole.Superadmin) || roles.Contains(PlatformRole.PlatformAdmin);
    }

    private async Task<ChallengeDetailDto> MapToDetailDtoAsync(IndustryChallenge c, Guid? currentUserId, CancellationToken ct)
    {
        var hasSignedNda = false;
        var isNdaApproved = false;

        if (currentUserId.HasValue)
        {
            var ndaStatus = await _ndaService.GetNdaStatusAsync(currentUserId.Value, c.Id, ct);
            hasSignedNda = ndaStatus.HasSignedNda;
            isNdaApproved = ndaStatus.IsApproved;
        }

        var creator = c.CreatedByUser?.Profile?.DisplayName ?? c.CreatedByUser?.UserName ?? "Creator";

        var attachmentsDto = c.Attachments.Select(a =>
        {
            var canDownload = a.ConfidentialityLevel == AttachmentConfidentialityLevel.PublicBriefing ||
                              (hasSignedNda && isNdaApproved);

            return new ChallengeAttachmentDto(
                a.Id,
                a.ChallengeId,
                a.FileName,
                a.FileExtension,
                a.MimeType,
                a.SizeBytes,
                a.ConfidentialityLevel,
                a.Description,
                canDownload,
                a.CreatedAtUtc
            );
        }).ToList();

        return new ChallengeDetailDto(
            c.Id,
            c.OrganizationId,
            c.Organization?.Name ?? "Organization",
            c.Organization?.LogoUrl,
            c.Organization?.Type ?? OrganizationType.Company,
            c.Organization?.VerificationStatus == OrganizationVerificationStatus.Verified,
            c.CreatedByUserId,
            creator,
            c.Title,
            c.Slug,
            c.Summary,
            c.ProblemStatement,
            c.ResearchScope,
            c.ExpectedOutcomes,
            c.Status,
            c.Visibility,
            c.Eligibility,
            c.Currency,
            c.MinBudget,
            c.MaxBudget,
            c.SubmissionDeadlineUtc,
            c.EstimatedProjectDurationMonths,
            c.RequiresManualNdaApproval,
            c.NdaTermsVersion,
            c.NdaTermsText,
            hasSignedNda,
            isNdaApproved,
            c.PublishedAtUtc,
            c.ClosedAtUtc,
            c.CancelledAtUtc,
            c.CancellationReason,
            c.Domains.Select(d => new ChallengeDomainDto(d.Id, d.DomainName, d.SubDomain, d.IsPrimary)).ToList(),
            c.RequiredExpertise.Select(e => new ChallengeExpertiseDto(e.Id, e.SkillOrDiscipline, e.ImportanceLevel, e.Description)).ToList(),
            attachmentsDto,
            c.CreatedAtUtc,
            c.UpdatedAtUtc
        );
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await _dbContext.IndustryChallenges.AnyAsync(c => c.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }
}

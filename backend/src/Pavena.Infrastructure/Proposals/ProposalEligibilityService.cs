using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Proposals.DTOs;
using Pavena.Application.Proposals.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Proposals;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Proposals;

public class ProposalEligibilityService : IProposalEligibilityService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly ILogger<ProposalEligibilityService> _logger;

    public ProposalEligibilityService(IPavenaDbContext dbContext, ILogger<ProposalEligibilityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EligibilityCheckResult> EvaluateEligibilityAsync(Guid proposalId, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Challenge).ThenInclude(c => c!.Organization)
            .Include(p => p.Challenge).ThenInclude(c => c!.Attachments)
            .Include(p => p.LeadResearcher).ThenInclude(u => u!.Profile)
            .Include(p => p.Investigators).ThenInclude(i => i.User).ThenInclude(u => u!.Profile)
            .Include(p => p.SubmittingOrganization)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        var challenge = proposal.Challenge;
        if (challenge == null)
            throw new KeyNotFoundException("Associated challenge not found.");

        var failureReasons = new List<string>();

        // 1. Challenge status and deadline check
        if (challenge.Status != ChallengeStatus.OpenForProposals && challenge.Status != ChallengeStatus.Published)
        {
            failureReasons.Add($"Challenge is not open for proposals (Current status: {challenge.Status}).");
        }

        if (challenge.SubmissionDeadlineUtc < DateTime.UtcNow)
        {
            failureReasons.Add($"Challenge submission deadline ({challenge.SubmissionDeadlineUtc:u}) has elapsed.");
        }

        // 2. NDA compliance
        bool hasAgreedNda = true;
        if (challenge.RequiresManualNdaApproval || challenge.Attachments.Any(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.ConfidentialNdaRequired))
        {
            var ndaAgreement = await _dbContext.ChallengeNdaAgreements
                .FirstOrDefaultAsync(n => n.ChallengeId == challenge.Id && n.ResearcherUserId == proposal.LeadResearcherId, ct);

            if (ndaAgreement == null)
            {
                hasAgreedNda = false;
                failureReasons.Add("Digital NDA agreement has not been executed by the lead researcher.");
            }
            else if (challenge.RequiresManualNdaApproval && !ndaAgreement.IsApprovedBySponsor)
            {
                hasAgreedNda = false;
                failureReasons.Add("Digital NDA agreement is awaiting sponsor approval.");
            }
            else if (ndaAgreement.NdaVersion != challenge.NdaTermsVersion)
            {
                hasAgreedNda = false;
                failureReasons.Add($"A new version of the challenge NDA ({challenge.NdaTermsVersion}) must be accepted.");
            }
        }

        // 3. Institutional Affiliation Eligibility
        bool meetsInstitutionalAffiliation = true;
        var leadOrgMemberships = await _dbContext.OrganizationMemberships
            .Include(m => m.Organization)
            .Where(m => m.UserId == proposal.LeadResearcherId && m.IsActive)
            .ToListAsync(ct);

        switch (challenge.Eligibility)
        {
            case InstitutionalEligibility.OpenToAll:
                meetsInstitutionalAffiliation = true;
                break;

            case InstitutionalEligibility.UniversitiesOnly:
                var hasVerifiedUniversity = leadOrgMemberships.Any(m =>
                    m.Organization != null &&
                    m.Organization.Type == OrganizationType.University &&
                    m.Organization.VerificationStatus == OrganizationVerificationStatus.Verified);

                var submittingOrgIsUni = proposal.SubmittingOrganization != null &&
                    proposal.SubmittingOrganization.Type == OrganizationType.University &&
                    proposal.SubmittingOrganization.VerificationStatus == OrganizationVerificationStatus.Verified;

                if (!hasVerifiedUniversity && !submittingOrgIsUni)
                {
                    meetsInstitutionalAffiliation = false;
                    failureReasons.Add("Challenge requires institutional affiliation with an accredited and verified University.");
                }
                break;

            case InstitutionalEligibility.AccreditedResearchInstitutesOnly:
                var hasVerifiedResearchInst = leadOrgMemberships.Any(m =>
                    m.Organization != null &&
                    (m.Organization.Type == OrganizationType.University ||
                     m.Organization.Type == OrganizationType.ResearchInstitution ||
                     m.Organization.Type == OrganizationType.IndependentResearchOrganization) &&
                    m.Organization.VerificationStatus == OrganizationVerificationStatus.Verified);

                var submittingOrgIsInst = proposal.SubmittingOrganization != null &&
                    (proposal.SubmittingOrganization.Type == OrganizationType.University ||
                     proposal.SubmittingOrganization.Type == OrganizationType.ResearchInstitution ||
                     proposal.SubmittingOrganization.Type == OrganizationType.IndependentResearchOrganization) &&
                    proposal.SubmittingOrganization.VerificationStatus == OrganizationVerificationStatus.Verified;

                if (!hasVerifiedResearchInst && !submittingOrgIsInst)
                {
                    meetsInstitutionalAffiliation = false;
                    failureReasons.Add("Challenge requires affiliation with an accredited University or verified Research Institute.");
                }
                break;

            case InstitutionalEligibility.ConsortiumRequired:
                var allInvestigatorIds = proposal.Investigators.Select(i => i.UserId).Concat(new[] { proposal.LeadResearcherId }).Distinct().ToList();
                var distinctVerifiedOrgs = await _dbContext.OrganizationMemberships
                    .Where(m => allInvestigatorIds.Contains(m.UserId) && m.IsActive && m.Organization!.VerificationStatus == OrganizationVerificationStatus.Verified)
                    .Select(m => m.OrganizationId)
                    .Distinct()
                    .CountAsync(ct);

                if (distinctVerifiedOrgs < 2)
                {
                    meetsInstitutionalAffiliation = false;
                    failureReasons.Add("Consortium required: Proposal research team must include investigators from at least two distinct verified institutions.");
                }
                break;
        }

        // 4. Profile completeness
        var profile = proposal.LeadResearcher?.Profile;
        bool isProfileComplete = profile != null &&
                                 !string.IsNullOrWhiteSpace(profile.Headline) &&
                                 !string.IsNullOrWhiteSpace(profile.Bio);

        if (!isProfileComplete)
        {
            failureReasons.Add("Lead researcher profile is incomplete (Headline and Bio are required).");
        }

        var isEligible = failureReasons.Count == 0;
        return new EligibilityCheckResult(
            isEligible,
            failureReasons,
            hasAgreedNda,
            meetsInstitutionalAffiliation,
            isProfileComplete);
    }

    public async Task<ProposalDetailResponse> RecordEligibilityReviewAsync(Guid proposalId, ReviewEligibilityRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Challenge).ThenInclude(c => c!.Organization)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        var isPlatformAdmin = await IsPlatformAdminAsync(actorUserId, ct);
        var isSponsorAdmin = false;
        if (proposal.Challenge?.OrganizationId != null)
        {
            isSponsorAdmin = await _dbContext.OrganizationMemberships.AnyAsync(
                m => m.OrganizationId == proposal.Challenge.OrganizationId &&
                     m.UserId == actorUserId &&
                     m.IsActive &&
                     m.Role == OrgMembershipRole.OrganizationAdmin,
                ct);
        }

        if (!isPlatformAdmin && !isSponsorAdmin)
            throw new UnauthorizedAccessException("Only the sponsoring organization administrator or platform admin can record eligibility reviews.");

        if (request.Passed)
        {
            proposal.PassEligibility(actorUserId, request.ReasonOrNotes);
            _logger.LogInformation("Proposal {ProposalId} marked eligible by user {UserId}", proposalId, actorUserId);
        }
        else
        {
            proposal.FailEligibility(actorUserId, request.ReasonOrNotes ?? "Failed institutional eligibility criteria.");
            _logger.LogInformation("Proposal {ProposalId} failed eligibility by user {UserId}. Reason: {Reason}", proposalId, actorUserId, request.ReasonOrNotes);
        }

        await _dbContext.SaveChangesAsync(ct);

        // Fetch detail response through proposal service logic
        var proposalService = new ProposalService(_dbContext, this, null!, _logger as ILogger<ProposalService> ?? new LoggerFactory().CreateLogger<ProposalService>());
        return (await proposalService.GetByIdAsync(proposalId, actorUserId, ct))!;
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct)
    {
        var adminRoles = new[] { PlatformRole.Superadmin, PlatformRole.PlatformAdmin };
        return await _dbContext.UserPlatformRoles
            .Join(_dbContext.PlatformRoles, upr => upr.RoleId, r => r.Id, (upr, r) => new { upr.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && adminRoles.Contains(x.Name!), ct);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Challenges.Interfaces;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Challenges;

public class ChallengeNdaService : IChallengeNdaService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly ILogger<ChallengeNdaService> _logger;

    public ChallengeNdaService(IPavenaDbContext dbContext, ILogger<ChallengeNdaService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ChallengeNdaStatusDto> GetNdaStatusAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        var agreement = await _dbContext.ChallengeNdaAgreements
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.ChallengeId == challengeId && n.ResearcherUserId == currentUserId, ct);

        if (agreement == null)
        {
            return new ChallengeNdaStatusDto(challengeId, false, false, null, null, false);
        }

        var requiresReagreement = !string.Equals(agreement.NdaVersion, challenge.NdaTermsVersion, StringComparison.OrdinalIgnoreCase);

        return new ChallengeNdaStatusDto(
            challengeId,
            true,
            agreement.IsApprovedBySponsor && !requiresReagreement,
            agreement.NdaVersion,
            agreement.AgreedAtUtc,
            requiresReagreement
        );
    }

    public async Task<ChallengeNdaStatusDto> SignNdaAsync(
        Guid currentUserId,
        Guid challengeId,
        SignNdaRequest request,
        string? clientIp = null,
        CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        var user = await _dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        // Find verified organization membership of the researcher, if any
        var primaryOrgMembership = await _dbContext.OrganizationMemberships
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == currentUserId && m.IsActive && m.Organization!.VerificationStatus == OrganizationVerificationStatus.Verified, ct);

        var existingAgreement = await _dbContext.ChallengeNdaAgreements
            .FirstOrDefaultAsync(n => n.ChallengeId == challengeId && n.ResearcherUserId == currentUserId, ct);

        if (existingAgreement != null)
        {
            // Re-sign / update terms
            existingAgreement.NdaVersion = request.NdaVersion;
            existingAgreement.TermsContentHash = request.TermsContentHash;
            existingAgreement.IpAddress = clientIp;
            existingAgreement.AgreedAtUtc = DateTime.UtcNow;
            existingAgreement.IsApprovedBySponsor = !challenge.RequiresManualNdaApproval;
            existingAgreement.ApprovedAtUtc = challenge.RequiresManualNdaApproval ? null : DateTime.UtcNow;
            existingAgreement.ApprovedByUserId = null;
        }
        else
        {
            var newAgreement = ChallengeNdaAgreement.Create(
                challengeId,
                currentUserId,
                request.NdaVersion,
                request.TermsContentHash,
                clientIp,
                challenge.RequiresManualNdaApproval,
                primaryOrgMembership?.OrganizationId
            );

            _dbContext.ChallengeNdaAgreements.Add(newAgreement);
        }

        challenge.LogActivity(
            currentUserId,
            "NdaSigned",
            $"Digital NDA ({request.NdaVersion}) signed by researcher {user.Profile?.DisplayName ?? user.UserName}."
        );

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} signed NDA for challenge {ChallengeId}. Auto-approved: {Approved}.",
            currentUserId, challengeId, !challenge.RequiresManualNdaApproval);

        return await GetNdaStatusAsync(currentUserId, challengeId, ct);
    }

    public async Task ApproveNdaAsync(Guid sponsorUserId, Guid challengeId, Guid ndaAgreementId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        var isPlatformAdmin = await IsPlatformAdminAsync(sponsorUserId, ct);
        var isSponsorAdmin = await _dbContext.OrganizationMemberships
            .AnyAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == sponsorUserId && m.IsActive && m.Role == OrgMembershipRole.OrganizationAdmin, ct);

        if (!isPlatformAdmin && !isSponsorAdmin)
        {
            throw new UnauthorizedAccessException("Only organization administrators or platform administrators can approve NDA access.");
        }

        var agreement = await _dbContext.ChallengeNdaAgreements
            .FirstOrDefaultAsync(n => n.Id == ndaAgreementId && n.ChallengeId == challengeId, ct);

        if (agreement == null)
            throw new KeyNotFoundException("NDA agreement record not found.");

        agreement.Approve(sponsorUserId);

        challenge.LogActivity(
            sponsorUserId,
            "NdaApproved",
            $"Confidential access approved for researcher {agreement.ResearcherUserId}."
        );

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Sponsor {SponsorId} approved NDA {NdaId} for challenge {ChallengeId}.", sponsorUserId, ndaAgreementId, challengeId);
    }

    public async Task<bool> HasValidNdaClearanceAsync(Guid userId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null) return false;

        // Platform admins automatically have clearance
        if (await IsPlatformAdminAsync(userId, ct)) return true;

        // Sponsor organization members automatically have internal clearance
        var isSponsorMember = await _dbContext.OrganizationMemberships
            .AnyAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == userId && m.IsActive, ct);

        if (isSponsorMember) return true;

        var agreement = await _dbContext.ChallengeNdaAgreements
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.ChallengeId == challengeId && n.ResearcherUserId == userId, ct);

        if (agreement == null || !agreement.IsApprovedBySponsor)
            return false;

        // If challenge terms version updated, previous NDA is invalidated
        return string.Equals(agreement.NdaVersion, challenge.NdaTermsVersion, StringComparison.OrdinalIgnoreCase);
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
}

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

public class ProposalReviewService : IProposalReviewService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly ILogger<ProposalReviewService> _logger;

    public ProposalReviewService(IPavenaDbContext dbContext, ILogger<ProposalReviewService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ReviewAssignmentResponse> AssignReviewerAsync(Guid proposalId, AssignReviewerRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Challenge).ThenInclude(c => c!.Organization)
            .Include(p => p.Investigators)
            .Include(p => p.ReviewAssignments)
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
            throw new UnauthorizedAccessException("Only the sponsoring organization administrator or platform admin can assign reviewers.");

        // Invariant 1: Reviewer cannot be self (Lead Researcher) or any co-investigator on proposal
        if (request.ReviewerUserId == proposal.LeadResearcherId || proposal.Investigators.Any(i => i.UserId == request.ReviewerUserId))
        {
            throw new DomainException("Self-review prohibition: An investigator on the proposal cannot be assigned as its reviewer.");
        }

        // Invariant 2: Cannot assign duplicate reviewer
        if (proposal.ReviewAssignments.Any(r => r.ReviewerUserId == request.ReviewerUserId && r.Status != ReviewAssignmentStatus.Declined && r.Status != ReviewAssignmentStatus.Recused))
        {
            throw new DomainException("This reviewer is already actively assigned to this proposal.");
        }

        // Invariant 3: Bilateral block check with proposal team
        var teamUserIds = proposal.Investigators.Select(i => i.UserId).Concat(new[] { proposal.LeadResearcherId }).Distinct().ToList();
        var hasBilateralBlock = await _dbContext.UserBlocks.AnyAsync(
            b => (teamUserIds.Contains(b.BlockerUserId) && b.BlockedUserId == request.ReviewerUserId) ||
                 (b.BlockerUserId == request.ReviewerUserId && teamUserIds.Contains(b.BlockedUserId)),
            ct);

        if (hasBilateralBlock)
        {
            throw new DomainException("Reviewer cannot be assigned due to an active interpersonal block conflict of interest.");
        }

        // Invariant 4: Institutional proximity detection
        var reviewerOrgIds = await _dbContext.OrganizationMemberships
            .Where(m => m.UserId == request.ReviewerUserId && m.IsActive)
            .Select(m => m.OrganizationId)
            .ToListAsync(ct);

        var teamOrgIds = await _dbContext.OrganizationMemberships
            .Where(m => teamUserIds.Contains(m.UserId) && m.IsActive)
            .Select(m => m.OrganizationId)
            .ToListAsync(ct);

        bool hasInstitutionalProximity = reviewerOrgIds.Intersect(teamOrgIds).Any();

        var assignment = new ProposalReviewAssignment
        {
            Id = Guid.NewGuid(),
            ProposalId = proposal.Id,
            ReviewerUserId = request.ReviewerUserId,
            AssignedByUserId = actorUserId,
            Status = ReviewAssignmentStatus.Assigned,
            DueAtUtc = request.DueAtUtc ?? DateTime.UtcNow.AddDays(14),
            AssignedAtUtc = DateTime.UtcNow,
            HasCoiDeclaration = false,
            CoiType = hasInstitutionalProximity ? CoiDeclarationType.InstitutionalProximity : CoiDeclarationType.None
        };

        proposal.ReviewAssignments.Add(assignment);

        if (proposal.Status == ProposalStatus.Submitted || proposal.Status == ProposalStatus.EligibilityReview)
        {
            proposal.Status = ProposalStatus.UnderReview;
            proposal.UnderReviewAtUtc = DateTime.UtcNow;
        }

        var reviewer = await _dbContext.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == request.ReviewerUserId, ct);
        proposal.LogActivity(actorUserId, ProposalActivityType.ReviewerAssigned, $"Assigned reviewer {reviewer?.Profile?.DisplayName ?? reviewer?.UserName ?? request.ReviewerUserId.ToString()} to proposal.");

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Reviewer {ReviewerId} assigned to proposal {ProposalId} by {ActorId}", request.ReviewerUserId, proposalId, actorUserId);

        return await MapToAssignmentResponseAsync(assignment.Id, ct);
    }

    public async Task<ReviewAssignmentResponse> RespondToAssignmentAsync(Guid assignmentId, bool accept, string? declineReason, Guid reviewerUserId, CancellationToken ct = default)
    {
        var assignment = await _dbContext.ProposalReviewAssignments
            .Include(a => a.Proposal)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment == null)
            throw new KeyNotFoundException("Review assignment not found.");

        if (assignment.ReviewerUserId != reviewerUserId)
            throw new UnauthorizedAccessException("You are not authorized to respond to this review assignment.");

        if (accept)
        {
            assignment.AcceptAssignment();
            assignment.Proposal?.LogActivity(reviewerUserId, ProposalActivityType.Updated, "Reviewer accepted evaluation assignment.");
        }
        else
        {
            assignment.DeclineAssignment(declineReason ?? "Declined by reviewer.");
            assignment.Proposal?.LogActivity(reviewerUserId, ProposalActivityType.Updated, $"Reviewer declined evaluation assignment. Reason: {declineReason}");
        }

        await _dbContext.SaveChangesAsync(ct);
        return await MapToAssignmentResponseAsync(assignment.Id, ct);
    }

    public async Task<ReviewAssignmentResponse> DeclareCoiAsync(Guid assignmentId, DeclareCoiRequest request, Guid reviewerUserId, CancellationToken ct = default)
    {
        var assignment = await _dbContext.ProposalReviewAssignments
            .Include(a => a.Proposal)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment == null)
            throw new KeyNotFoundException("Review assignment not found.");

        if (assignment.ReviewerUserId != reviewerUserId)
            throw new UnauthorizedAccessException("You are not authorized to make a COI declaration for this assignment.");

        if (request.HasConflict)
        {
            assignment.RecuseDueToCoi(request.CoiType, request.Description ?? "Recused due to conflict of interest declaration.");
            assignment.Proposal?.LogActivity(reviewerUserId, ProposalActivityType.Updated, $"Reviewer recused due to declared conflict of interest: {request.CoiType}.");
        }
        else
        {
            assignment.DeclareNoCoi();
            assignment.Proposal?.LogActivity(reviewerUserId, ProposalActivityType.Updated, "Reviewer declared absence of conflict of interest.");
        }

        await _dbContext.SaveChangesAsync(ct);
        return await MapToAssignmentResponseAsync(assignment.Id, ct);
    }

    public async Task<ProposalAssessmentResponse> SubmitAssessmentAsync(Guid assignmentId, SubmitAssessmentRequest request, Guid reviewerUserId, CancellationToken ct = default)
    {
        var assignment = await _dbContext.ProposalReviewAssignments
            .Include(a => a.Proposal)
            .Include(a => a.Assessment)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment == null)
            throw new KeyNotFoundException("Review assignment not found.");

        if (assignment.ReviewerUserId != reviewerUserId)
            throw new UnauthorizedAccessException("You are not authorized to submit assessment for this assignment.");

        if (assignment.Status == ReviewAssignmentStatus.Recused)
            throw new DomainException("Cannot submit assessment: Reviewer has been recused due to conflict of interest.");

        if (assignment.Status == ReviewAssignmentStatus.Declined)
            throw new DomainException("Cannot submit assessment: Review assignment was previously declined.");

        if (!assignment.HasCoiDeclaration)
            throw new DomainException("Mandatory Conflict of Interest (COI) declaration must be completed before submitting an assessment.");

        if (assignment.Assessment == null)
        {
            assignment.Assessment = new ProposalAssessment
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignment.Id,
                ProposalId = assignment.ProposalId,
                ReviewerUserId = reviewerUserId,
                Status = AssessmentStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow
            };
            _dbContext.ProposalAssessments.Add(assignment.Assessment);
        }

        assignment.Assessment.UpdateRubricScores(
            request.TechnicalMeritScore,
            request.MethodologyScore,
            request.FeasibilityScore,
            request.InnovationScore,
            request.BudgetAdequacyScore,
            request.TeamQualificationScore,
            request.ImpactPotentialScore,
            request.AlignmentWithChallengeScore,
            request.Strengths,
            request.Weaknesses,
            request.RecommendationsForImprovement,
            request.ConfidentialRemarksForSponsor,
            request.Recommendation);

        assignment.Assessment.SubmitAssessment();
        assignment.Status = ReviewAssignmentStatus.Completed;

        assignment.Proposal?.LogActivity(reviewerUserId, ProposalActivityType.ReviewCompleted, $"Reviewer submitted structured rubric evaluation with recommendation {request.Recommendation} (Score: {assignment.Assessment.OverallScore}/100).");

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Assessment submitted for assignment {AssignmentId} by reviewer {ReviewerId}", assignmentId, reviewerUserId);

        var reviewer = await _dbContext.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == reviewerUserId, ct);

        return new ProposalAssessmentResponse(
            assignment.Assessment.Id,
            assignment.Assessment.AssignmentId,
            assignment.Assessment.ProposalId,
            assignment.Assessment.ReviewerUserId,
            reviewer?.Profile?.DisplayName ?? reviewer?.UserName ?? "Reviewer",
            assignment.Assessment.Status,
            assignment.Assessment.TechnicalMeritScore,
            assignment.Assessment.MethodologyScore,
            assignment.Assessment.FeasibilityScore,
            assignment.Assessment.InnovationScore,
            assignment.Assessment.BudgetAdequacyScore,
            assignment.Assessment.TeamQualificationScore,
            assignment.Assessment.ImpactPotentialScore,
            assignment.Assessment.AlignmentWithChallengeScore,
            assignment.Assessment.OverallScore,
            assignment.Assessment.Strengths,
            assignment.Assessment.Weaknesses,
            assignment.Assessment.RecommendationsForImprovement,
            assignment.Assessment.ConfidentialRemarksForSponsor,
            assignment.Assessment.Recommendation,
            assignment.Assessment.SubmittedAtUtc,
            assignment.Assessment.CreatedAtUtc);
    }

    public async Task<List<ReviewAssignmentResponse>> GetReviewerAssignmentsAsync(Guid reviewerUserId, CancellationToken ct = default)
    {
        var assignments = await _dbContext.ProposalReviewAssignments
            .Include(a => a.Proposal)
            .Include(a => a.ReviewerUser).ThenInclude(u => u!.Profile)
            .Include(a => a.Assessment)
            .Where(a => a.ReviewerUserId == reviewerUserId)
            .OrderByDescending(a => a.AssignedAtUtc)
            .ToListAsync(ct);

        return assignments.Select(a =>
        {
            var assessmentDto = a.Assessment == null ? null : new ProposalAssessmentResponse(
                a.Assessment.Id,
                a.Assessment.AssignmentId,
                a.Assessment.ProposalId,
                a.Assessment.ReviewerUserId,
                a.ReviewerUser?.Profile?.DisplayName ?? a.ReviewerUser?.UserName ?? "Reviewer",
                a.Assessment.Status,
                a.Assessment.TechnicalMeritScore,
                a.Assessment.MethodologyScore,
                a.Assessment.FeasibilityScore,
                a.Assessment.InnovationScore,
                a.Assessment.BudgetAdequacyScore,
                a.Assessment.TeamQualificationScore,
                a.Assessment.ImpactPotentialScore,
                a.Assessment.AlignmentWithChallengeScore,
                a.Assessment.OverallScore,
                a.Assessment.Strengths,
                a.Assessment.Weaknesses,
                a.Assessment.RecommendationsForImprovement,
                a.Assessment.ConfidentialRemarksForSponsor,
                a.Assessment.Recommendation,
                a.Assessment.SubmittedAtUtc,
                a.Assessment.CreatedAtUtc);

            return new ReviewAssignmentResponse(
                a.Id,
                a.ProposalId,
                a.ReviewerUserId,
                a.ReviewerUser?.Profile?.DisplayName ?? a.ReviewerUser?.UserName ?? "Reviewer",
                a.ReviewerUser?.Email ?? string.Empty,
                a.ReviewerUser?.Profile?.AvatarUrl,
                a.ReviewerUser?.Profile?.Headline,
                a.Status,
                a.DueAtUtc,
                a.AssignedAtUtc,
                a.AcceptedAtUtc,
                a.DeclinedAtUtc,
                a.DeclineReason,
                a.HasCoiDeclaration,
                a.CoiType,
                a.CoiDescription,
                a.CoiDeclaredAtUtc,
                assessmentDto);
        }).ToList();
    }

    private async Task<ReviewAssignmentResponse> MapToAssignmentResponseAsync(Guid assignmentId, CancellationToken ct)
    {
        var a = await _dbContext.ProposalReviewAssignments
            .Include(r => r.ReviewerUser).ThenInclude(u => u!.Profile)
            .Include(r => r.Assessment)
            .FirstAsync(r => r.Id == assignmentId, ct);

        var assessmentDto = a.Assessment == null ? null : new ProposalAssessmentResponse(
            a.Assessment.Id,
            a.Assessment.AssignmentId,
            a.Assessment.ProposalId,
            a.Assessment.ReviewerUserId,
            a.ReviewerUser?.Profile?.DisplayName ?? a.ReviewerUser?.UserName ?? "Reviewer",
            a.Assessment.Status,
            a.Assessment.TechnicalMeritScore,
            a.Assessment.MethodologyScore,
            a.Assessment.FeasibilityScore,
            a.Assessment.InnovationScore,
            a.Assessment.BudgetAdequacyScore,
            a.Assessment.TeamQualificationScore,
            a.Assessment.ImpactPotentialScore,
            a.Assessment.AlignmentWithChallengeScore,
            a.Assessment.OverallScore,
            a.Assessment.Strengths,
            a.Assessment.Weaknesses,
            a.Assessment.RecommendationsForImprovement,
            a.Assessment.ConfidentialRemarksForSponsor,
            a.Assessment.Recommendation,
            a.Assessment.SubmittedAtUtc,
            a.Assessment.CreatedAtUtc);

        return new ReviewAssignmentResponse(
            a.Id,
            a.ProposalId,
            a.ReviewerUserId,
            a.ReviewerUser?.Profile?.DisplayName ?? a.ReviewerUser?.UserName ?? "Reviewer",
            a.ReviewerUser?.Email ?? string.Empty,
            a.ReviewerUser?.Profile?.AvatarUrl,
            a.ReviewerUser?.Profile?.Headline,
            a.Status,
            a.DueAtUtc,
            a.AssignedAtUtc,
            a.AcceptedAtUtc,
            a.DeclinedAtUtc,
            a.DeclineReason,
            a.HasCoiDeclaration,
            a.CoiType,
            a.CoiDescription,
            a.CoiDeclaredAtUtc,
            assessmentDto);
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct)
    {
        var adminRoles = new[] { PlatformRole.Superadmin, PlatformRole.PlatformAdmin };
        return await _dbContext.UserPlatformRoles
            .Join(_dbContext.PlatformRoles, upr => upr.RoleId, r => r.Id, (upr, r) => new { upr.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && adminRoles.Contains(x.Name!), ct);
    }
}

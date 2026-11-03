using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Proposals.DTOs;
using Pavena.Application.Proposals.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Proposals;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Proposals;

public class ProposalService : IProposalService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IProposalEligibilityService _eligibilityService;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(
        IPavenaDbContext dbContext,
        IProposalEligibilityService eligibilityService,
        IObjectStorageService storageService,
        ILogger<ProposalService> logger)
    {
        _dbContext = dbContext;
        _eligibilityService = eligibilityService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ProposalDetailResponse> CreateDraftAsync(CreateProposalDraftRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Research challenge not found.");

        if (challenge.Status != ChallengeStatus.OpenForProposals && challenge.Status != ChallengeStatus.Published)
            throw new DomainException($"Cannot submit proposal for challenge in '{challenge.Status}' status. Challenge must be Open For Proposals.");

        if (challenge.SubmissionDeadlineUtc < DateTime.UtcNow)
            throw new DomainException("Submission deadline for this challenge has passed.");

        // Check NDA requirement
        if (challenge.RequiresManualNdaApproval || challenge.Attachments.Any(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.ConfidentialNdaRequired))
        {
            var hasSignedNda = await _dbContext.ChallengeNdaAgreements
                .AnyAsync(n => n.ChallengeId == challenge.Id && n.ResearcherUserId == actorUserId && n.IsApprovedBySponsor, ct);

            if (!hasSignedNda)
            {
                var openAgreement = await _dbContext.ChallengeNdaAgreements
                    .AnyAsync(n => n.ChallengeId == challenge.Id && n.ResearcherUserId == actorUserId, ct);

                if (!openAgreement && !challenge.RequiresManualNdaApproval)
                {
                    var nda = ChallengeNdaAgreement.Create(
                        challenge.Id,
                        actorUserId,
                        challenge.NdaTermsVersion,
                        "sha256-default",
                        null,
                        false,
                        request.SubmittingOrganizationId);
                    _dbContext.ChallengeNdaAgreements.Add(nda);
                }
            }
        }

        var baseSlug = GenerateSlug(request.Title);
        var slug = await EnsureUniqueSlugAsync(baseSlug, ct);

        var proposal = ResearchProposal.CreateDraft(
            request.ChallengeId,
            actorUserId,
            request.SubmittingOrganizationId,
            request.Title,
            slug,
            request.Abstract,
            request.ProblemStatement,
            request.ResearchObjectives,
            request.ProposedMethodology,
            request.ExpectedOutcomes,
            request.ProposedTimelineMonths,
            request.ProposedBudget,
            request.Currency ?? "USD",
            request.FundingJustification,
            request.Confidentiality);

        if (request.Domains != null)
        {
            foreach (var domain in request.Domains.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                proposal.Domains.Add(new ProposalResearchDomain
                {
                    Id = Guid.NewGuid(),
                    ProposalId = proposal.Id,
                    Domain = domain.Trim()
                });
            }
        }

        if (request.ExpertiseTags != null)
        {
            foreach (var tag in request.ExpertiseTags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                proposal.ExpertiseTags.Add(new ProposalExpertise
                {
                    Id = Guid.NewGuid(),
                    ProposalId = proposal.Id,
                    ExpertiseTag = tag.Trim()
                });
            }
        }

        _dbContext.ResearchProposals.Add(proposal);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} created draft proposal {ProposalId} for challenge {ChallengeId}", actorUserId, proposal.Id, request.ChallengeId);

        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalDetailResponse> UpdateDraftAsync(Guid proposalId, UpdateProposalDraftRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Domains)
            .Include(p => p.ExpertiseTags)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);

        var baseSlug = GenerateSlug(request.Title);
        var slug = (baseSlug == proposal.Slug) ? proposal.Slug : await EnsureUniqueSlugAsync(baseSlug, ct);

        proposal.UpdateDraft(
            actorUserId,
            request.Title,
            slug,
            request.Abstract,
            request.ProblemStatement,
            request.ResearchObjectives,
            request.ProposedMethodology,
            request.ExpectedOutcomes,
            request.ProposedTimelineMonths,
            request.ProposedBudget,
            request.Currency ?? "USD",
            request.FundingJustification,
            request.Confidentiality,
            request.SubmittingOrganizationId);

        // Update domains
        if (request.Domains != null)
        {
            proposal.Domains.Clear();
            foreach (var domain in request.Domains.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                proposal.Domains.Add(new ProposalResearchDomain
                {
                    Id = Guid.NewGuid(),
                    ProposalId = proposal.Id,
                    Domain = domain.Trim()
                });
            }
        }

        // Update expertise tags
        if (request.ExpertiseTags != null)
        {
            proposal.ExpertiseTags.Clear();
            foreach (var tag in request.ExpertiseTags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                proposal.ExpertiseTags.Add(new ProposalExpertise
                {
                    Id = Guid.NewGuid(),
                    ProposalId = proposal.Id,
                    ExpertiseTag = tag.Trim()
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalDetailResponse> SubmitAsync(Guid proposalId, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Challenge)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);

        // Pre-submission eligibility check
        var eligibility = await _eligibilityService.EvaluateEligibilityAsync(proposalId, actorUserId, ct);
        if (!eligibility.IsEligible)
        {
            var failureDetails = string.Join("; ", eligibility.FailureReasons);
            throw new DomainException($"Cannot submit proposal: eligibility criteria not satisfied. Details: {failureDetails}");
        }

        proposal.Submit(actorUserId);

        // If challenge has open review, proposal can move to UnderReview immediately or stay in Submitted
        if (proposal.Challenge != null && proposal.Challenge.Status == ChallengeStatus.OpenForProposals)
        {
            proposal.PassEligibility(actorUserId, "Eligibility verified automatically upon submission.");
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Proposal {ProposalId} successfully submitted by user {UserId}", proposalId, actorUserId);

        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalDetailResponse?> GetByIdAsync(Guid proposalId, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Challenge).ThenInclude(c => c!.Organization)
            .Include(p => p.LeadResearcher).ThenInclude(u => u!.Profile)
            .Include(p => p.SubmittingOrganization)
            .Include(p => p.Investigators).ThenInclude(i => i.User).ThenInclude(u => u!.Profile)
            .Include(p => p.Domains)
            .Include(p => p.ExpertiseTags)
            .Include(p => p.Attachments).ThenInclude(a => a.UploadedByUser).ThenInclude(u => u!.Profile)
            .Include(p => p.VersionSnapshots).ThenInclude(v => v.CreatedByUser).ThenInclude(u => u!.Profile)
            .Include(p => p.ReviewAssignments).ThenInclude(r => r.ReviewerUser).ThenInclude(u => u!.Profile)
            .Include(p => p.ReviewAssignments).ThenInclude(r => r.Assessment)
            .Include(p => p.ActivityLogs).ThenInclude(l => l.ActorUser).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            return null;

        // Platform admins have NO implicit read access to confidential proposals.
        var isPlatformAdmin = false;
        var isLeadResearcher = proposal.LeadResearcherId == actorUserId;
        var isTeamMember = proposal.Investigators.Any(i => i.UserId == actorUserId);
        
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

        var isReviewer = proposal.ReviewAssignments.Any(r => r.ReviewerUserId == actorUserId);

        // 404 Concealment if unauthorized
        if (!isPlatformAdmin && !isLeadResearcher && !isTeamMember && !isSponsorAdmin && !isReviewer)
        {
            return null;
        }

        // Blind peer assessment isolation
        var visibleAssignments = proposal.ReviewAssignments.ToList();
        if (isReviewer && !isSponsorAdmin && !isPlatformAdmin)
        {
            visibleAssignments = visibleAssignments.Where(r => r.ReviewerUserId == actorUserId).ToList();
        }
        else if ((isLeadResearcher || isTeamMember) && !isSponsorAdmin && !isPlatformAdmin)
        {
            if (proposal.Status != ProposalStatus.Accepted && proposal.Status != ProposalStatus.Rejected && proposal.Status != ProposalStatus.RevisionRequested)
            {
                visibleAssignments.Clear();
            }
        }

        return MapToDetailResponse(proposal, actorUserId, isSponsorAdmin || isPlatformAdmin, isReviewer, visibleAssignments);
    }

    public async Task<List<ProposalSummaryResponse>> GetSummariesAsync(
        Guid? challengeId,
        Guid? leadResearcherId,
        ProposalStatus? status,
        int page,
        int pageSize,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        // Platform admins are scoped like any other user (no implicit access to confidential proposals).
        var isPlatformAdmin = false;

        var query = _dbContext.ResearchProposals
            .Include(p => p.Challenge).ThenInclude(c => c!.Organization)
            .Include(p => p.LeadResearcher).ThenInclude(u => u!.Profile)
            .Include(p => p.SubmittingOrganization)
            .Include(p => p.ReviewAssignments).ThenInclude(r => r.Assessment)
            .AsNoTracking()
            .AsQueryable();

        if (challengeId.HasValue)
            query = query.Where(p => p.ChallengeId == challengeId.Value);

        if (leadResearcherId.HasValue)
            query = query.Where(p => p.LeadResearcherId == leadResearcherId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        // Authorization Scoping
        if (!isPlatformAdmin)
        {
            var userOrgIds = await _dbContext.OrganizationMemberships
                .Where(m => m.UserId == actorUserId && m.IsActive && m.Role == OrgMembershipRole.OrganizationAdmin)
                .Select(m => m.OrganizationId)
                .ToListAsync(ct);

            query = query.Where(p =>
                p.LeadResearcherId == actorUserId ||
                p.Investigators.Any(i => i.UserId == actorUserId) ||
                p.ReviewAssignments.Any(r => r.ReviewerUserId == actorUserId) ||
                (p.Challenge != null && userOrgIds.Contains(p.Challenge.OrganizationId)));
        }

        var pagedProposals = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return pagedProposals.Select(p =>
        {
            var assessments = p.ReviewAssignments
                .Where(r => r.Assessment != null && r.Assessment.Status == AssessmentStatus.Submitted)
                .Select(r => r.Assessment!)
                .ToList();

            decimal? avgScore = assessments.Any() ? Math.Round(assessments.Average(a => a.OverallScore), 2) : null;

            return new ProposalSummaryResponse(
                p.Id,
                p.ChallengeId,
                p.Challenge?.Title ?? "Unknown Challenge",
                p.LeadResearcherId,
                p.LeadResearcher?.Profile?.DisplayName ?? p.LeadResearcher?.UserName ?? "Unknown",
                p.LeadResearcher?.Profile?.AvatarUrl,
                p.SubmittingOrganizationId,
                p.SubmittingOrganization?.Name,
                p.Title,
                p.Slug,
                p.ProposedBudget,
                p.Currency,
                p.ProposedTimelineMonths,
                p.Status,
                p.SubmissionVersion,
                p.ConvertedProjectId,
                p.CreatedAtUtc,
                p.SubmittedAtUtc,
                p.DecisionAtUtc,
                avgScore,
                assessments.Count);
        }).ToList();
    }

    public async Task<ProposalDetailResponse> AddInvestigatorAsync(Guid proposalId, AddInvestigatorRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Investigators)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);

        var targetUser = await _dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (targetUser == null)
            throw new KeyNotFoundException("User to add as investigator not found.");

        var isBlocked = await _dbContext.UserBlocks.AnyAsync(
            b => (b.BlockerUserId == actorUserId && b.BlockedUserId == request.UserId) ||
                 (b.BlockerUserId == request.UserId && b.BlockedUserId == actorUserId),
            ct);

        if (isBlocked)
            throw new DomainException("Cannot add investigator due to an active block relationship.");

        proposal.AddInvestigator(request.UserId, request.Role, request.ContributionDescription);
        proposal.LogActivity(actorUserId, ProposalActivityType.Updated, $"Added investigator {targetUser.Profile?.DisplayName ?? targetUser.UserName} as {request.Role}.");

        await _dbContext.SaveChangesAsync(ct);
        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalDetailResponse> RemoveInvestigatorAsync(Guid proposalId, Guid investigatorUserId, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Investigators)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);
        proposal.RemoveInvestigator(investigatorUserId);
        proposal.LogActivity(actorUserId, ProposalActivityType.Updated, $"Removed investigator {investigatorUserId} from proposal.");

        await _dbContext.SaveChangesAsync(ct);
        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalAttachmentResponse> UploadAttachmentAsync(
        Guid proposalId,
        ProposalAttachmentType type,
        string fileName,
        string contentType,
        Stream stream,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Investigators)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        var isLead = proposal.LeadResearcherId == actorUserId;
        var isTeam = proposal.Investigators.Any(i => i.UserId == actorUserId);
        if (!isLead && !isTeam)
            throw new UnauthorizedAccessException("Only proposal investigators can upload attachments.");

        if (proposal.Status != ProposalStatus.Draft && proposal.Status != ProposalStatus.RevisionRequested)
            throw new DomainException($"Cannot upload attachments to proposal in '{proposal.Status}' status.");

        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        var bytes = memoryStream.ToArray();
        memoryStream.Position = 0;

        if (bytes.Length > 25 * 1024 * 1024)
            throw new DomainException("Attachment file size exceeds maximum permitted limit of 25 MB.");

        var hashString = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var storageKey = $"proposals/{proposalId}/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";

        await _storageService.SaveObjectAsync(storageKey, memoryStream, contentType, ct);

        proposal.AddAttachment(type, fileName, storageKey, contentType, bytes.Length, hashString, actorUserId);
        proposal.LogActivity(actorUserId, ProposalActivityType.Updated, $"Uploaded attachment '{fileName}' ({type}).");

        await _dbContext.SaveChangesAsync(ct);

        var attachment = proposal.Attachments.First(a => a.StoragePath == storageKey);
        var user = await _dbContext.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == actorUserId, ct);

        return new ProposalAttachmentResponse(
            attachment.Id,
            attachment.ProposalId,
            attachment.AttachmentType,
            attachment.FileName,
            attachment.StoragePath,
            attachment.ContentType,
            attachment.FileSizeBytes,
            attachment.FileHashSha256,
            attachment.UploadedByUserId,
            user?.Profile?.DisplayName ?? user?.UserName ?? "Unknown",
            attachment.UploadedAtUtc);
    }

    public async Task<ProposalDetailResponse> RemoveAttachmentAsync(Guid proposalId, Guid attachmentId, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);

        var att = proposal.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (att != null)
        {
            try
            {
                await _storageService.DeleteObjectAsync(att.StoragePath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete storage file {StoragePath}", att.StoragePath);
            }

            proposal.RemoveAttachment(attachmentId);
            proposal.LogActivity(actorUserId, ProposalActivityType.Updated, $"Removed attachment '{att.FileName}'.");
            await _dbContext.SaveChangesAsync(ct);
        }

        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalDetailResponse> ResubmitAsync(Guid proposalId, ResubmitProposalRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.VersionSnapshots)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);

        if (proposal.Status != ProposalStatus.RevisionRequested)
            throw new DomainException($"Cannot resubmit proposal in '{proposal.Status}' status. Must be in RevisionRequested status.");

        // Snapshot existing state before mutating
        var snapshotState = new
        {
            proposal.Id,
            proposal.SubmissionVersion,
            proposal.Title,
            proposal.Abstract,
            proposal.ProblemStatement,
            proposal.ResearchObjectives,
            proposal.ProposedMethodology,
            proposal.ExpectedOutcomes,
            proposal.ProposedTimelineMonths,
            proposal.ProposedBudget,
            proposal.Currency,
            proposal.FundingJustification
        };
        var snapshotJson = JsonSerializer.Serialize(snapshotState);
        proposal.AddSnapshot(proposal.SubmissionVersion, snapshotJson, request.ChangeSummary, actorUserId);

        // Mutate with updated data
        proposal.Title = request.Title.Trim();
        proposal.Abstract = request.Abstract.Trim();
        proposal.ProblemStatement = request.ProblemStatement.Trim();
        proposal.ResearchObjectives = request.ResearchObjectives.Trim();
        proposal.ProposedMethodology = request.ProposedMethodology.Trim();
        proposal.ExpectedOutcomes = request.ExpectedOutcomes.Trim();
        proposal.ProposedTimelineMonths = request.ProposedTimelineMonths;
        proposal.ProposedBudget = request.ProposedBudget;
        proposal.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant();
        proposal.FundingJustification = request.FundingJustification?.Trim();

        proposal.Resubmit(actorUserId, request.ChangeSummary);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Proposal {ProposalId} resubmitted by user {UserId} as version {Version}", proposalId, actorUserId, proposal.SubmissionVersion);
        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalDetailResponse> WithdrawAsync(Guid proposalId, string? reason, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals.FirstOrDefaultAsync(p => p.Id == proposalId, ct);
        if (proposal == null)
            throw new KeyNotFoundException("Proposal not found.");

        proposal.EnsureLeadResearcher(actorUserId);
        proposal.Withdraw(actorUserId, reason);

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Proposal {ProposalId} withdrawn by user {UserId}", proposalId, actorUserId);

        return (await GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    private ProposalDetailResponse MapToDetailResponse(
        ResearchProposal p,
        Guid currentUserId,
        bool isSponsorOrPlatformAdmin,
        bool isReviewer,
        List<ProposalReviewAssignment> visibleAssignments)
    {
        var canEdit = (p.LeadResearcherId == currentUserId) && (p.Status == ProposalStatus.Draft || p.Status == ProposalStatus.RevisionRequested);
        var canSubmit = (p.LeadResearcherId == currentUserId) && (p.Status == ProposalStatus.Draft);
        var canResubmit = (p.LeadResearcherId == currentUserId) && (p.Status == ProposalStatus.RevisionRequested);
        var canWithdraw = (p.LeadResearcherId == currentUserId) && (p.Status != ProposalStatus.Accepted && p.Status != ProposalStatus.ConvertedToProject && p.Status != ProposalStatus.Withdrawn);
        
        var reviewerAssignment = p.ReviewAssignments.FirstOrDefault(r => r.ReviewerUserId == currentUserId);
        var canReview = reviewerAssignment != null &&
                        reviewerAssignment.Status != ReviewAssignmentStatus.Completed &&
                        reviewerAssignment.Status != ReviewAssignmentStatus.Recused &&
                        reviewerAssignment.Status != ReviewAssignmentStatus.Declined &&
                        (p.Status == ProposalStatus.UnderReview || p.Status == ProposalStatus.Resubmitted);

        var canDecide = isSponsorOrPlatformAdmin && (p.Status == ProposalStatus.UnderReview || p.Status == ProposalStatus.Resubmitted || p.Status == ProposalStatus.Submitted);
        var canConvert = isSponsorOrPlatformAdmin && (p.Status == ProposalStatus.Accepted) && p.ConvertedProjectId == null;

        var investigators = p.Investigators.Select(i => new InvestigatorResponse(
            i.Id,
            i.UserId,
            i.User?.Profile?.DisplayName ?? i.User?.UserName ?? "Unknown",
            i.User?.Email ?? string.Empty,
            i.Role,
            i.ContributionDescription,
            i.User?.Profile?.Headline,
            i.User?.Profile?.AvatarUrl
        )).ToList();

        var attachments = p.Attachments.Select(a => new ProposalAttachmentResponse(
            a.Id,
            a.ProposalId,
            a.AttachmentType,
            a.FileName,
            a.StoragePath,
            a.ContentType,
            a.FileSizeBytes,
            a.FileHashSha256,
            a.UploadedByUserId,
            a.UploadedByUser?.Profile?.DisplayName ?? a.UploadedByUser?.UserName ?? "Unknown",
            a.UploadedAtUtc
        )).ToList();

        var snapshots = p.VersionSnapshots.OrderByDescending(v => v.VersionNumber).Select(v => new ProposalVersionSnapshotResponse(
            v.Id,
            v.ProposalId,
            v.VersionNumber,
            v.SnapshotDataJson,
            v.ChangeSummary,
            v.CreatedByUserId,
            v.CreatedByUser?.Profile?.DisplayName ?? v.CreatedByUser?.UserName ?? "Unknown",
            v.CreatedAtUtc
        )).ToList();

        var reviewDtos = visibleAssignments.Select(r =>
        {
            var reviewerName = isSponsorOrPlatformAdmin || r.ReviewerUserId == currentUserId
                ? (r.ReviewerUser?.Profile?.DisplayName ?? r.ReviewerUser?.UserName ?? "Anonymous Reviewer")
                : "Anonymous Reviewer";

            var reviewerEmail = isSponsorOrPlatformAdmin || r.ReviewerUserId == currentUserId
                ? (r.ReviewerUser?.Email ?? string.Empty)
                : string.Empty;

            var assessmentDto = r.Assessment == null ? null : new ProposalAssessmentResponse(
                r.Assessment.Id,
                r.Assessment.AssignmentId,
                r.Assessment.ProposalId,
                r.Assessment.ReviewerUserId,
                reviewerName,
                r.Assessment.Status,
                r.Assessment.TechnicalMeritScore,
                r.Assessment.MethodologyScore,
                r.Assessment.FeasibilityScore,
                r.Assessment.InnovationScore,
                r.Assessment.BudgetAdequacyScore,
                r.Assessment.TeamQualificationScore,
                r.Assessment.ImpactPotentialScore,
                r.Assessment.AlignmentWithChallengeScore,
                r.Assessment.OverallScore,
                r.Assessment.Strengths,
                r.Assessment.Weaknesses,
                r.Assessment.RecommendationsForImprovement,
                isSponsorOrPlatformAdmin ? r.Assessment.ConfidentialRemarksForSponsor : null,
                r.Assessment.Recommendation,
                r.Assessment.SubmittedAtUtc,
                r.Assessment.CreatedAtUtc);

            return new ReviewAssignmentResponse(
                r.Id,
                r.ProposalId,
                r.ReviewerUserId,
                reviewerName,
                reviewerEmail,
                isSponsorOrPlatformAdmin || r.ReviewerUserId == currentUserId ? r.ReviewerUser?.Profile?.AvatarUrl : null,
                isSponsorOrPlatformAdmin || r.ReviewerUserId == currentUserId ? r.ReviewerUser?.Profile?.Headline : null,
                r.Status,
                r.DueAtUtc,
                r.AssignedAtUtc,
                r.AcceptedAtUtc,
                r.DeclinedAtUtc,
                r.DeclineReason,
                r.HasCoiDeclaration,
                r.CoiType,
                r.CoiDescription,
                r.CoiDeclaredAtUtc,
                assessmentDto);
        }).ToList();

        var activityDtos = p.ActivityLogs.OrderByDescending(l => l.CreatedAtUtc).Select(l => new ProposalActivityLogResponse(
            l.Id,
            l.ProposalId,
            l.ActorUserId,
            l.ActorUser?.Profile?.DisplayName ?? l.ActorUser?.UserName ?? "System",
            l.ActivityType,
            l.Description,
            l.MetadataJson,
            l.CreatedAtUtc
        )).ToList();

        return new ProposalDetailResponse(
            p.Id,
            p.ChallengeId,
            p.Challenge?.Title ?? "Unknown Challenge",
            p.Challenge?.OrganizationId ?? Guid.Empty,
            p.Challenge?.Organization?.Name ?? "Unknown Organization",
            p.LeadResearcherId,
            p.LeadResearcher?.Profile?.DisplayName ?? p.LeadResearcher?.UserName ?? "Unknown",
            p.LeadResearcher?.Profile?.AvatarUrl,
            p.SubmittingOrganizationId,
            p.SubmittingOrganization?.Name,
            p.Title,
            p.Slug,
            p.Abstract,
            p.ProblemStatement,
            p.ResearchObjectives,
            p.ProposedMethodology,
            p.ExpectedOutcomes,
            p.ProposedTimelineMonths,
            p.ProposedBudget,
            p.Currency,
            p.FundingJustification,
            p.Confidentiality,
            p.Status,
            p.SubmissionVersion,
            p.ConvertedProjectId,
            p.DecisionNotes,
            p.DecisionByUserId,
            p.DecisionAtUtc,
            p.EligibilityRejectionReason,
            p.RevisionGuidance,
            p.SubmittedAtUtc,
            p.UnderReviewAtUtc,
            p.RevisionRequestedAtUtc,
            p.ResubmittedAtUtc,
            p.CreatedAtUtc,
            p.UpdatedAtUtc,
            investigators,
            p.Domains.Select(d => d.Domain).ToList(),
            p.ExpertiseTags.Select(e => e.ExpertiseTag).ToList(),
            attachments,
            snapshots,
            reviewDtos,
            activityDtos,
            canEdit,
            canSubmit,
            canResubmit,
            canWithdraw,
            canReview,
            canDecide,
            canConvert);
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct)
    {
        var adminRoles = new[] { PlatformRole.Superadmin, PlatformRole.PlatformAdmin };
        return await _dbContext.UserPlatformRoles
            .Join(_dbContext.PlatformRoles, upr => upr.RoleId, r => r.Id, (upr, r) => new { upr.UserId, r.Name })
            .AnyAsync(x => x.UserId == userId && adminRoles.Contains(x.Name!), ct);
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "proposal" : (slug.Length > 200 ? slug[..200] : slug);
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var counter = 1;
        while (await _dbContext.ResearchProposals.AnyAsync(p => p.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{counter++}";
        }
        return slug;
    }
}

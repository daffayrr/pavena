using System.Text.RegularExpressions;
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
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Proposals;

public class ProposalConversionService : IProposalConversionService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IProposalService _proposalService;
    private readonly ILogger<ProposalConversionService> _logger;

    public ProposalConversionService(
        IPavenaDbContext dbContext,
        IProposalService proposalService,
        ILogger<ProposalConversionService> logger)
    {
        _dbContext = dbContext;
        _proposalService = proposalService;
        _logger = logger;
    }

    public async Task<ProposalDetailResponse> ApplySponsorDecisionAsync(Guid proposalId, SponsorDecisionRequest request, Guid actorUserId, CancellationToken ct = default)
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
            throw new UnauthorizedAccessException("Only the sponsoring organization administrator or platform admin can issue proposal decisions.");

        if (request.Decision.Equals("Accept", StringComparison.OrdinalIgnoreCase))
        {
            proposal.Accept(actorUserId, request.NotesOrGuidance);
            _logger.LogInformation("Proposal {ProposalId} ACCEPTED by sponsor {ActorId}", proposalId, actorUserId);
        }
        else if (request.Decision.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            proposal.Reject(actorUserId, request.NotesOrGuidance);
            _logger.LogInformation("Proposal {ProposalId} REJECTED by sponsor {ActorId}", proposalId, actorUserId);
        }
        else if (request.Decision.Equals("RequestRevision", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.NotesOrGuidance))
                throw new DomainException("Revision guidance is required when requesting proposal revisions.");

            proposal.RequestRevision(actorUserId, request.NotesOrGuidance);
            _logger.LogInformation("Proposal {ProposalId} REVISION REQUESTED by sponsor {ActorId}", proposalId, actorUserId);
        }
        else
        {
            throw new DomainException($"Invalid decision '{request.Decision}'. Must be 'Accept', 'Reject', or 'RequestRevision'.");
        }

        await _dbContext.SaveChangesAsync(ct);
        return (await _proposalService.GetByIdAsync(proposal.Id, actorUserId, ct))!;
    }

    public async Task<ProposalConversionResponse> ConvertToProjectAsync(Guid proposalId, ConvertProposalRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var proposal = await _dbContext.ResearchProposals
            .Include(p => p.Challenge).ThenInclude(c => c!.Organization)
            .Include(p => p.Investigators)
            .Include(p => p.Domains)
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
            throw new UnauthorizedAccessException("Only the sponsoring organization administrator or platform admin can convert accepted proposals to research projects.");

        // Idempotency: If already converted, return existing project details without duplicate creation
        if (proposal.ConvertedProjectId.HasValue)
        {
            var existingProject = await _dbContext.ResearchProjects.FirstOrDefaultAsync(p => p.Id == proposal.ConvertedProjectId.Value, ct);
            if (existingProject != null)
            {
                return new ProposalConversionResponse(
                    proposal.Id,
                    existingProject.Id,
                    existingProject.Slug,
                    existingProject.Title,
                    existingProject.CreatedAtUtc,
                    true);
            }
        }

        if (proposal.Status != ProposalStatus.Accepted)
            throw new DomainException($"Only Accepted proposals can be converted to projects. Current status: '{proposal.Status}'.");

        var projectTitle = !string.IsNullOrWhiteSpace(request.CustomProjectTitle)
            ? request.CustomProjectTitle.Trim()
            : proposal.Title;

        var baseSlug = GenerateSlug(projectTitle);
        var projectSlug = await EnsureUniqueProjectSlugAsync(baseSlug, ct);
        var researchField = proposal.Domains.FirstOrDefault()?.Domain ?? "Interdisciplinary";

        // Create ResearchProject with full traceability
        var project = ResearchProject.CreateFromProposal(
            projectTitle,
            projectSlug,
            proposal.LeadResearcherId,
            proposal.ChallengeId,
            proposal.Id,
            proposal.Abstract,
            researchField,
            proposal.ResearchObjectives);

        // Add additional investigators as Project Members
        foreach (var inv in proposal.Investigators.Where(i => i.UserId != proposal.LeadResearcherId))
        {
            var role = inv.Role switch
            {
                InvestigatorRole.CoInvestigator => ProjectMemberRole.CoResearcher,
                InvestigatorRole.PostdoctoralResearcher => ProjectMemberRole.Researcher,
                InvestigatorRole.GraduateResearcher => ProjectMemberRole.ResearchAssistant,
                InvestigatorRole.Consultant => ProjectMemberRole.Observer,
                _ => ProjectMemberRole.Researcher
            };

            project.Members.Add(new ResearchMember(project.Id, inv.UserId, role, actorUserId));
        }

        // Add Sponsor representative as IndustryPartner member if applicable
        if (proposal.Challenge?.CreatedByUserId != null && proposal.Challenge.CreatedByUserId != proposal.LeadResearcherId)
        {
            if (!project.Members.Any(m => m.UserId == proposal.Challenge.CreatedByUserId))
            {
                project.Members.Add(new ResearchMember(project.Id, proposal.Challenge.CreatedByUserId, ProjectMemberRole.IndustryPartner, actorUserId));
            }
        }

        _dbContext.ResearchProjects.Add(project);

        // Mark proposal converted
        proposal.MarkConverted(project.Id, actorUserId);

        // Update challenge status to Awarded if still in Open/Review status
        if (proposal.Challenge != null &&
            (proposal.Challenge.Status == ChallengeStatus.OpenForProposals || proposal.Challenge.Status == ChallengeStatus.UnderReview))
        {
            proposal.Challenge.Status = ChallengeStatus.Awarded;
            proposal.Challenge.LogActivity(actorUserId, "ChallengeAwarded", $"Challenge awarded to proposal '{proposal.Title}' (Project: '{project.Title}').");
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Proposal {ProposalId} converted into ResearchProject {ProjectId} by user {ActorId}", proposalId, project.Id, actorUserId);

        return new ProposalConversionResponse(
            proposal.Id,
            project.Id,
            project.Slug,
            project.Title,
            project.CreatedAtUtc,
            false);
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
        return string.IsNullOrWhiteSpace(slug) ? "project" : (slug.Length > 200 ? slug[..200] : slug);
    }

    private async Task<string> EnsureUniqueProjectSlugAsync(string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var counter = 1;
        while (await _dbContext.ResearchProjects.AnyAsync(p => p.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{counter++}";
        }
        return slug;
    }
}

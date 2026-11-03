using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Proposals.DTOs;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class ProposalApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly PavenaApiFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProposalApiIntegrationTests(PavenaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Email)> CreateAndAuthenticateUserAsync(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{suffix}@academic.test";
        var username = $"{prefix}_{suffix}";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, username, password, $"Dr. {prefix}", "GB");
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginReq = new LoginRequest(email, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var authBody = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);

        // Ensure user has a complete profile for eligibility
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == authBody!.UserId);
        if (profile == null)
        {
            profile = new UserProfile(authBody!.UserId, $"Dr. {prefix}");
            db.UserProfiles.Add(profile);
        }
        profile.Headline = "Senior Research Scientist";
        profile.Bio = "Extensive academic expertise in applied physical and chemical sciences.";
        await db.SaveChangesAsync();

        return (authBody!.AccessToken, authBody.UserId, email);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null)
        {
            msg.Content = JsonContent.Create(body);
        }
        return msg;
    }

    private async Task<Guid> SeedOpenChallengeAsync(string sponsorToken, Guid sponsorUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var org = new Organization($"EnergyCorp {suffix}", $"energycorp-{suffix}", OrganizationType.Company)
        {
            VerificationStatus = OrganizationVerificationStatus.Verified
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var membership = new OrganizationMembership(org.Id, sponsorUserId, OrgMembershipRole.OrganizationAdmin)
        {
            IsActive = true
        };
        db.OrganizationMemberships.Add(membership);
        await db.SaveChangesAsync();

        var createReq = new CreateChallengeRequest(
            OrganizationId: org.Id,
            Title: $"High-Efficiency Perovskite Tandem Solar Cell {suffix}",
            Summary: "Seeking innovative encapsulation methods for moisture-tolerant perovskite tandem photovoltaics.",
            ProblemStatement: "Perovskite degradation under 85C/85% RH damp-heat conditions remains the barrier to commercial 25-year warranties.",
            ResearchScope: "ALD inorganic barrier layers and cross-linked polymeric edge sealants.",
            ExpectedOutcomes: "Tested 1000h damp-heat photovoltaic modules with <5% efficiency loss.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(45),
            Visibility: ChallengeVisibility.Public,
            Eligibility: InstitutionalEligibility.OpenToAll,
            Currency: "USD",
            MinBudget: 50000,
            MaxBudget: 150000,
            EstimatedDurationMonths: 18,
            RequiresManualNdaApproval: false,
            Domains: new() { new CreateDomainRequest("Renewable Energy & Photovoltaics", "Perovskite Encapsulation", true) },
            Expertise: new() { new CreateExpertiseRequest("Thin Film Deposition", RequirementImportance.Required) }
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/challenges", sponsorToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var challengeDetail = await createRes.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions);
        var challengeId = challengeDetail!.Id;

        // Submit, Approve, Publish, Open for Proposals
        using var submitMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/submit", sponsorToken, "Ready");
        var submitRes = await _client.SendAsync(submitMsg);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var decisionReq = new ChallengeDecisionRequest(ChallengeApprovalDecision.Approved, "Approved");
        using var decMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/decision", sponsorToken, decisionReq);
        var decRes = await _client.SendAsync(decMsg);
        decRes.StatusCode.Should().Be(HttpStatusCode.OK);

        using var pubMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/publish", sponsorToken);
        var pubRes = await _client.SendAsync(pubMsg);
        pubRes.StatusCode.Should().Be(HttpStatusCode.OK);

        using var openMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/open-proposals", sponsorToken);
        var openRes = await _client.SendAsync(openMsg);
        openRes.StatusCode.Should().Be(HttpStatusCode.OK);

        return challengeId;
    }

    [Fact]
    public async Task ProposalLifecycle_Submit_Review_Accept_ConvertToProject_SucceedsEndToEnd()
    {
        // 1. Setup Sponsor and Challenge
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorPerov");
        var challengeId = await SeedOpenChallengeAsync(sponsorToken, sponsorUserId);

        // 2. Setup Lead Researcher and Co-Investigator
        var (leadToken, leadUserId, _) = await CreateAndAuthenticateUserAsync("leadScholar");
        var (_, coInvestigatorUserId, _) = await CreateAndAuthenticateUserAsync("coScholar");

        // 3. Create Proposal Draft
        var createProposalReq = new CreateProposalDraftRequest(
            ChallengeId: challengeId,
            Title: "Atomic Layer Deposited AlOx/SnOx Hybrid Moisture Barrier for Perovskite Cells",
            Abstract: "We propose a dual-layer ALD nanolaminate combined with UV-curable olefin sealants.",
            ProblemStatement: "Direct water vapor transmission rate through organic polymers exceeds 10^-2 g/m2/day.",
            ResearchObjectives: "Demonstrate WVTR < 10^-4 g/m2/day and preserve 95% PCE after 1000h IEC 61215 standard testing.",
            ProposedMethodology: "Spatial ALD thin film deposition, calcium mirror optical degradation testing, and accelerated weatherometer exposure.",
            ExpectedOutcomes: "Validated encapsulation stack design and complete degradation kinetics model.",
            ProposedTimelineMonths: 18,
            ProposedBudget: 120000m,
            Currency: "USD",
            FundingJustification: "ALD precursor materials ($30k), post-doc researcher stipends ($60k), characterization equipment time ($30k)."
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/proposals", leadToken, createProposalReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var proposalDetail = await createRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);
        proposalDetail.Should().NotBeNull();
        proposalDetail!.Status.Should().Be(ProposalStatus.Draft);
        var proposalId = proposalDetail.Id;

        // 4. Add Co-Investigator to Proposal
        var addInvReq = new AddInvestigatorRequest(coInvestigatorUserId, InvestigatorRole.CoInvestigator, "Thin film deposition specialist");
        using var addInvMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/investigators", leadToken, addInvReq);
        var addInvRes = await _client.SendAsync(addInvMsg);
        addInvRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Submit Proposal
        using var submitProposalMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/submit", leadToken);
        var submitProposalRes = await _client.SendAsync(submitProposalMsg);
        submitProposalRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var submittedProposal = await submitProposalRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);
        submittedProposal!.Status.Should().Be(ProposalStatus.UnderReview);

        // 6. Sponsor assigns independent Reviewer
        var (reviewerToken, reviewerUserId, _) = await CreateAndAuthenticateUserAsync("peerReviewer");
        var assignReq = new AssignReviewerRequest(reviewerUserId, DateTime.UtcNow.AddDays(14));
        using var assignMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/reviewers", sponsorToken, assignReq);
        var assignRes = await _client.SendAsync(assignMsg);
        assignRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignment = await assignRes.Content.ReadFromJsonAsync<ReviewAssignmentResponse>(_jsonOptions);
        assignment.Should().NotBeNull();
        assignment!.Status.Should().Be(ReviewAssignmentStatus.Assigned);
        var assignmentId = assignment.Id;

        // 7. Reviewer declares absence of COI
        var coiReq = new DeclareCoiRequest(false, CoiDeclarationType.None, null);
        using var coiMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/assignments/{assignmentId}/coi", reviewerToken, coiReq);
        var coiRes = await _client.SendAsync(coiMsg);
        coiRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Reviewer submits structured rubric assessment
        var assessmentReq = new SubmitAssessmentRequest(
            TechnicalMeritScore: 9.0m,
            MethodologyScore: 8.5m,
            FeasibilityScore: 8.0m,
            InnovationScore: 9.0m,
            BudgetAdequacyScore: 8.5m,
            TeamQualificationScore: 9.0m,
            ImpactPotentialScore: 8.5m,
            AlignmentWithChallengeScore: 9.5m,
            Strengths: "Exceptional nanoscale ALD barrier methodology and clear milestone path.",
            Weaknesses: "Edge sealant curing parameters require careful calibration during scaling.",
            RecommendationsForImprovement: "Include thermal shock cycling (-40C to 85C) alongside damp-heat tests.",
            ConfidentialRemarksForSponsor: "Strongly recommended for funding award.",
            Recommendation: AssessmentRecommendation.RecommendAccept
        );

        using var assessMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/assignments/{assignmentId}/assessment", reviewerToken, assessmentReq);
        var assessRes = await _client.SendAsync(assessMsg);
        assessRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var assessmentResult = await assessRes.Content.ReadFromJsonAsync<ProposalAssessmentResponse>(_jsonOptions);
        assessmentResult.Should().NotBeNull();
        assessmentResult!.Status.Should().Be(AssessmentStatus.Submitted);
        assessmentResult.OverallScore.Should().BeGreaterThan(80);

        // 9. Sponsor accepts proposal
        var decisionReq = new SponsorDecisionRequest("Accept", "Funded based on top-tier peer assessment score.");
        using var decisionMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/decision", sponsorToken, decisionReq);
        var decisionRes = await _client.SendAsync(decisionMsg);
        decisionRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptedProposal = await decisionRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);
        acceptedProposal!.Status.Should().Be(ProposalStatus.Accepted);

        // 10. Sponsor converts proposal to ResearchProject
        var convertReq = new ConvertProposalRequest("Perovskite ALD Barrier Project");
        using var convertMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/convert", sponsorToken, convertReq);
        var convertRes = await _client.SendAsync(convertMsg);
        convertRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var conversionResult = await convertRes.Content.ReadFromJsonAsync<ProposalConversionResponse>(_jsonOptions);
        conversionResult.Should().NotBeNull();
        conversionResult!.AlreadyConverted.Should().BeFalse();
        conversionResult.ProjectId.Should().NotBeEmpty();

        // 11. Verify in database: Project OriginType and traceability
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();
            var project = await db.ResearchProjects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == conversionResult.ProjectId);

            project.Should().NotBeNull();
            project!.OriginType.Should().Be(ProjectOriginType.IndustryChallenge);
            project.SourceChallengeId.Should().Be(challengeId);
            project.SourceProposalId.Should().Be(proposalId);
            project.OwnerUserId.Should().Be(leadUserId);
            project.Status.Should().Be(ProjectStatus.Planning);

            // Verify team members transferred: Lead + Co-Investigator + Sponsor
            project.Members.Should().Contain(m => m.UserId == leadUserId && m.Role == ProjectMemberRole.ProjectLead);
            project.Members.Should().Contain(m => m.UserId == coInvestigatorUserId);
            project.Members.Should().Contain(m => m.UserId == sponsorUserId && m.Role == ProjectMemberRole.IndustryPartner);
        }
    }

    [Fact]
    public async Task ProposalConversion_RepeatedCalls_ReturnsIdempotentResponse()
    {
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorIdemp");
        var challengeId = await SeedOpenChallengeAsync(sponsorToken, sponsorUserId);

        var (leadToken, _, _) = await CreateAndAuthenticateUserAsync("leadIdemp");

        // Create and Submit Proposal
        var createReq = new CreateProposalDraftRequest(
            ChallengeId: challengeId,
            Title: "Idempotent Conversion Test Proposal",
            Abstract: "Testing idempotent conversion guarantees across multiple concurrent invocations.",
            ProblemStatement: "Duplicate conversion requests could inadvertently instantiate multiple projects.",
            ResearchObjectives: "Guarantee 1:1 proposal to project conversion mapping.",
            ProposedMethodology: "Database transaction isolation and ConvertedProjectId checking.",
            ExpectedOutcomes: "Single project instance returned consistently.",
            ProposedTimelineMonths: 12,
            ProposedBudget: 80000m
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/proposals", leadToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposal = await createRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);

        using var submitMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal!.Id}/submit", leadToken);
        var submitRes = await _client.SendAsync(submitMsg);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Sponsor accepts
        var decisionReq = new SponsorDecisionRequest("Accept", "Accepted for project launch.");
        using var decMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal.Id}/decision", sponsorToken, decisionReq);
        await _client.SendAsync(decMsg);

        // Conversion 1
        var convertReq = new ConvertProposalRequest();
        using var convMsg1 = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal.Id}/convert", sponsorToken, convertReq);
        var convRes1 = await _client.SendAsync(convMsg1);
        convRes1.StatusCode.Should().Be(HttpStatusCode.OK);
        var convBody1 = await convRes1.Content.ReadFromJsonAsync<ProposalConversionResponse>(_jsonOptions);
        convBody1!.AlreadyConverted.Should().BeFalse();

        // Conversion 2 (Duplicate Call)
        using var convMsg2 = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal.Id}/convert", sponsorToken, convertReq);
        var convRes2 = await _client.SendAsync(convMsg2);
        convRes2.StatusCode.Should().Be(HttpStatusCode.OK);
        var convBody2 = await convRes2.Content.ReadFromJsonAsync<ProposalConversionResponse>(_jsonOptions);
        
        // Invariant: Exact same project ID, AlreadyConverted = true
        convBody2!.ProjectId.Should().Be(convBody1.ProjectId);
        convBody2.AlreadyConverted.Should().BeTrue();

        // Verify database contains exactly one project for this proposal
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();
        var projectCount = await db.ResearchProjects.CountAsync(p => p.SourceProposalId == proposal.Id);
        projectCount.Should().Be(1);
    }

    [Fact]
    public async Task ReviewerAssignment_SelfReviewOrInvestigator_ReturnsBadRequest()
    {
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorSelfRev");
        var challengeId = await SeedOpenChallengeAsync(sponsorToken, sponsorUserId);

        var (leadToken, leadUserId, _) = await CreateAndAuthenticateUserAsync("leadSelfRev");

        var createReq = new CreateProposalDraftRequest(
            ChallengeId: challengeId,
            Title: "Self Review Prohibition Invariant Verification",
            Abstract: "Validating that PI cannot review their own submitted scientific proposal.",
            ProblemStatement: "Conflict of interest violates peer review integrity.",
            ResearchObjectives: "Block self-review attempts.",
            ProposedMethodology: "Strict domain invariant check on reviewer assignment.",
            ExpectedOutcomes: "HTTP 400 with domain error.",
            ProposedTimelineMonths: 12,
            ProposedBudget: 60000m
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/proposals", leadToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposal = await createRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);

        using var submitMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal!.Id}/submit", leadToken);
        var submitRes = await _client.SendAsync(submitMsg);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Sponsor attempts to assign Lead Researcher as reviewer
        var invalidAssignReq = new AssignReviewerRequest(leadUserId);
        using var assignMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal.Id}/reviewers", sponsorToken, invalidAssignReq);
        var assignRes = await _client.SendAsync(assignMsg);

        // Must fail with BadRequest
        assignRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReviewerAssessment_BlindPeerIsolation_Enforced()
    {
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorBlind");
        var challengeId = await SeedOpenChallengeAsync(sponsorToken, sponsorUserId);

        var (leadToken, _, _) = await CreateAndAuthenticateUserAsync("leadBlind");

        var createReq = new CreateProposalDraftRequest(
            ChallengeId: challengeId,
            Title: "Blind Peer Isolation Invariant Verification",
            Abstract: "Ensuring reviewer A cannot observe reviewer B's assignment or scores.",
            ProblemStatement: "Reviewer conformity bias occurs if peer assessments are leaked.",
            ResearchObjectives: "Isolate review assignments by reviewer user identity.",
            ProposedMethodology: "Filtered navigation mapping in proposal read service.",
            ExpectedOutcomes: "Independent assessment scores isolated.",
            ProposedTimelineMonths: 12,
            ProposedBudget: 75000m
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/proposals", leadToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposal = await createRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);

        using var submitMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal!.Id}/submit", leadToken);
        var submitRes = await _client.SendAsync(submitMsg);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assign Reviewer A and Reviewer B
        var (reviewerAToken, reviewerAUserId, _) = await CreateAndAuthenticateUserAsync("revA");
        var (_, reviewerBUserId, _) = await CreateAndAuthenticateUserAsync("revB");

        using var assignAMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal.Id}/reviewers", sponsorToken, new AssignReviewerRequest(reviewerAUserId));
        await _client.SendAsync(assignAMsg);

        using var assignBMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/proposals/{proposal.Id}/reviewers", sponsorToken, new AssignReviewerRequest(reviewerBUserId));
        await _client.SendAsync(assignBMsg);

        // Reviewer A queries proposal details
        using var readMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/proposals/{proposal.Id}", reviewerAToken);
        var readRes = await _client.SendAsync(readMsg);
        readRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var readDetail = await readRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);
        readDetail.Should().NotBeNull();

        // Invariant: Reviewer A sees ONLY their own assignment (Count == 1) and cannot see Reviewer B
        readDetail!.ReviewAssignments.Should().HaveCount(1);
        readDetail.ReviewAssignments.First().ReviewerUserId.Should().Be(reviewerAUserId);
    }

    [Fact]
    public async Task ProposalDetail_AccessByUnauthorizedUser_ReturnsNotFoundConcealment()
    {
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorConceal");
        var challengeId = await SeedOpenChallengeAsync(sponsorToken, sponsorUserId);

        var (leadToken, _, _) = await CreateAndAuthenticateUserAsync("leadConceal");
        var (strangerToken, _, _) = await CreateAndAuthenticateUserAsync("strangerUser");

        var createReq = new CreateProposalDraftRequest(
            ChallengeId: challengeId,
            Title: "Confidential Commercial Patent Proposal",
            Abstract: "Proprietary algorithmic secrets not for public indexing.",
            ProblemStatement: "Intellectual property leakage risk.",
            ResearchObjectives: "Preserve 404 concealment for non-team members.",
            ProposedMethodology: "Authorization filter concealing entity presence.",
            ExpectedOutcomes: "HTTP 404 NotFound returned.",
            ProposedTimelineMonths: 12,
            ProposedBudget: 90000m
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/proposals", leadToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposal = await createRes.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions);

        // Stranger tries to access proposal
        using var strangerMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/proposals/{proposal!.Id}", strangerToken);
        var strangerRes = await _client.SendAsync(strangerMsg);

        // Invariant: Must return 404 NotFound (not 403 Forbidden) to prevent enumeration of confidential proposals
        strangerRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

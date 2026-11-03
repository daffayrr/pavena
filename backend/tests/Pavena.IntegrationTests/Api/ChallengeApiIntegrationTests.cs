using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Identity.DTOs;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class ChallengeApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly PavenaApiFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ChallengeApiIntegrationTests(PavenaApiFactory factory)
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

    private async Task<Organization> SeedVerifiedOrganizationAsync(Guid ownerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var org = new Organization($"AeroDynamics Corp {suffix}", $"aerodynamics-{suffix}", OrganizationType.Company)
        {
            VerificationStatus = OrganizationVerificationStatus.Verified
        };

        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var membership = new OrganizationMembership(org.Id, ownerUserId, OrgMembershipRole.OrganizationAdmin)
        {
            IsActive = true
        };

        db.OrganizationMemberships.Add(membership);
        await db.SaveChangesAsync();

        return org;
    }

    [Fact]
    public async Task ChallengeLifecycle_Draft_Submit_Approve_Publish_OpenProposals_SucceedsEndToEnd()
    {
        // 1. Authenticate sponsor user and seed verified organization
        var (token, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorLead");
        var org = await SeedVerifiedOrganizationAsync(sponsorUserId);

        // 2. Create Draft Challenge
        var createReq = new CreateChallengeRequest(
            OrganizationId: org.Id,
            Title: "Autonomous Underwater Swarm Inspection",
            Summary: "Seeking academic research proposals for autonomous underwater swarm vehicle algorithms.",
            ProblemStatement: "Acoustic signal attenuation in littoral turbid waters causes severe swarm localization degradation beyond 30 meters.",
            ResearchScope: "Subsea littoral pipelines and renewable offshore turbine foundations.",
            ExpectedOutcomes: "Simulation models in Gazebo with ROS2 acoustics fusion library and empirical test reports.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(60),
            Visibility: ChallengeVisibility.Public,
            Eligibility: InstitutionalEligibility.OpenToAll,
            Currency: "USD",
            MinBudget: 80000,
            MaxBudget: 150000,
            EstimatedDurationMonths: 12,
            RequiresManualNdaApproval: false,
            Domains: new() { new CreateDomainRequest("Robotics & Autonomous Systems", "Underwater Swarms", true) },
            Expertise: new() { new CreateExpertiseRequest("Acoustic Sensor Fusion", RequirementImportance.Required) }
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/challenges", token, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdChallenge = await createRes.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions);
        createdChallenge.Should().NotBeNull();
        createdChallenge!.Status.Should().Be(ChallengeStatus.Draft);
        var challengeId = createdChallenge.Id;

        // 3. Submit for Approval
        using var submitMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/submit", token, "Technical specs verified.");
        var submitRes = await _client.SendAsync(submitMsg);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status in DB is SubmittedForApproval
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();
            var ch = await db.IndustryChallenges.FindAsync(challengeId);
            ch!.Status.Should().Be(ChallengeStatus.SubmittedForApproval);
        }

        // 4. Organization Admin Approves Challenge
        var decisionReq = new ChallengeDecisionRequest(ChallengeApprovalDecision.Approved, "Corporate scientific review completed successfully.");
        using var decisionMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/decision", token, decisionReq);
        var decisionRes = await _client.SendAsync(decisionMsg);
        decisionRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Publish Challenge
        using var publishMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/publish", token);
        var publishRes = await _client.SendAsync(publishMsg);
        publishRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Open Proposals
        using var openMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/open-proposals", token);
        var openRes = await _client.SendAsync(openMsg);
        openRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Anonymous Search: public user discovers challenge
        var searchRes = await _client.GetAsync($"/api/v1/challenges?query=Underwater&domain=Robotics");
        searchRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResults = await searchRes.Content.ReadFromJsonAsync<List<ChallengeSummaryDto>>(_jsonOptions);
        searchResults.Should().NotBeNull();
        searchResults!.Should().Contain(c => c.Id == challengeId);
    }

    [Fact]
    public async Task DraftChallenge_AccessByUnauthorizedUser_ReturnsNotFoundConcealment()
    {
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorA");
        var org = await SeedVerifiedOrganizationAsync(sponsorUserId);

        // Create Draft Challenge
        var createReq = new CreateChallengeRequest(
            OrganizationId: org.Id,
            Title: "Proprietary Material Synthesis",
            Summary: "Confidential draft challenge for high-temperature superalloys.",
            ProblemStatement: "Grain boundary embrittlement occurs at 1200 degrees Celsius under cyclic mechanical stresses.",
            ResearchScope: "Single-crystal nickel-based superalloys for aerospace turbines.",
            ExpectedOutcomes: "Molecular dynamics simulation benchmarks.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(40),
            Visibility: ChallengeVisibility.Public,
            Eligibility: InstitutionalEligibility.OpenToAll,
            Domains: new() { new CreateDomainRequest("Materials Science", "Superalloys", true) }
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/challenges", sponsorToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions);

        // Other researcher tries to GET draft
        var (researcherToken, _, _) = await CreateAndAuthenticateUserAsync("unauthorizedScholar");
        using var getMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/challenges/{created!.Id}", researcherToken);
        var getRes = await _client.SendAsync(getMsg);

        // Must return 404 (concealment), preventing resource enumeration
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TwoTierAttachment_And_DigitalNda_GatingLifecycle()
    {
        var (sponsorToken, sponsorUserId, _) = await CreateAndAuthenticateUserAsync("sponsorSec");
        var org = await SeedVerifiedOrganizationAsync(sponsorUserId);

        // Create and publish challenge
        var createReq = new CreateChallengeRequest(
            OrganizationId: org.Id,
            Title: "Rare Earth Catalyst Recovery Pipeline",
            Summary: "Industrial research challenge to recover dysprosium and neodymium from discarded electric motors.",
            ProblemStatement: "Acid leaching creates excessive hazardous effluent without achieving 90% purity recovery.",
            ResearchScope: "Hydrometallurgical and bio-leaching methodologies for e-waste magnets.",
            ExpectedOutcomes: "Validated hydrometallurgical recovery protocols.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(90),
            Visibility: ChallengeVisibility.Public,
            Eligibility: InstitutionalEligibility.OpenToAll,
            RequiresManualNdaApproval: false,
            NdaTermsText: "Standard Pavena Academic Industry NDA v1.0. All proprietary chemical formulations are strictly confidential.",
            Domains: new() { new CreateDomainRequest("Chemical Engineering", "Rare Earth Recovery", true) }
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/challenges", sponsorToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        var challenge = await createRes.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions);
        var challengeId = challenge!.Id;

        // Publish challenge
        using var submitMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/submit", sponsorToken);
        await _client.SendAsync(submitMsg);
        using var approveMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/decision", sponsorToken, new ChallengeDecisionRequest(ChallengeApprovalDecision.Approved));
        await _client.SendAsync(approveMsg);
        using var publishMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/publish", sponsorToken);
        await _client.SendAsync(publishMsg);

        // Upload Public Briefing Attachment
        using var publicUploadContent = new MultipartFormDataContent();
        var publicBytes = Encoding.UTF8.GetBytes("Public briefing content for catalyst challenge.");
        var publicFile = new ByteArrayContent(publicBytes);
        publicFile.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        publicUploadContent.Add(publicFile, "file", "public_brief.pdf");
        publicUploadContent.Add(new StringContent(AttachmentConfidentialityLevel.PublicBriefing.ToString()), "confidentiality");

        using var publicUploadMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/attachments");
        publicUploadMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sponsorToken);
        publicUploadMsg.Content = publicUploadContent;
        var publicUploadRes = await _client.SendAsync(publicUploadMsg);
        publicUploadRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicAtt = await publicUploadRes.Content.ReadFromJsonAsync<ChallengeAttachmentDto>(_jsonOptions);

        // Upload Confidential Attachment
        using var confUploadContent = new MultipartFormDataContent();
        var confBytes = Encoding.UTF8.GetBytes("Confidential proprietary metallurgical assays: NdFeB 99.4% assay dataset.");
        var confFile = new ByteArrayContent(confBytes);
        confFile.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        confUploadContent.Add(confFile, "file", "confidential_assays.csv");
        confUploadContent.Add(new StringContent(AttachmentConfidentialityLevel.ConfidentialNdaRequired.ToString()), "confidentiality");

        using var confUploadMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/attachments");
        confUploadMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sponsorToken);
        confUploadMsg.Content = confUploadContent;
        var confUploadRes = await _client.SendAsync(confUploadMsg);
        confUploadRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var confAtt = await confUploadRes.Content.ReadFromJsonAsync<ChallengeAttachmentDto>(_jsonOptions);

        // 2. Researcher logs in and checks NDA status
        var (researcherToken, _, _) = await CreateAndAuthenticateUserAsync("catalystResearcher");
        using var ndaStatusMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/challenges/{challengeId}/nda/status", researcherToken);
        var ndaStatusRes = await _client.SendAsync(ndaStatusMsg);
        ndaStatusRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var ndaStatus = await ndaStatusRes.Content.ReadFromJsonAsync<ChallengeNdaStatusDto>(_jsonOptions);
        ndaStatus!.HasSignedNda.Should().BeFalse();

        // 3. Researcher downloads Public Attachment -> 200 OK
        using var dlPublicMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/challenges/{challengeId}/attachments/{publicAtt!.Id}/download", researcherToken);
        var dlPublicRes = await _client.SendAsync(dlPublicMsg);
        dlPublicRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadedPublic = await dlPublicRes.Content.ReadAsByteArrayAsync();
        downloadedPublic.Should().BeEquivalentTo(publicBytes);

        // 4. Researcher tries to download Confidential Attachment without NDA -> 403 Forbidden
        using var dlConfBlockedMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/challenges/{challengeId}/attachments/{confAtt!.Id}/download", researcherToken);
        var dlConfBlockedRes = await _client.SendAsync(dlConfBlockedMsg);
        dlConfBlockedRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. Researcher signs NDA
        var termsHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(createReq.NdaTermsText!)));
        var signReq = new SignNdaRequest(challenge.NdaTermsVersion, termsHash);

        using var signMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/challenges/{challengeId}/nda/sign", researcherToken, signReq);
        var signRes = await _client.SendAsync(signMsg);
        signRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var signedStatus = await signRes.Content.ReadFromJsonAsync<ChallengeNdaStatusDto>(_jsonOptions);
        signedStatus!.HasSignedNda.Should().BeTrue();
        signedStatus.IsApproved.Should().BeTrue();

        // 6. Researcher downloads Confidential Attachment after signing -> 200 OK
        using var dlConfAllowedMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/challenges/{challengeId}/attachments/{confAtt.Id}/download", researcherToken);
        var dlConfAllowedRes = await _client.SendAsync(dlConfAllowedMsg);
        dlConfAllowedRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadedConf = await dlConfAllowedRes.Content.ReadAsByteArrayAsync();
        downloadedConf.Should().BeEquivalentTo(confBytes);
    }
}

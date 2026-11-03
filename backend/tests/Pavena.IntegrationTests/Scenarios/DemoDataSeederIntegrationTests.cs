using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pavena.Application.Identity.DTOs;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Proposals;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Pavena.Infrastructure.Seeding;
using Pavena.IntegrationTests.Api;
using Xunit;

namespace Pavena.IntegrationTests.Scenarios;

public class DemoDataSeederIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly PavenaApiFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DemoDataSeederIntegrationTests(PavenaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DemoDataSeeder_ExecutesIdempotently_AndCreatesRealisticScenarios()
    {
        // -------------------------------------------------------------
        // Step 1: Execute Seeder First Time
        // -------------------------------------------------------------
        using (var scope = _factory.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await seeder.SeedAsync();
        }

        // -------------------------------------------------------------
        // Step 2: Execute Seeder Second Time (Idempotency Test)
        // -------------------------------------------------------------
        using (var scope = _factory.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            // Must not throw duplicate key or unique constraint exceptions
            var act = () => seeder.SeedAsync();
            await act.Should().NotThrowAsync();
        }

        // -------------------------------------------------------------
        // Step 3: Verify User Authentication with Default Demo Password
        // -------------------------------------------------------------
        string[] demoEmails =
        [
            "admin@pavena.local",
            "andi.pratama@und.ac.id",
            "siti.rahmawati@itc.ac.id",
            "budi.santoso@prin.go.id",
            "maya.putri@und.ac.id",
            "eko.wijaya@prin.go.id",
            "rina.dewi@itc.ac.id",
            "diana.kusuma@energi-hijau.co.id"
        ];

        foreach (var email in demoEmails)
        {
            var loginReq = new LoginRequest(email, DemoDataSeeder.DefaultDemoPassword);
            var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
            loginRes.StatusCode.Should().Be(HttpStatusCode.OK, $"User {email} should be able to log in with demo password.");

            var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
            body.Should().NotBeNull();
            body!.AccessToken.Should().NotBeNullOrWhiteSpace();
            body.Email.Should().Be(email);
        }

        // -------------------------------------------------------------
        // Step 4: Verify Database Invariants & Entity Relationships
        // -------------------------------------------------------------
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PavenaDbContext>();

            // Organizations
            var und = await db.Organizations.FirstOrDefaultAsync(o => o.Slug == "und");
            und.Should().NotBeNull();
            und!.VerificationStatus.Should().Be(OrganizationVerificationStatus.Verified);

            var itc = await db.Organizations.FirstOrDefaultAsync(o => o.Slug == "itc");
            itc.Should().NotBeNull();

            var prin = await db.Organizations.FirstOrDefaultAsync(o => o.Slug == "prin");
            prin.Should().NotBeNull();

            var energiHijau = await db.Organizations.FirstOrDefaultAsync(o => o.Slug == "energi-hijau");
            energiHijau.Should().NotBeNull();

            // HULU: Researcher-Led Project
            var huluProject = await db.ResearchProjects
                .Include(p => p.Members)
                .Include(p => p.Milestones)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Slug == "eco-drone-swarm");

            huluProject.Should().NotBeNull();
            huluProject!.OriginType.Should().Be(ProjectOriginType.ResearcherInitiated);
            huluProject.Members.Should().HaveCountGreaterOrEqualTo(2);
            huluProject.Milestones.Should().HaveCount(3);
            huluProject.Milestones.Should().Contain(m => m.Status == MilestoneStatus.Achieved);
            huluProject.Tasks.Should().HaveCount(3);
            huluProject.Tasks.Should().Contain(t => t.Status == ResearchTaskStatus.Done);

            // HULU: Collaborative LaTeX Manuscript
            var latexDoc = await db.Documents
                .Include(d => d.Files)
                .Include(d => d.Collaborators)
                .FirstOrDefaultAsync(d => d.ProjectId == huluProject.Id && d.Title == "Peatland-Swarm-Monitoring-Framework");

            latexDoc.Should().NotBeNull();
            latexDoc!.Mode.Should().Be(EditorMode.Latex);
            latexDoc.Files.Should().Contain(f => f.IsMain && f.FileName == "main.tex");
            latexDoc.Collaborators.Should().Contain(c => c.Role == DocumentPermissionRole.Editor);

            // HILIR: Industry Challenge & Attachments
            var challenge = await db.IndustryChallenges
                .Include(c => c.Domains)
                .Include(c => c.Attachments)
                .Include(c => c.NdaAgreements)
                .FirstOrDefaultAsync(c => c.Slug == "tropical-tandem-pv-encapsulation");

            challenge.Should().NotBeNull();
            challenge!.Status.Should().Be(ChallengeStatus.OpenForProposals);
            challenge.Attachments.Should().Contain(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.PublicBriefing);
            challenge.Attachments.Should().Contain(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.ConfidentialNdaRequired);

            // NDA Verification
            var sitiUser = await db.Users.FirstAsync(u => u.Email == "siti.rahmawati@itc.ac.id");
            var andiUser = await db.Users.FirstAsync(u => u.Email == "andi.pratama@und.ac.id");
            challenge.NdaAgreements.Should().Contain(a => a.ResearcherUserId == sitiUser.Id);
            challenge.NdaAgreements.Should().NotContain(a => a.ResearcherUserId == andiUser.Id);

            // Proposal A: Accepted & Converted to Project
            var proposalA = await db.ResearchProposals
                .FirstOrDefaultAsync(p => p.Slug == "fluoropolymer-tandem-pv-encapsulation");

            proposalA.Should().NotBeNull();
            proposalA!.Status.Should().Be(ProposalStatus.ConvertedToProject);
            proposalA.ConvertedProjectId.Should().NotBeNull();

            var convertedProject = await db.ResearchProjects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == proposalA.ConvertedProjectId);

            convertedProject.Should().NotBeNull();
            convertedProject!.OriginType.Should().Be(ProjectOriginType.IndustryChallenge);
            convertedProject.SourceChallengeId.Should().Be(challenge.Id);
            convertedProject.SourceProposalId.Should().Be(proposalA.Id);
            convertedProject.Members.Should().Contain(m => m.Role == ProjectMemberRole.IndustryPartner);

            // Proposal B: Under Review with Multi-Reviewer Rubric & Assessments
            var proposalB = await db.ResearchProposals
                .Include(p => p.ReviewAssignments).ThenInclude(a => a.Assessment)
                .FirstOrDefaultAsync(p => p.Slug == "graphene-nanocomposite-pv-barrier");

            proposalB.Should().NotBeNull();
            proposalB!.Status.Should().Be(ProposalStatus.UnderReview);
            proposalB.ReviewAssignments.Should().HaveCount(2);
            proposalB.ReviewAssignments.Should().OnlyContain(a => a.Status == ReviewAssignmentStatus.Completed && a.HasCoiDeclaration);

            var assessments = await db.ProposalAssessments
                .Where(a => a.ProposalId == proposalB.Id)
                .ToListAsync();

            assessments.Should().HaveCount(2);
            assessments.Should().AllSatisfy(ass =>
            {
                ass.Status.Should().Be(AssessmentStatus.Submitted);
                ass.OverallScore.Should().BeGreaterThan(80);
            });

            // COI & Blocking Enforcement
            var budiUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "budi.santoso@prin.go.id");
            var hendraUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "hendra.gunawan@und.ac.id");

            var block = await db.UserBlocks.FirstOrDefaultAsync(b => b.BlockerUserId == budiUser!.Id && b.BlockedUserId == hendraUser!.Id);
            block.Should().NotBeNull("Dr. Budi Santoso must block Dr. Hendra Gunawan for COI enforcement.");

            // Social Graph & Posts
            var posts = await db.Posts.ToListAsync();
            posts.Should().Contain(p => p.Visibility == PostVisibility.Public);
            posts.Should().Contain(p => p.Visibility == PostVisibility.Connections);
            posts.Should().Contain(p => p.Visibility == PostVisibility.OnlyMe);

            var connections = await db.Connections.ToListAsync();
            connections.Should().Contain(c => c.Status == ConnectionStatus.Accepted);
            connections.Should().Contain(c => c.Status == ConnectionStatus.Pending);
        }
    }
}

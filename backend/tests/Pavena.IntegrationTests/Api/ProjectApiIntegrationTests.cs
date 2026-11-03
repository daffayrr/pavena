using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Research.DTOs;
using Pavena.IntegrationTests.Api;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class ProjectApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProjectApiIntegrationTests(PavenaApiFactory factory)
    {
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

    [Fact]
    public async Task ProjectLifecycle_CreatePersonal_Update_AdvanceStage_ChangeStatus_WorksEndToEnd()
    {
        var (token, userId, _) = await CreateAndAuthenticateUserAsync("projLead");
        var slug = $"ai-diagnostics-{Guid.NewGuid():N}"[..25];

        // 1. Create Project
        var createReq = new CreateProjectRequest(
            Title: "AI Diagnostics for Oncology",
            Slug: slug,
            Description: "Deep learning models for early stage tumour detection",
            ResearchField: "Medical AI",
            ResearchObjectives: "Build multimodal image segmentation pipeline",
            Visibility: "Private"
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", token, createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await createRes.Content.ReadFromJsonAsync<ProjectSummaryDto>(_jsonOptions);
        summary.Should().NotBeNull();
        summary!.Title.Should().Be("AI Diagnostics for Oncology");
        summary.Slug.Should().Be(slug);
        summary.OriginType.Should().Be("ResearcherInitiated");
        summary.Status.Should().Be("Active");
        summary.Stage.Should().Be("Concept");

        // 2. Get Project by ID
        using var getMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{summary.Id}", token);
        var getRes = await _client.SendAsync(getMsg);
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getRes.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions);
        detail.Should().NotBeNull();
        detail!.ResearchObjectives.Should().Be("Build multimodal image segmentation pipeline");
        detail.Members.Should().ContainSingle(m => m.UserId == userId && m.Role == "ProjectLead");

        // 3. Update Metadata
        var updateReq = new UpdateProjectRequest(
            Title: "AI Diagnostics for Oncology (Phase 1)",
            Description: "Updated scope with clinical validation",
            ResearchField: "Medical AI & Oncology",
            ResearchObjectives: "Validate on 5,000 histopathology slides",
            Visibility: "Private"
        );

        using var updateMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/projects/{summary.Id}", token, updateReq);
        var updateRes = await _client.SendAsync(updateMsg);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedDetail = await updateRes.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions);
        updatedDetail!.Title.Should().Be("AI Diagnostics for Oncology (Phase 1)");

        // 4. Advance Stage
        var stageReq = new AdvanceProjectStageRequest("MethodologyDesign");
        using var stageMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{summary.Id}/stage", token, stageReq);
        var stageRes = await _client.SendAsync(stageMsg);
        stageRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var stagedDetail = await stageRes.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions);
        stagedDetail!.Stage.Should().Be("MethodologyDesign");

        // 5. Update Status
        var statusReq = new UpdateProjectStatusRequest("Planning", "Refining clinical partner protocols");
        using var statusMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{summary.Id}/status", token, statusReq);
        var statusRes = await _client.SendAsync(statusMsg);
        statusRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusDetail = await statusRes.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions);
        statusDetail!.Status.Should().Be("Planning");
    }

    [Fact]
    public async Task ProjectCollaboration_InviteMember_AcceptInvitation_UpdateRole_RemoveMember()
    {
        var (leadToken, leadId, _) = await CreateAndAuthenticateUserAsync("teamLead");
        var (memberToken, memberId, _) = await CreateAndAuthenticateUserAsync("colleague");

        var slug = $"quantum-sim-{Guid.NewGuid():N}"[..25];
        var createReq = new CreateProjectRequest("Quantum Simulation", slug, "Simulation of spin chains", "Quantum Physics");
        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", leadToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectSummaryDto>(_jsonOptions);

        // 1. Lead invites colleague
        var inviteReq = new InviteMemberRequest(memberId, "CoResearcher", "Please join our quantum study.");
        using var inviteMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members/invite", leadToken, inviteReq);
        var inviteRes = await _client.SendAsync(inviteMsg);
        inviteRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitation = await inviteRes.Content.ReadFromJsonAsync<ProjectInvitationDto>(_jsonOptions);
        invitation.Should().NotBeNull();
        invitation!.Status.Should().Be("Pending");
        invitation.RoleOffered.Should().Be("CoResearcher");

        // 2. Colleague retrieves my invitations
        using var myInvsMsg = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/projects/invitations/mine", memberToken);
        var myInvsRes = await _client.SendAsync(myInvsMsg);
        myInvsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var myInvs = await myInvsRes.Content.ReadFromJsonAsync<List<ProjectInvitationDto>>(_jsonOptions);
        myInvs.Should().Contain(i => i.Id == invitation.Id);

        // 3. Colleague accepts invitation
        var respondReq = new RespondInvitationRequest(Accept: true);
        using var respondMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/invitations/{invitation.Id}/respond", memberToken, respondReq);
        var respondRes = await _client.SendAsync(respondMsg);
        respondRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify member is now active on project
        using var getProjectMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{project.Id}", memberToken);
        var getProjectRes = await _client.SendAsync(getProjectMsg);
        getProjectRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getProjectRes.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions);
        detail!.Members.Should().Contain(m => m.UserId == memberId && m.Role == "CoResearcher" && m.Status == "Active");

        // 5. Lead changes role to Researcher
        var updateRoleReq = new UpdateMemberRoleRequest("Researcher");
        using var roleMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/projects/{project.Id}/members/{memberId}/role", leadToken, updateRoleReq);
        var roleRes = await _client.SendAsync(roleMsg);
        roleRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Lead removes member
        using var removeMsg = CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/v1/projects/{project.Id}/members/{memberId}", leadToken);
        var removeRes = await _client.SendAsync(removeMsg);
        removeRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Removed member can NO LONGER view the private project
        using var postRemovalMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{project.Id}", memberToken);
        var postRemovalRes = await _client.SendAsync(postRemovalMsg);
        postRemovalRes.StatusCode.Should().Be(HttpStatusCode.NotFound); // Masked
    }

    [Fact]
    public async Task TasksAndMilestones_Create_Update_Complete_TracksTimeline()
    {
        var (leadToken, leadId, _) = await CreateAndAuthenticateUserAsync("taskLead");
        var slug = $"renewable-grid-{Guid.NewGuid():N}"[..25];
        var createReq = new CreateProjectRequest("Smart Renewable Grid", slug, "Grid stability simulation", "Electrical Engineering");
        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", leadToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectSummaryDto>(_jsonOptions);

        // 1. Create Milestone
        var milestoneReq = new CreateMilestoneRequest("Algorithm Benchmark", DateTime.UtcNow.AddMonths(1), "Complete standard IEEE benchmarks");
        using var msMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/milestones", leadToken, milestoneReq);
        var msRes = await _client.SendAsync(msMsg);
        msRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var milestone = await msRes.Content.ReadFromJsonAsync<ProjectMilestoneDto>(_jsonOptions);
        milestone.Should().NotBeNull();
        milestone!.Status.Should().Be("Planned");

        // 2. Create Task linked to milestone
        var taskReq = new CreateTaskRequest("Implement IEEE 14-bus test feeder", "Simulate power flow", leadId, "High", DateTime.UtcNow.AddDays(14), milestone.Id);
        using var taskMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/tasks", leadToken, taskReq);
        var taskRes = await _client.SendAsync(taskMsg);
        taskRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await taskRes.Content.ReadFromJsonAsync<ProjectTaskDto>(_jsonOptions);
        task.Should().NotBeNull();
        task!.MilestoneId.Should().Be(milestone.Id);
        task.Status.Should().Be("Todo");

        // 3. Update Task to Done
        var updateTaskReq = new UpdateTaskRequest(task.Title, task.Description, leadId, "High", "Done", task.DueDateUtc, milestone.Id);
        using var updateTaskMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/projects/{project.Id}/tasks/{task.Id}", leadToken, updateTaskReq);
        var updateTaskRes = await _client.SendAsync(updateTaskMsg);
        updateTaskRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var completedTask = await updateTaskRes.Content.ReadFromJsonAsync<ProjectTaskDto>(_jsonOptions);
        completedTask!.Status.Should().Be("Done");
        completedTask.CompletedAtUtc.Should().NotBeNull();

        // 4. Update Milestone to Achieved
        var updateMsReq = new UpdateMilestoneRequest(milestone.Title, milestone.TargetDateUtc, "Achieved", "All feeder tests passed");
        using var updateMsMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/projects/{project.Id}/milestones/{milestone.Id}", leadToken, updateMsReq);
        var updateMsRes = await _client.SendAsync(updateMsMsg);
        updateMsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var achievedMs = await updateMsRes.Content.ReadFromJsonAsync<ProjectMilestoneDto>(_jsonOptions);
        achievedMs!.Status.Should().Be("Achieved");
        achievedMs.AchievedAtUtc.Should().NotBeNull();

        // 5. Verify Timeline Log
        using var timelineMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{project.Id}/timeline", leadToken);
        var timelineRes = await _client.SendAsync(timelineMsg);
        timelineRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await timelineRes.Content.ReadFromJsonAsync<List<ProjectActivityLogDto>>(_jsonOptions);
        logs.Should().Contain(l => l.Action == "TaskCompleted");
        logs.Should().Contain(l => l.Action == "MilestoneAchieved");
    }

    [Fact]
    public async Task Security_PrivateProjectConcealment_AndUnauthorizedAccessBlocked()
    {
        var (authorToken, authorId, _) = await CreateAndAuthenticateUserAsync("secProjAuthor");
        var (attackerToken, attackerId, _) = await CreateAndAuthenticateUserAsync("secProjAttacker");

        var slug = $"stealth-fusion-{Guid.NewGuid():N}"[..25];
        var createReq = new CreateProjectRequest("Stealth Fusion Reactor", slug, "Classified fusion data", "Nuclear Physics", Visibility: "Private");
        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", authorToken, createReq);
        var createRes = await _client.SendAsync(createMsg);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectSummaryDto>(_jsonOptions);

        // 1. Attacker attempts direct GET /api/v1/projects/{id} -> returns 404 (Masked existence)
        using var attackGetMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{project!.Id}", attackerToken);
        var attackGetRes = await _client.SendAsync(attackGetMsg);
        attackGetRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. Attacker attempts to update project metadata -> 404
        var updateReq = new UpdateProjectRequest("Hacked Title", Visibility: "PublicDiscovery");
        using var attackPutMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/projects/{project.Id}", attackerToken, updateReq);
        var attackPutRes = await _client.SendAsync(attackPutMsg);
        attackPutRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Attacker attempts to add task to private project -> 404
        var taskReq = new CreateTaskRequest("Malicious Task");
        using var attackTaskMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/tasks", attackerToken, taskReq);
        var attackTaskRes = await _client.SendAsync(attackTaskMsg);
        attackTaskRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class AcademicProfileApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly PavenaApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AcademicProfileApiIntegrationTests(PavenaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Username)> CreateAndAuthenticateUserAsync(string prefix)
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@pavena.test";
        var username = $"{prefix}_{Guid.NewGuid():N}"[..20];
        var password = "SecurePassword123!";

        var registerReq = new RegisterRequest(email, username, password, $"Scholar {prefix}");
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regRes.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPavenaDbContext>();
        var user = db.Users.First(u => u.Email == email);
        user.EmailConfirmed = true;
        await db.SaveChangesAsync();

        var loginReq = new LoginRequest(email, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.EnsureSuccessStatusCode();

        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return (loginData!.AccessToken, loginData.UserId, username);
    }

    [Fact]
    public async Task GetAcademicProfile_ReturnsEnrichedData_WithCountsAndCompleteness()
    {
        var (token, userId, username) = await CreateAndAuthenticateUserAsync("acadProf");

        // 1. Add Education
        var eduReq = new CreateEducationRequest("Universitas Indonesia", "S.T.", "Teknik Komputer", 2016, 2020, "3.85");
        using var eduMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/education")
        {
            Content = JsonContent.Create(eduReq)
        };
        eduMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var eduRes = await _client.SendAsync(eduMsg);
        eduRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Add Experience
        var expReq = new CreateExperienceRequest("Pusat Riset AI", "Asisten Peneliti", "Full-time", new DateTime(2021, 1, 1), null, true, "Riset NLP");
        using var expMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/experience")
        {
            Content = JsonContent.Create(expReq)
        };
        expMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var expRes = await _client.SendAsync(expMsg);
        expRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Add Skill
        var skillReq = new CreateSkillRequest("Distributed Systems");
        using var skillMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/skills")
        {
            Content = JsonContent.Create(skillReq)
        };
        skillMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var skillRes = await _client.SendAsync(skillMsg);
        skillRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Publish a post to increment PublicationsCount
        var postReq = new CreatePostRequest("Paper riset tentang model konsensus terdistribusi.", "Public", true);
        using var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
        {
            Content = JsonContent.Create(postReq)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var postRes = await _client.SendAsync(postMsg);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 5. Get My Academic Profile
        using var getMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me/academic-profile");
        getMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getMsg);
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await getRes.Content.ReadFromJsonAsync<AcademicProfileDto>(JsonOptions);
        profile.Should().NotBeNull();
        profile!.UserId.Should().Be(userId);
        profile.Username.Should().Be(username);
        profile.CanEdit.Should().BeTrue();
        profile.Educations.Should().HaveCount(1);
        profile.Educations[0].InstitutionName.Should().Be("Universitas Indonesia");
        profile.Experiences.Should().HaveCount(1);
        profile.Experiences[0].OrganizationName.Should().Be("Pusat Riset AI");
        profile.Skills.Should().HaveCount(1);
        profile.Skills[0].SkillName.Should().Be("Distributed Systems");
        profile.PublicationsCount.Should().Be(1);
        profile.CompletenessScore.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public async Task Privacy_WhenPrivate_MasksDetailsForUnrelatedUsers()
    {
        var (tokenA, userAId, usernameA) = await CreateAndAuthenticateUserAsync("privA");
        var (tokenB, _, _) = await CreateAndAuthenticateUserAsync("viewerB");

        // User A adds education and updates profile to Private
        var eduReq = new CreateEducationRequest("Harvard University", "Ph.D.", "Physics", 2018, 2022, null);
        using var eduMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/education")
        {
            Content = JsonContent.Create(eduReq)
        };
        eduMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await _client.SendAsync(eduMsg);

        var updateReq = new UpdateProfileRequest("Scholar Private A", "Quantum Physicist", "Secret research bio", "US", "Boston", "en", "Private");
        using var updMsg = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me")
        {
            Content = JsonContent.Create(updateReq)
        };
        updMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var updRes = await _client.SendAsync(updMsg);
        updRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // User B views User A's profile
        using var viewMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{usernameA}/academic-profile");
        viewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewRes = await _client.SendAsync(viewMsg);
        viewRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var viewedProfile = await viewRes.Content.ReadFromJsonAsync<AcademicProfileDto>(JsonOptions);
        viewedProfile.Should().NotBeNull();
        viewedProfile!.DisplayName.Should().Be("Scholar Private A");
        viewedProfile.Bio.Should().BeNull("Private profile bio must be masked from other users");
        viewedProfile.Educations.Should().BeEmpty("Private profile education history must be masked from other users");
        viewedProfile.Experiences.Should().BeEmpty("Private profile experiences must be masked from other users");
        viewedProfile.Skills.Should().BeEmpty("Private profile skills must be masked from other users");
        viewedProfile.CanEdit.Should().BeFalse();

        // User A views own profile
        using var ownViewMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me/academic-profile");
        ownViewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var ownRes = await _client.SendAsync(ownViewMsg);
        ownRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var ownProfile = await ownRes.Content.ReadFromJsonAsync<AcademicProfileDto>(JsonOptions);
        ownProfile.Should().NotBeNull();
        ownProfile!.Bio.Should().Be("Secret research bio");
        ownProfile.Educations.Should().HaveCount(1);
        ownProfile.CanEdit.Should().BeTrue();
    }

    [Fact]
    public async Task EducationAndExperience_CRUD_WorksProperly()
    {
        var (token, _, _) = await CreateAndAuthenticateUserAsync("crudProf");

        // 1. Add Education
        var eduReq = new CreateEducationRequest("MIT", "M.Sc.", "Robotics", 2018, 2020, null);
        using var eduMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/education") { Content = JsonContent.Create(eduReq) };
        eduMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var eduRes = await _client.SendAsync(eduMsg);
        eduRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdEdu = await eduRes.Content.ReadFromJsonAsync<UserEducationDto>(JsonOptions);
        createdEdu.Should().NotBeNull();

        // 2. Delete Education
        using var delEduMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/education/{createdEdu!.Id}");
        delEduMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var delEduRes = await _client.SendAsync(delEduMsg);
        delEduRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. Add Experience
        var expReq = new CreateExperienceRequest("DeepMind", "Research Scientist", "Full-time", new DateTime(2020, 1, 1), null, true, "AI Safety");
        using var expMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/experience") { Content = JsonContent.Create(expReq) };
        expMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var expRes = await _client.SendAsync(expMsg);
        expRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdExp = await expRes.Content.ReadFromJsonAsync<UserExperienceDto>(JsonOptions);
        createdExp.Should().NotBeNull();

        // 4. Delete Experience
        using var delExpMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/experience/{createdExp!.Id}");
        delExpMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var delExpRes = await _client.SendAsync(delExpMsg);
        delExpRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Add Skill & Delete Skill
        var skillReq = new CreateSkillRequest("Quantum Machine Learning");
        using var skillMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/skills") { Content = JsonContent.Create(skillReq) };
        skillMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var skillRes = await _client.SendAsync(skillMsg);
        skillRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdSkill = await skillRes.Content.ReadFromJsonAsync<UserSkillDto>(JsonOptions);
        createdSkill.Should().NotBeNull();

        using var delSkillMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/me/skills/{createdSkill!.Id}");
        delSkillMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var delSkillRes = await _client.SendAsync(delSkillMsg);
        delSkillRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

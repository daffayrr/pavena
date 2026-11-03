using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pavena.Application.Common.DTOs;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;
using Pavena.IntegrationTests.Api;
using Xunit;

namespace Pavena.IntegrationTests.Scenarios;

public class EndToEndResearcherCollaborationScenarioTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EndToEndResearcherCollaborationScenarioTests(PavenaApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Email, string Username)> RegisterAndLoginAsync(string prefix, string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{suffix}@academic.test";
        var username = $"{prefix}_{suffix}";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, username, password, name, "GB");
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginReq = new LoginRequest(email, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var authBody = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
        return (authBody!.AccessToken, authBody.UserId, email, username);
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
    public async Task FullResearcherCollaborationScenario_EndToEnd()
    {
        // -------------------------------------------------------------
        // Step 1: Register Researcher 1 (Dr. Alice Smith - Cambridge)
        // -------------------------------------------------------------
        var (aliceToken, aliceId, aliceEmail, aliceUser) = await RegisterAndLoginAsync("alice", "Dr. Alice Smith");
        aliceId.Should().NotBeEmpty();

        // -------------------------------------------------------------
        // Step 2: Register Researcher 2 (Dr. Bob Jones - Oxford)
        // -------------------------------------------------------------
        var (bobToken, bobId, bobEmail, bobUser) = await RegisterAndLoginAsync("bob", "Dr. Bob Jones");
        bobId.Should().NotBeEmpty();

        // -------------------------------------------------------------
        // Step 3: Alice completes profile with headline and bio
        // -------------------------------------------------------------
        var aliceProfileUpdate = new UpdateProfileRequest(
            DisplayName: "Dr. Alice Smith",
            Headline: "Senior AI Researcher at Cambridge University",
            Bio: "Pioneering neural-symbolic integration and verifiable AI.",
            CountryCode: "GB",
            City: "Cambridge",
            PreferredLanguage: "en",
            Visibility: "Public"
        );

        using var updateProfileMsg = CreateAuthenticatedRequest(HttpMethod.Put, "/api/v1/users/me", aliceToken, aliceProfileUpdate);
        var updateProfileRes = await _client.SendAsync(updateProfileMsg);
        updateProfileRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------------------------------------------------------------
        // Step 4: Bob discovers Alice via search
        // -------------------------------------------------------------
        using var discoverMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/users/discover?q={aliceUser}", bobToken);
        var discoverRes = await _client.SendAsync(discoverMsg);
        discoverRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var discoveryResults = await discoverRes.Content.ReadFromJsonAsync<List<ResearcherDiscoveryDto>>(_jsonOptions);
        discoveryResults.Should().NotBeNull();
        discoveryResults.Should().Contain(r => r.UserId == aliceId);

        // -------------------------------------------------------------
        // Step 5: Bob sends connection request to Alice
        // -------------------------------------------------------------
        var sendConnectionReq = new SendConnectionRequest(aliceId);
        using var sendConnMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/connections/requests", bobToken, sendConnectionReq);
        var sendConnRes = await _client.SendAsync(sendConnMsg);
        sendConnRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var connectionDto = await sendConnRes.Content.ReadFromJsonAsync<ConnectionDto>(_jsonOptions);
        connectionDto.Should().NotBeNull();
        connectionDto!.Status.Should().Be("Pending");

        // -------------------------------------------------------------
        // Step 6: Alice accepts Bob's connection request
        // -------------------------------------------------------------
        using var acceptConnMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/connections/requests/{connectionDto.Id}/accept", aliceToken);
        var acceptConnRes = await _client.SendAsync(acceptConnMsg);
        acceptConnRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptedConnection = await acceptConnRes.Content.ReadFromJsonAsync<ConnectionDto>(_jsonOptions);
        acceptedConnection!.Status.Should().Be("Accepted");

        // -------------------------------------------------------------
        // Step 7: Alice publishes a Post with Visibility = Connections & Category = ResearchFinding
        // -------------------------------------------------------------
        var createPostReq = new CreatePostRequest(
            Content: "Breakthrough results: Neuro-symbolic proof verified across 10,000 theorem benchmarks.",
            Visibility: "Connections",
            PublishImmediately: true,
            Category: "ResearchFinding"
        );

        using var createPostMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/posts", aliceToken, createPostReq);
        var createPostRes = await _client.SendAsync(createPostMsg);
        createPostRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var alicePost = await createPostRes.Content.ReadFromJsonAsync<PostDto>(_jsonOptions);
        alicePost.Should().NotBeNull();
        alicePost!.Visibility.Should().Be("Connections");
        alicePost.Category.Should().Be("ResearchFinding");
        alicePost.Status.Should().Be("Published");

        // -------------------------------------------------------------
        // Step 8: Bob retrieves My Network Feed and observes Alice's post
        // -------------------------------------------------------------
        using var bobFeedMsg = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/feed/network", bobToken);
        var bobFeedRes = await _client.SendAsync(bobFeedMsg);
        bobFeedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var bobFeed = await bobFeedRes.Content.ReadFromJsonAsync<FeedResponseDto>(_jsonOptions);
        bobFeed.Should().NotBeNull();
        bobFeed!.Items.Should().Contain(p => p.Id == alicePost.Id);

        var observedPost = bobFeed.Items.First(p => p.Id == alicePost.Id);
        observedPost.AuthorDisplayName.Should().Be("Dr. Alice Smith");
        observedPost.AuthorHeadline.Should().Be("Senior AI Researcher at Cambridge University");
        observedPost.Category.Should().Be("ResearchFinding");

        // -------------------------------------------------------------
        // Step 9: Register Dr. Charlie (unaffiliated) and test visibility isolation
        // -------------------------------------------------------------
        var (charlieToken, charlieId, _, _) = await RegisterAndLoginAsync("charlie", "Dr. Charlie Unaffiliated");

        // Charlie Discover feed should NOT show Alice's Connections-only post
        using var charlieDiscoverFeedMsg = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/feed/discover", charlieToken);
        var charlieDiscoverRes = await _client.SendAsync(charlieDiscoverFeedMsg);
        charlieDiscoverRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var charlieDiscoverFeed = await charlieDiscoverRes.Content.ReadFromJsonAsync<FeedResponseDto>(_jsonOptions);
        charlieDiscoverFeed!.Items.Should().NotContain(p => p.Id == alicePost.Id);

        // Charlie direct access to Alice's Connections-only post should be blocked (404 masked)
        using var charlieDirectPostMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/posts/{alicePost.Id}", charlieToken);
        var charlieDirectPostRes = await _client.SendAsync(charlieDirectPostMsg);
        charlieDirectPostRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // -------------------------------------------------------------
        // Step 10: Alice blocks Bob. Verify bilateral severance and feed purge.
        // -------------------------------------------------------------
        var blockReq = new BlockUserRequest("Research dispute");
        using var blockMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/users/{bobId}/block", aliceToken, blockReq);
        var blockRes = await _client.SendAsync(blockMsg);
        blockRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Bob network feed must NO LONGER contain Alice's post
        using var bobPostBlockFeedMsg = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/feed/network", bobToken);
        var bobPostBlockFeedRes = await _client.SendAsync(bobPostBlockFeedMsg);
        bobPostBlockFeedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var bobPostBlockFeed = await bobPostBlockFeedRes.Content.ReadFromJsonAsync<FeedResponseDto>(_jsonOptions);
        bobPostBlockFeed!.Items.Should().NotContain(p => p.Id == alicePost.Id);

        // Bob direct post access to Alice's post must be blocked
        using var bobDirectMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/posts/{alicePost.Id}", bobToken);
        var bobDirectRes = await _client.SendAsync(bobDirectMsg);
        bobDirectRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

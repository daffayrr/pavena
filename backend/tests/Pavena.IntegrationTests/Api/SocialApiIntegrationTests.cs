using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class SocialApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;

    public SocialApiIntegrationTests(PavenaApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Email)> CreateAndAuthenticateUserAsync(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{suffix}@pavena.org";
        var username = $"{prefix}_{suffix}";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, username, password, $"User {prefix}", "US");
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginReq = new LoginRequest(email, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var authBody = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (authBody!.AccessToken, authBody.UserId, email);
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _client;
        var requestClient = new HttpClient(_client.BaseAddress != null ? new SocketsHttpHandler() : new HttpClientHandler());
        return client;
    }

    [Fact]
    public async Task ConnectionLifecycle_Send_Accept_Status_Remove_WorksEndToEnd()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("connA");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("connB");

        using var requestA = new HttpRequestMessage(HttpMethod.Post, "/api/v1/connections/requests");
        requestA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        requestA.Content = JsonContent.Create(new SendConnectionRequest(userBId));
        var sendRes = await _client.SendAsync(requestA);
        sendRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var connDto = await sendRes.Content.ReadFromJsonAsync<ConnectionDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        connDto.Should().NotBeNull();
        connDto!.Status.Should().Be("Pending");
        connDto.RequesterUserId.Should().Be(userAId);
        connDto.AddresseeUserId.Should().Be(userBId);

        // Check incoming on B
        using var incomingReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/connections/requests/incoming");
        incomingReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var incomingRes = await _client.SendAsync(incomingReq);
        incomingRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var incomingList = await incomingRes.Content.ReadFromJsonAsync<List<ConnectionDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        incomingList.Should().Contain(c => c.Id == connDto.Id);

        // B accepts
        using var acceptReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/connections/requests/{connDto.Id}/accept");
        acceptReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var acceptRes = await _client.SendAsync(acceptReq);
        acceptRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check connection status from A's perspective
        using var statusReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/connections/status/{userBId}");
        statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var statusRes = await _client.SendAsync(statusReq);
        statusRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusDto = await statusRes.Content.ReadFromJsonAsync<ConnectionStatusDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        statusDto!.Status.Should().Be("Accepted");

        // A removes connection
        using var removeReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/connections/{userBId}");
        removeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var removeRes = await _client.SendAsync(removeReq);
        removeRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Status should now be None
        using var statusReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/connections/status/{userBId}");
        statusReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var statusRes2 = await _client.SendAsync(statusReq2);
        var statusDto2 = await statusRes2.Content.ReadFromJsonAsync<ConnectionStatusDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        statusDto2!.Status.Should().Be("None");
    }

    [Fact]
    public async Task Follow_Lifecycle_WorksCorrectly()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("folA");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("folB");

        // A follows B
        using var followReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userBId}/follow");
        followReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var followRes = await _client.SendAsync(followReq);
        followRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check follow status from A
        using var statusReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{userBId}/follow-status");
        statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var statusRes = await _client.SendAsync(statusReq);
        statusRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusDto = await statusRes.Content.ReadFromJsonAsync<FollowStatusDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        statusDto!.IsFollowing.Should().BeTrue();
        statusDto.FollowersCount.Should().BeGreaterOrEqualTo(1);

        // A unfollows B
        using var unfollowReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/{userBId}/follow");
        unfollowReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var unfollowRes = await _client.SendAsync(unfollowReq);
        unfollowRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Check follow status after unfollow
        using var statusReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{userBId}/follow-status");
        statusReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var statusRes2 = await _client.SendAsync(statusReq2);
        var statusDto2 = await statusRes2.Content.ReadFromJsonAsync<FollowStatusDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        statusDto2!.IsFollowing.Should().BeFalse();
    }

    [Fact]
    public async Task BlockUser_AtomicallySeversConnectionAndFollowsBilateral()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("blkA");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("blkB");

        // 1. Establish connection A -> B
        using var sendConn = new HttpRequestMessage(HttpMethod.Post, "/api/v1/connections/requests");
        sendConn.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        sendConn.Content = JsonContent.Create(new SendConnectionRequest(userBId));
        var sendRes = await _client.SendAsync(sendConn);
        var connDto = await sendRes.Content.ReadFromJsonAsync<ConnectionDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // B accepts
        using var acceptConn = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/connections/requests/{connDto!.Id}/accept");
        acceptConn.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.SendAsync(acceptConn);

        // 2. Establish bilateral follows: A follows B, B follows A
        using var folA = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userBId}/follow");
        folA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await _client.SendAsync(folA);

        using var folB = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userAId}/follow");
        folB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.SendAsync(folB);

        // 3. A blocks B
        using var blockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userBId}/block");
        blockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        blockReq.Content = JsonContent.Create(new BlockUserRequest("Inappropriate behavior"));
        var blockRes = await _client.SendAsync(blockReq);
        blockRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify connection severed (Status is Blocked)
        using var statusConnReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/connections/status/{userBId}");
        statusConnReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var statusConnRes = await _client.SendAsync(statusConnReq);
        var statusConnDto = await statusConnRes.Content.ReadFromJsonAsync<ConnectionStatusDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        statusConnDto!.Status.Should().Be("Blocked");

        // 5. Verify bilateral follows severed
        using var folStatusA = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{userBId}/follow-status");
        folStatusA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var folResA = await _client.SendAsync(folStatusA);
        var folDtoA = await folResA.Content.ReadFromJsonAsync<FollowStatusDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        folDtoA!.IsFollowing.Should().BeFalse();
        folDtoA.IsFollowedBy.Should().BeFalse();

        // 6. Verify B cannot send connection request to A
        using var blockConnAttempt = new HttpRequestMessage(HttpMethod.Post, "/api/v1/connections/requests");
        blockConnAttempt.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        blockConnAttempt.Content = JsonContent.Create(new SendConnectionRequest(userAId));
        var blockedAttemptRes = await _client.SendAsync(blockConnAttempt);
        blockedAttemptRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MilestoneB_AcademicProfile_RemainsFullyFunctional()
    {
        var (token, userId, _) = await CreateAndAuthenticateUserAsync("profUser");

        // 1. Get profile
        using var getProfReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        getProfReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getProfRes = await _client.SendAsync(getProfReq);
        getProfRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await getProfRes.Content.ReadFromJsonAsync<UserProfileDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        profile.Should().NotBeNull();
        profile!.DisplayName.Should().Be("User profUser");

        // 2. Update profile
        var updateReq = new UpdateProfileRequest(
            "Dr. Updated Scholar",
            "Senior AI Researcher",
            "Passionate about distributed AI and academic collaboration.",
            "US",
            "Cambridge",
            "en",
            "Public"
        );

        using var putProfReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me");
        putProfReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        putProfReq.Content = JsonContent.Create(updateReq);
        var putProfRes = await _client.SendAsync(putProfReq);
        putProfRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await putProfRes.Content.ReadFromJsonAsync<UserProfileDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        updated!.DisplayName.Should().Be("Dr. Updated Scholar");
        updated.Headline.Should().Be("Senior AI Researcher");
        updated.Bio.Should().Be("Passionate about distributed AI and academic collaboration.");
    }
}

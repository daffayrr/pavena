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

public class SocialConnectionsApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly PavenaApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SocialConnectionsApiIntegrationTests(PavenaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Username, string DisplayName)> CreateAndAuthenticateUserAsync(string prefix)
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@pavena.test";
        var username = $"{prefix}_{Guid.NewGuid():N}"[..20];
        var displayName = $"Dr. {prefix}";
        var password = "SecurePassword123!";

        var registerReq = new RegisterRequest(email, username, password, displayName);
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
        return (loginData!.AccessToken, loginData.UserId, username, displayName);
    }

    [Fact]
    public async Task DiscoverResearchers_ReturnsPublicResearchers_FilteredByQuery()
    {
        var (tokenA, userIdA, _, _) = await CreateAndAuthenticateUserAsync("discA");
        var (tokenB, userIdB, usernameB, displayNameB) = await CreateAndAuthenticateUserAsync("discB");

        // User B updates headline
        using var updateMsg = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequest(displayNameB, "Quantum Computing Specialist", "Quantum optics researcher", "ID", "Jakarta", "id", "Public"))
        };
        updateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var updateRes = await _client.SendAsync(updateMsg);
        updateRes.EnsureSuccessStatusCode();

        // User A searches for "Quantum"
        using var searchMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/discover?q=Quantum");
        searchMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var searchRes = await _client.SendAsync(searchMsg);
        searchRes.EnsureSuccessStatusCode();

        var results = await searchRes.Content.ReadFromJsonAsync<List<ResearcherDiscoveryDto>>(JsonOptions);
        results.Should().NotBeNull();
        results!.Should().Contain(r => r.UserId == userIdB);

        var match = results!.First(r => r.UserId == userIdB);
        match.DisplayName.Should().Be(displayNameB);
        match.Headline.Should().Contain("Quantum");
        match.ConnectionStatus.Should().Be("None");
        match.IsFollowing.Should().BeFalse();

        // User A should not appear in their own search
        results.Should().NotContain(r => r.UserId == userIdA);
    }

    [Fact]
    public async Task DiscoverResearchers_ExcludesBlockedUsers()
    {
        var (tokenA, _, _, _) = await CreateAndAuthenticateUserAsync("blkA");
        var (_, userIdB, _, displayNameB) = await CreateAndAuthenticateUserAsync("blkB");

        // User A blocks User B
        using var blockMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userIdB}/block")
        {
            Content = JsonContent.Create(new BlockUserRequest("Spam behavior"))
        };
        blockMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var blockRes = await _client.SendAsync(blockMsg);
        blockRes.EnsureSuccessStatusCode();

        // User A queries discover
        using var searchMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/discover?q={displayNameB}");
        searchMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var searchRes = await _client.SendAsync(searchMsg);
        searchRes.EnsureSuccessStatusCode();

        var results = await searchRes.Content.ReadFromJsonAsync<List<ResearcherDiscoveryDto>>(JsonOptions);
        results.Should().NotBeNull();
        results!.Should().NotContain(r => r.UserId == userIdB);
    }

    [Fact]
    public async Task Connections_IncomingAndOutgoing_ReturnsEnrichedRequesterDetails()
    {
        var (tokenA, userIdA, _, displayNameA) = await CreateAndAuthenticateUserAsync("reqA");
        var (tokenB, userIdB, _, displayNameB) = await CreateAndAuthenticateUserAsync("reqB");

        // User A sends connection request to User B
        using var sendMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/connections/requests")
        {
            Content = JsonContent.Create(new SendConnectionRequest(userIdB))
        };
        sendMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var sendRes = await _client.SendAsync(sendMsg);
        sendRes.EnsureSuccessStatusCode();

        // User B checks incoming requests
        using var incMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/connections/requests/incoming");
        incMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var incRes = await _client.SendAsync(incMsg);
        incRes.EnsureSuccessStatusCode();

        var incoming = await incRes.Content.ReadFromJsonAsync<List<ConnectionDto>>(JsonOptions);
        incoming.Should().NotBeNull();
        incoming!.Should().Contain(r => r.RequesterUserId == userIdA);
        var reqItem = incoming!.First(r => r.RequesterUserId == userIdA);
        reqItem.RequesterDisplayName.Should().Be(displayNameA);

        // User A checks outgoing requests
        using var outMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/connections/requests/outgoing");
        outMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var outRes = await _client.SendAsync(outMsg);
        outRes.EnsureSuccessStatusCode();

        var outgoing = await outRes.Content.ReadFromJsonAsync<List<ConnectionDto>>(JsonOptions);
        outgoing.Should().NotBeNull();
        outgoing!.Should().Contain(r => r.AddresseeUserId == userIdB);
        var outItem = outgoing!.First(r => r.AddresseeUserId == userIdB);
        outItem.AddresseeDisplayName.Should().Be(displayNameB);

        // User B accepts request
        using var acceptMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/connections/requests/{reqItem.Id}/accept");
        acceptMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var acceptRes = await _client.SendAsync(acceptMsg);
        acceptRes.EnsureSuccessStatusCode();

        // User A checks accepted connections
        using var connsMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/connections");
        connsMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var connsRes = await _client.SendAsync(connsMsg);
        connsRes.EnsureSuccessStatusCode();

        var connections = await connsRes.Content.ReadFromJsonAsync<List<ConnectionUserDto>>(JsonOptions);
        connections.Should().NotBeNull();
        connections!.Should().Contain(c => c.UserId == userIdB);
        var connItem = connections!.First(c => c.UserId == userIdB);
        connItem.DisplayName.Should().Be(displayNameB);
    }
}

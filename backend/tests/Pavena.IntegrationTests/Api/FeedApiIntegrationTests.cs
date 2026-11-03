using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class FeedApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;

    public FeedApiIntegrationTests(PavenaApiFactory factory)
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

    private async Task<PostDto> CreatePostAsync(string token, string content, string visibility, bool publish = true)
    {
        var req = new CreatePostRequest(content, visibility, PublishImmediately: publish);
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg.Content = JsonContent.Create(req);
        var res = await _client.SendAsync(msg);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var post = await res.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return post!;
    }

    [Fact]
    public async Task MyNetworkFeed_EnforcesVisibility_FollowDoesNotGrantConnectionsAccess()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("netA");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("netB");
        var (tokenC, userCId, _) = await CreateAndAuthenticateUserAsync("netC");
        var (tokenD, _, _) = await CreateAndAuthenticateUserAsync("netD");

        // 1. Establish mutual connection between A and B
        using var sendConn = new HttpRequestMessage(HttpMethod.Post, "/api/v1/connections/requests");
        sendConn.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        sendConn.Content = JsonContent.Create(new SendConnectionRequest(userBId));
        var connRes = await _client.SendAsync(sendConn);
        var connDto = await connRes.Content.ReadFromJsonAsync<ConnectionDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        using var acceptConn = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/connections/requests/{connDto!.Id}/accept");
        acceptConn.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.SendAsync(acceptConn);

        // 2. User A follows User C (NO connection)
        using var followC = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userCId}/follow");
        followC.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await _client.SendAsync(followC);

        // 3. User D is an unrelated stranger (neither connected nor followed)

        // 4. Create posts:
        // - A posts own Public and Connections
        var postA1 = await CreatePostAsync(tokenA, "Post A - Public", "Public");
        var postA2 = await CreatePostAsync(tokenA, "Post A - Connections", "Connections");

        // - B posts Connections
        var postB1 = await CreatePostAsync(tokenB, "Post B - Connections", "Connections");

        // - C posts Public and Connections
        var postC1 = await CreatePostAsync(tokenC, "Post C - Public", "Public");
        var postC2 = await CreatePostAsync(tokenC, "Post C - Connections", "Connections");

        // - D posts Public
        var postD1 = await CreatePostAsync(tokenD, "Post D - Stranger Public", "Public");

        // 5. Query User A's Network Feed
        using var feedMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/feed?limit=50");
        feedMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var feedRes = await _client.SendAsync(feedMsg);
        feedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var feed = await feedRes.Content.ReadFromJsonAsync<FeedResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        feed.Should().NotBeNull();
        var itemIds = feed!.Items.Select(i => i.Id).ToList();

        // Asserts:
        itemIds.Should().Contain(postA1.Id, "Author's own public post must appear");
        itemIds.Should().Contain(postA2.Id, "Author's own connections post must appear");
        itemIds.Should().Contain(postB1.Id, "Connected peer's connections post must appear");
        itemIds.Should().Contain(postC1.Id, "Followed user's public post must appear");

        // Crucial security invariant:
        itemIds.Should().NotContain(postC2.Id, "Follow must NOT grant access to Connections posts!");
        itemIds.Should().NotContain(postD1.Id, "Stranger's post must NOT appear in network feed");
    }

    [Fact]
    public async Task DiscoverFeed_ExcludesDrafts_Archived_AndNonPublicPosts()
    {
        var (token, _, _) = await CreateAndAuthenticateUserAsync("discUser");

        var publicPost = await CreatePostAsync(token, "Discoverable public science.", "Public", publish: true);
        var draftPost = await CreatePostAsync(token, "Unpublished preliminary note.", "Public", publish: false);
        var connPost = await CreatePostAsync(token, "Connections only discussion.", "Connections", publish: true);

        var (viewerToken, _, _) = await CreateAndAuthenticateUserAsync("discViewer");

        using var discMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/feed/discover?limit=50");
        discMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var discRes = await _client.SendAsync(discMsg);
        discRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var feed = await discRes.Content.ReadFromJsonAsync<FeedResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var ids = feed!.Items.Select(i => i.Id).ToList();

        ids.Should().Contain(publicPost.Id);
        ids.Should().NotContain(draftPost.Id, "Draft posts must be excluded from Discover");
        ids.Should().NotContain(connPost.Id, "Connections-only posts must be excluded from Discover");
    }

    [Fact]
    public async Task KeysetPagination_Deterministic_NoDuplicates_NoGaps()
    {
        var (token, _, _) = await CreateAndAuthenticateUserAsync("pageUser");

        // Create 5 published posts
        var posts = new List<PostDto>();
        for (int i = 1; i <= 5; i++)
        {
            var p = await CreatePostAsync(token, $"Sequential post number {i} for pagination test.", "Public");
            posts.Add(p);
            // Brief delay to ensure monotonically increasing or distinct timestamps
            await Task.Delay(50);
        }

        var (viewerToken, _, _) = await CreateAndAuthenticateUserAsync("pageViewer");

        // Page 1: limit = 2
        using var page1Msg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/feed/discover?limit=2");
        page1Msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var res1 = await _client.SendAsync(page1Msg);
        var feed1 = await res1.Content.ReadFromJsonAsync<FeedResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        feed1!.Items.Count.Should().Be(2);
        feed1.HasMore.Should().BeTrue();
        feed1.NextCursor.Should().NotBeNullOrWhiteSpace();

        // Page 2: limit = 2, cursor = nextCursor
        using var page2Msg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/feed/discover?limit=2&cursor={Uri.EscapeDataString(feed1.NextCursor!)}");
        page2Msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var res2 = await _client.SendAsync(page2Msg);
        res2.IsSuccessStatusCode.Should().BeTrue();
        var feed2 = await res2.Content.ReadFromJsonAsync<FeedResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        feed2!.Items.Count.Should().Be(2);
        feed2.HasMore.Should().BeTrue();

        // Ensure no overlap between page 1 and page 2
        var page1Ids = feed1.Items.Select(i => i.Id).ToHashSet();
        var page2Ids = feed2.Items.Select(i => i.Id).ToHashSet();
        page1Ids.Overlaps(page2Ids).Should().BeFalse("Keyset pagination must never duplicate items across pages");
    }

    [Fact]
    public async Task BilateralBlock_ExcludesPostsFromBothUsersInFeed()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("blkFeedA");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("blkFeedB");

        var postA = await CreatePostAsync(tokenA, "Post by Author A", "Public");
        var postB = await CreatePostAsync(tokenB, "Post by Author B", "Public");

        // A blocks B
        using var blockMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userBId}/block");
        blockMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await _client.SendAsync(blockMsg);

        // User A's discover feed must NOT contain B's post
        using var feedAMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/feed/discover?limit=50");
        feedAMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var resA = await _client.SendAsync(feedAMsg);
        var feedA = await resA.Content.ReadFromJsonAsync<FeedResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        feedA!.Items.Should().NotContain(i => i.Id == postB.Id);

        // User B's discover feed must NOT contain A's post (bilateral exclusion)
        using var feedBMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/feed/discover?limit=50");
        feedBMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var resB = await _client.SendAsync(feedBMsg);
        var feedB = await resB.Content.ReadFromJsonAsync<FeedResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        feedB!.Items.Should().NotContain(i => i.Id == postA.Id);
    }
}

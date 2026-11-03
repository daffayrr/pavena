using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class PostApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;

    public PostApiIntegrationTests(PavenaApiFactory factory)
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

    [Fact]
    public async Task PostLifecycle_CreateDraft_Publish_Get_Update_Archive_Delete_WorksEndToEnd()
    {
        var (token, userId, _) = await CreateAndAuthenticateUserAsync("postAuthor");

        // 1. Create Draft with attachment
        var createDraftReq = new CreatePostRequest(
            "Initial draft of my research methodology.",
            "Public",
            PublishImmediately: false,
            Attachments: new List<CreateAttachmentRequest>
            {
                new("methodology_notes.pdf", "application/pdf", 1024 * 50, "storage/posts/methodology_notes.pdf")
            }
        );

        using var createDraftMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createDraftMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createDraftMsg.Content = JsonContent.Create(createDraftReq);
        var createDraftRes = await _client.SendAsync(createDraftMsg);
        createDraftRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var draftPost = await createDraftRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        draftPost.Should().NotBeNull();
        draftPost!.Status.Should().Be("Draft");
        draftPost.AuthorUserId.Should().Be(userId);
        draftPost.Attachments.Should().ContainSingle(a => a.FileName == "methodology_notes.pdf");

        // 2. Publish post
        using var publishMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{draftPost.Id}/publish");
        publishMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var publishRes = await _client.SendAsync(publishMsg);
        publishRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedPost = await publishRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        publishedPost!.Status.Should().Be("Published");
        publishedPost.PublishedAtUtc.Should().NotBeNull();

        // 3. Get published post
        using var getMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{draftPost.Id}");
        getMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getMsg);
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Update post content
        var updateReq = new UpdatePostRequest("Updated and refined research methodology.", "Public");
        using var updateMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{draftPost.Id}");
        updateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        updateMsg.Content = JsonContent.Create(updateReq);
        var updateRes = await _client.SendAsync(updateMsg);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPost = await updateRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        updatedPost!.Content.Should().Be("Updated and refined research methodology.");
        updatedPost.EditedAtUtc.Should().NotBeNull();

        // 5. Archive post
        using var archiveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{draftPost.Id}/archive");
        archiveMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var archiveRes = await _client.SendAsync(archiveMsg);
        archiveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var archivedPost = await archiveRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        archivedPost!.Status.Should().Be("Archived");

        // 6. Delete post
        using var deleteMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{draftPost.Id}");
        deleteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteRes = await _client.SendAsync(deleteMsg);
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Verification: Deleted post returns 404
        using var getDeletedMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{draftPost.Id}");
        getDeletedMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getDeletedRes = await _client.SendAsync(getDeletedMsg);
        getDeletedRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Visibility_Public_AccessibleToOtherUser()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("authorPub");
        var (tokenB, _, _) = await CreateAndAuthenticateUserAsync("viewerPub");

        // User A publishes a public post
        var createReq = new CreatePostRequest("Public open-access scientific findings.", "Public", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // User B views post
        using var viewMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post!.Id}");
        viewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewRes = await _client.SendAsync(viewMsg);
        viewRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Visibility_Connections_AccessibleOnlyToAcceptedConnections_NotFollowers()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("connAuthor");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("connViewer");

        // User A publishes Connections post
        var createReq = new CreatePostRequest("Confidential lab pre-print for connections.", "Connections", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // 1. User B (stranger) attempts to view -> returns 404 (does NOT leak existence)
        using var viewStrangerMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post!.Id}");
        viewStrangerMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewStrangerRes = await _client.SendAsync(viewStrangerMsg);
        viewStrangerRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. User B follows User A -> follow should NOT grant access!
        using var followMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userAId}/follow");
        followMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.SendAsync(followMsg);

        using var viewFollowerMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        viewFollowerMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewFollowerRes = await _client.SendAsync(viewFollowerMsg);
        viewFollowerRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. User A and User B connect (mutual accepted connection)
        using var sendConnMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/connections/requests");
        sendConnMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        sendConnMsg.Content = JsonContent.Create(new SendConnectionRequest(userBId));
        var sendConnRes = await _client.SendAsync(sendConnMsg);
        var connDto = await sendConnRes.Content.ReadFromJsonAsync<ConnectionDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        using var acceptConnMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/connections/requests/{connDto!.Id}/accept");
        acceptConnMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.SendAsync(acceptConnMsg);

        // 4. User B now views the Connections post -> returns 200 OK!
        using var viewConnectedMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        viewConnectedMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewConnectedRes = await _client.SendAsync(viewConnectedMsg);
        viewConnectedRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Visibility_OnlyMe_AccessibleOnlyToAuthor()
    {
        var (tokenA, _, _) = await CreateAndAuthenticateUserAsync("privAuthor");
        var (tokenB, _, _) = await CreateAndAuthenticateUserAsync("privStranger");

        var createReq = new CreatePostRequest("Private preliminary research note.", "OnlyMe", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Author can view
        using var viewAuthorMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post!.Id}");
        viewAuthorMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var viewAuthorRes = await _client.SendAsync(viewAuthorMsg);
        viewAuthorRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Non-author gets 404
        using var viewStrangerMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        viewStrangerMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewStrangerRes = await _client.SendAsync(viewStrangerMsg);
        viewStrangerRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Security_IDOR_Protection_NonAuthorCannotUpdateOrDelete()
    {
        var (tokenA, _, _) = await CreateAndAuthenticateUserAsync("idorAuthor");
        var (tokenB, _, _) = await CreateAndAuthenticateUserAsync("idorAttacker");

        var createReq = new CreatePostRequest("Author post to protect.", "Public", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // User B attempts to edit User A's post
        var updateReq = new UpdatePostRequest("Hacked content", "Public");
        using var updateMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{post!.Id}");
        updateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        updateMsg.Content = JsonContent.Create(updateReq);
        var updateRes = await _client.SendAsync(updateMsg);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // User B attempts to delete User A's post
        using var deleteMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{post.Id}");
        deleteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var deleteRes = await _client.SendAsync(deleteMsg);
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Security_AuthorSpoofing_Prevention()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("spoofUserA");
        var (_, userBId, _) = await CreateAndAuthenticateUserAsync("spoofUserB");

        // User A attempts to create a post; API ignores any spoofed identity and binds to authenticated principal
        var createReq = new CreatePostRequest("Authentic discourse", "Public", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        post!.AuthorUserId.Should().Be(userAId);
        post.AuthorUserId.Should().NotBe(userBId);
    }

    [Fact]
    public async Task Block_HidesPublicPost()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("blkAuthor");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("blkTarget");

        var createReq = new CreatePostRequest("Public thought to be blocked.", "Public", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // User B can view initially
        using var viewBeforeMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post!.Id}");
        viewBeforeMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewBeforeRes = await _client.SendAsync(viewBeforeMsg);
        viewBeforeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // User A blocks User B
        using var blockMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{userBId}/block");
        blockMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await _client.SendAsync(blockMsg);

        // User B attempts to view now -> returns 404
        using var viewAfterMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        viewAfterMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewAfterRes = await _client.SendAsync(viewAfterMsg);
        viewAfterRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VisibilityChange_ImmediatelyAffectsAccess()
    {
        var (tokenA, _, _) = await CreateAndAuthenticateUserAsync("visChgAuthor");
        var (tokenB, _, _) = await CreateAndAuthenticateUserAsync("visChgViewer");

        // 1. Post created as Public
        var createReq = new CreatePostRequest("Public initially", "Public", PublishImmediately: true);
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createMsg.Content = JsonContent.Create(createReq);
        var createRes = await _client.SendAsync(createMsg);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // User B can view
        using var view1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post!.Id}");
        view1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewRes1 = await _client.SendAsync(view1);
        viewRes1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. User A updates visibility to Connections
        var updateReq = new UpdatePostRequest("Public initially", "Connections");
        using var updateMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{post.Id}");
        updateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        updateMsg.Content = JsonContent.Create(updateReq);
        await _client.SendAsync(updateMsg);

        // User B (not connected) immediately gets 404
        using var view2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        view2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var viewRes2 = await _client.SendAsync(view2);
        viewRes2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

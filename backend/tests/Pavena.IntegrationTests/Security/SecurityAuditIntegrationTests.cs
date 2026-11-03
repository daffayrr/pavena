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

namespace Pavena.IntegrationTests.Security;

public class SecurityAuditIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;

    public SecurityAuditIntegrationTests(PavenaApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId)> AuthenticateUserAsync(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{suffix}@pavena.org";
        var username = $"{prefix}_{suffix}";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, username, password, $"Dr. {prefix}", "US");
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginReq = new LoginRequest(email, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var authBody = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (authBody!.AccessToken, authBody.UserId);
    }

    [Fact]
    public async Task HealthChecks_AndProbes_ReturnHealthy()
    {
        var resHealth = await _client.GetAsync("/health");
        resHealth.StatusCode.Should().Be(HttpStatusCode.OK);

        var resReady = await _client.GetAsync("/health/ready");
        resReady.StatusCode.Should().Be(HttpStatusCode.OK);

        var resLive = await _client.GetAsync("/health/live");
        resLive.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecurityHeaders_AreEnforcedAcrossResponses()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");

        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");

        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");

        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task BrokenAccessControl_IDOR_CrossUserPostMutation_IsPrevented()
    {
        var (authorToken, authorId) = await AuthenticateUserAsync("secAuthor");
        var (attackerToken, attackerId) = await AuthenticateUserAsync("secAttacker");

        // 1. Author creates a post
        var createReq = new CreatePostRequest(
            "Original confidential research findings.",
            "Public",
            PublishImmediately: true,
            Category: "ResearchFinding"
        );

        using var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
        {
            Content = JsonContent.Create(createReq)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var postRes = await _client.SendAsync(postMsg);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var post = await postRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        post.Should().NotBeNull();

        // 2. Attacker attempts to update Author's post (IDOR attack)
        var maliciousUpdate = new UpdatePostRequest("Hacked content by attacker!", "Public", "ResearchFinding");
        using var attackUpdateMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{post!.Id}")
        {
            Content = JsonContent.Create(maliciousUpdate)
        };
        attackUpdateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var attackUpdateRes = await _client.SendAsync(attackUpdateMsg);
        // Server returns 404 (IDOR mask) or 403, never 200 OK
        attackUpdateRes.IsSuccessStatusCode.Should().BeFalse();

        // 3. Attacker attempts to delete Author's post
        using var attackDeleteMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{post.Id}");
        attackDeleteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var attackDeleteRes = await _client.SendAsync(attackDeleteMsg);
        attackDeleteRes.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task PrivateAttachment_Download_EnforcesStrictServerAuthorization()
    {
        var (authorToken, authorId) = await AuthenticateUserAsync("privAuthor");
        var (otherToken, otherId) = await AuthenticateUserAsync("privOther");

        // 1. Author creates a private post (OnlyMe) with an attachment
        var createReq = new CreatePostRequest(
            "Confidential lab notebook entries.",
            "OnlyMe",
            PublishImmediately: true,
            Attachments: new List<CreateAttachmentRequest>
            {
                new("lab_notes.pdf", "application/pdf", 1024 * 10, "storage/posts/author/lab_notes.pdf")
            },
            Category: "ResearchFinding"
        );

        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
        {
            Content = JsonContent.Create(createReq)
        };
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var post = await createRes.Content.ReadFromJsonAsync<PostDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        post.Should().NotBeNull();
        var attachment = post!.Attachments.First();

        // 2. Other user tries to get download URL for the private attachment
        using var unauthorizedDownloadMsg = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/posts/{post.Id}/attachments/{attachment.Id}/download");
        unauthorizedDownloadMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var unauthorizedRes = await _client.SendAsync(unauthorizedDownloadMsg);
        unauthorizedRes.StatusCode.Should().Be(HttpStatusCode.NotFound); // ResourceAuthorization masks private content

        // 3. Author requests download URL -> succeeds with signed ticket
        using var authorizedDownloadMsg = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/posts/{post.Id}/attachments/{attachment.Id}/download");
        authorizedDownloadMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var authorizedRes = await _client.SendAsync(authorizedDownloadMsg);
        authorizedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var downloadPayload = await authorizedRes.Content.ReadFromJsonAsync<JsonElement>();
        var downloadUrl = downloadPayload.GetProperty("downloadUrl").GetString();
        downloadUrl.Should().NotBeNullOrWhiteSpace();
        downloadUrl.Should().Contain("/api/v1/storage/download?key=");
        downloadUrl.Should().Contain("&sig=");
    }

    [Fact]
    public async Task MaliciousUpload_WithExecutableSignature_IsRejectedByMalwareScanner()
    {
        var (token, userId) = await AuthenticateUserAsync("scannerTest");

        // 1. Obtain upload ticket
        var ticketReq = new UploadTicketRequest("paper.pdf", "application/pdf", 1024);
        using var ticketMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/storage/upload-ticket")
        {
            Content = JsonContent.Create(ticketReq)
        };
        ticketMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ticketRes = await _client.SendAsync(ticketMsg);
        ticketRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticket = await ticketRes.Content.ReadFromJsonAsync<UploadTicketResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // 2. Craft malicious payload containing MZ binary executable header inside fake PDF
        using var formContent = new MultipartFormDataContent();
        var maliciousBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 }; // DOS MZ header
        var byteContent = new ByteArrayContent(maliciousBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formContent.Add(byteContent, "file", "paper.pdf");

        var uploadRes = await _client.PostAsync(ticket!.UploadUrl, formContent);
        uploadRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorBody = await uploadRes.Content.ReadAsStringAsync();
        errorBody.Should().Contain("ExecutableBinarySignature_MZ");
    }
}

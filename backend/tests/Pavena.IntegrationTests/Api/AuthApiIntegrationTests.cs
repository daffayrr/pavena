using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pavena.Application.Identity.DTOs;
using Pavena.Infrastructure.Persistence;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class PavenaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Port=3306;Database=pavena_test_db;User=root;Password=abc_123;CharSet=utf8mb4;",
                ["DatabaseProvider"] = "MySQL",
                ["SUPERADMIN_EMAIL"] = "admin_test@pavena.org",
                ["SUPERADMIN_PASSWORD"] = "SuperAdmin#Test2026"
            };
            config.AddInMemoryCollection(testConfig);
        });
    }
}

public class AuthApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly PavenaApiFactory _factory;

    public AuthApiIntegrationTests(PavenaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterRequest(
            $"scholar_{suffix}@pavena.org",
            $"scholar_{suffix}",
            "StrongPass#2026",
            "Dr. Test Scholar",
            "US"
        );

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be(request.Email);
        body.Username.Should().Be(request.Username);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterRequest(
            $"dup_{suffix}@pavena.org",
            $"dup_{suffix}",
            "StrongPass#2026",
            "Duplicate User"
        );

        var firstResp = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        firstResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Attempt duplicate
        var secondResp = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        secondResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessTokenAndRefreshToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@pavena.org";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, $"login_{suffix}", password, "Login Tester");
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        var loginReq = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithConsecutiveFailedAttempts_TriggersAccountLockout()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"lockout_{suffix}@pavena.org";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, $"lockout_{suffix}", password, "Lockout Tester");
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        // Submit 5 consecutive invalid passwords
        for (int i = 0; i < 5; i++)
        {
            var failReq = new LoginRequest(email, "WrongPassword#123");
            var failResp = await _client.PostAsJsonAsync("/api/v1/auth/login", failReq);
            failResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // 6th attempt with even correct password should be rejected due to lockout
        var lockedReq = new LoginRequest(email, password);
        var lockedResp = await _client.PostAsJsonAsync("/api/v1/auth/login", lockedReq);

        lockedResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await lockedResp.Content.ReadAsStringAsync();
        content.Should().Contain("locked due to multiple failed login attempts");
    }

    [Fact]
    public async Task AdminUsersEndpoint_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminUsersEndpoint_StandardUser_ReturnsForbidden()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"standard_{suffix}@pavena.org";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, $"std_{suffix}", password, "Standard User");
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        var loginReq = new LoginRequest(email, password);
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminUsersEndpoint_Superadmin_ReturnsOkWithUsersList()
    {
        // Login as seeded Superadmin
        var loginReq = new LoginRequest("admin_test@pavena.org", "SuperAdmin#Test2026");
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
    }
}

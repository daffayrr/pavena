using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Pavena.Web.Services.Auth;

public interface IScholarSessionService
{
    Guid CurrentUserId { get; }
    string CurrentUsername { get; }
    string CurrentDisplayName { get; }
    string CurrentEmail { get; }
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
    event Action? OnSessionChanged;
    void SetSession(Guid userId, string username, string displayName, string token);
    void SetSession(Guid userId, string username, string displayName, string token, string email);
    void ClearSession();
    string GenerateDevToken(Guid userId, string username, string email, string role = "PlatformAdmin");
}

public class ScholarSessionService : IScholarSessionService
{
    private readonly IConfiguration? _configuration;

    // Default scholar for development and UI inspection (Dr. Marie Curie)
    public Guid CurrentUserId { get; private set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string CurrentUsername { get; private set; } = "marie_curie";
    public string CurrentDisplayName { get; private set; } = "Dr. Marie Curie";
    public string CurrentEmail { get; private set; } = "marie.curie@pavena.local";
    public string? AccessToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) || CurrentUserId != Guid.Empty;

    public event Action? OnSessionChanged;

    public ScholarSessionService(IConfiguration? configuration = null)
    {
        _configuration = configuration;

        var env = _configuration?["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        // In non-production environments, bootstrap a signed development JWT
        // so that calls to protected API endpoints (/api/v1/feed, /api/v1/posts/mine, etc.) succeed seamlessly.
        if (!string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            AccessToken = GenerateDevToken(CurrentUserId, CurrentUsername, CurrentEmail);
        }
    }

    public void SetSession(Guid userId, string username, string displayName, string token)
    {
        SetSession(userId, username, displayName, token, $"{username}@pavena.local");
    }

    public void SetSession(Guid userId, string username, string displayName, string token, string email)
    {
        CurrentUserId = userId;
        CurrentUsername = username;
        CurrentDisplayName = displayName;
        CurrentEmail = email;
        AccessToken = token;
        OnSessionChanged?.Invoke();
    }

    public void ClearSession()
    {
        CurrentUserId = Guid.Empty;
        CurrentUsername = string.Empty;
        CurrentDisplayName = string.Empty;
        CurrentEmail = string.Empty;
        AccessToken = null;
        OnSessionChanged?.Invoke();
    }

    public string GenerateDevToken(Guid userId, string username, string email, string role = "PlatformAdmin")
    {
        var secret = _configuration?["Jwt:Secret"] ?? "pavena_super_secret_dev_key_must_be_at_least_32_characters_long_123456";
        var issuer = _configuration?["Jwt:Issuer"] ?? "Pavena";
        var audience = _configuration?["Jwt:Audience"] ?? "Pavena";

        var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = now + 86400 * 30; // 30 days validity for development sessions

        var payloadObj = new Dictionary<string, object>
        {
            ["sub"] = userId.ToString(),
            ["email"] = email,
            ["unique_name"] = username,
            ["nameid"] = userId.ToString(),
            ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] = userId.ToString(),
            ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] = role,
            ["role"] = role,
            ["jti"] = Guid.NewGuid().ToString(),
            ["iss"] = issuer,
            ["aud"] = audience,
            ["nbf"] = now,
            ["exp"] = exp,
            ["iat"] = now
        };

        var payload = JsonSerializer.Serialize(payloadObj);
        var encHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var encPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var stringToSign = $"{encHeader}.{encPayload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        return $"{stringToSign}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

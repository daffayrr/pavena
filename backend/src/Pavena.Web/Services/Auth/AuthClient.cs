using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pavena.Application.Identity.DTOs;

namespace Pavena.Web.Services.Auth;

public class AuthClient : IAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly IScholarSessionService _sessionService;
    private readonly ILogger<AuthClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthClient(
        HttpClient httpClient,
        IScholarSessionService sessionService,
        ILogger<AuthClient>? logger = null)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions, cancellationToken);
                if (result != null)
                {
                    _sessionService.SetSession(result.UserId, result.Username, result.Username, result.AccessToken, result.Email);
                    _logger?.LogInformation("User {Username} logged in successfully", result.Username);
                    return result;
                }
            }

            _logger?.LogWarning("Login failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred during login request");
            return null;
        }
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/register", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RegisterResponse>(_jsonOptions, cancellationToken);
            }

            _logger?.LogWarning("Registration failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred during registration request");
            return null;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsync("api/v1/auth/logout", null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Logout API call returned error");
        }
        finally
        {
            _sessionService.ClearSession();
        }
    }
}

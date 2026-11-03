using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;
using Pavena.Web.Services.Auth;

namespace Pavena.Web.Services.Profile;

public class AcademicProfileClient : IAcademicProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly IScholarSessionService _sessionService;
    private readonly ILogger<AcademicProfileClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Fallback in-memory state for standalone / offline UI preview
    private AcademicProfileDto? _previewProfile;
    private readonly List<ResearcherDiscoveryDto> _previewDiscoveries = new();
    private readonly List<ConnectionUserDto> _previewConnections = new();
    private readonly List<ConnectionDto> _previewIncomingRequests = new();
    private readonly List<ConnectionDto> _previewOutgoingRequests = new();
    private readonly List<FollowUserDto> _previewFollowing = new();
    private readonly List<FollowUserDto> _previewFollowers = new();
    private readonly List<BlockedUserDto> _previewBlocked = new();

    public AcademicProfileClient(
        HttpClient httpClient,
        IScholarSessionService sessionService,
        ILogger<AcademicProfileClient>? logger = null)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        InitPreviewData();
    }

    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(_sessionService.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _sessionService.AccessToken);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<AcademicProfileDto?> GetMyAcademicProfileAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/users/me/academic-profile", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AcademicProfileDto>(_jsonOptions, cancellationToken);
            }

            _logger?.LogWarning("GetMyAcademicProfile returned {StatusCode}", response.StatusCode);
            return _previewProfile;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetMyAcademicProfile. Falling back to preview state.");
            return _previewProfile;
        }
    }

    public async Task<AcademicProfileDto?> GetAcademicProfileAsync(string identifier, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/users/{Uri.EscapeDataString(identifier)}/academic-profile", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AcademicProfileDto>(_jsonOptions, cancellationToken);
            }

            _logger?.LogWarning("GetAcademicProfile for {Identifier} returned {StatusCode}", identifier, response.StatusCode);
            
            if (identifier.Equals("marie_curie", StringComparison.OrdinalIgnoreCase) ||
                identifier.Equals(_sessionService.CurrentUserId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return _previewProfile;
            }

            return CreateColleaguePreview(identifier);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetAcademicProfile for {Identifier}. Falling back to preview state.", identifier);
            return CreateColleaguePreview(identifier);
        }
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/users/me", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<UserProfileDto>(_jsonOptions, cancellationToken);
                if (_previewProfile != null && updated != null)
                {
                    _previewProfile = _previewProfile with
                    {
                        DisplayName = updated.DisplayName,
                        Headline = updated.Headline,
                        Bio = updated.Bio,
                        CountryCode = updated.CountryCode,
                        City = updated.City,
                        PreferredLanguage = updated.PreferredLanguage,
                        Visibility = updated.Visibility
                    };
                }
                return updated;
            }

            _logger?.LogWarning("UpdateProfile returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to UpdateProfile. Updating local preview state.");
            if (_previewProfile != null)
            {
                _previewProfile = _previewProfile with
                {
                    DisplayName = request.DisplayName,
                    Headline = request.Headline,
                    Bio = request.Bio,
                    CountryCode = request.CountryCode,
                    City = request.City,
                    PreferredLanguage = request.PreferredLanguage,
                    Visibility = request.Visibility
                };
                return new UserProfileDto(
                    _previewProfile.UserId,
                    _previewProfile.DisplayName,
                    _previewProfile.Headline,
                    _previewProfile.Bio,
                    _previewProfile.CountryCode,
                    _previewProfile.City,
                    _previewProfile.PreferredLanguage,
                    _previewProfile.AvatarUrl,
                    _previewProfile.BannerUrl,
                    _previewProfile.Visibility,
                    DateTime.UtcNow
                );
            }
            return null;
        }
    }

    public async Task<UserEducationDto?> AddEducationAsync(CreateEducationRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/users/me/education", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserEducationDto>(_jsonOptions, cancellationToken);
            }

            _logger?.LogWarning("AddEducation returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to AddEducation. Updating local preview state.");
            var newEdu = new UserEducationDto(
                Guid.NewGuid(),
                request.InstitutionName,
                request.Degree,
                request.FieldOfStudy,
                request.StartYear,
                request.EndYear,
                request.Grade
            );
            if (_previewProfile != null)
            {
                var list = _previewProfile.Educations.ToList();
                list.Insert(0, newEdu);
                _previewProfile = _previewProfile with { Educations = list };
            }
            return newEdu;
        }
    }

    public async Task<bool> DeleteEducationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/users/me/education/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (_previewProfile != null)
                {
                    _previewProfile = _previewProfile with
                    {
                        Educations = _previewProfile.Educations.Where(e => e.Id != id).ToList()
                    };
                }
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to DeleteEducation. Updating local preview state.");
            if (_previewProfile != null)
            {
                _previewProfile = _previewProfile with
                {
                    Educations = _previewProfile.Educations.Where(e => e.Id != id).ToList()
                };
            }
            return true;
        }
    }

    public async Task<UserExperienceDto?> AddExperienceAsync(CreateExperienceRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/users/me/experience", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserExperienceDto>(_jsonOptions, cancellationToken);
            }
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to AddExperience. Updating local preview state.");
            var newExp = new UserExperienceDto(
                Guid.NewGuid(),
                request.OrganizationName,
                request.RoleTitle,
                request.EmploymentType,
                request.StartDate,
                request.EndDate,
                request.IsCurrent,
                request.Description
            );
            if (_previewProfile != null)
            {
                var list = _previewProfile.Experiences.ToList();
                list.Insert(0, newExp);
                _previewProfile = _previewProfile with { Experiences = list };
            }
            return newExp;
        }
    }

    public async Task<bool> DeleteExperienceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/users/me/experience/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (_previewProfile != null)
                {
                    _previewProfile = _previewProfile with
                    {
                        Experiences = _previewProfile.Experiences.Where(e => e.Id != id).ToList()
                    };
                }
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to DeleteExperience. Updating local preview state.");
            if (_previewProfile != null)
            {
                _previewProfile = _previewProfile with
                {
                    Experiences = _previewProfile.Experiences.Where(e => e.Id != id).ToList()
                };
            }
            return true;
        }
    }

    public async Task<UserSkillDto?> AddSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/users/me/skills", new CreateSkillRequest(skillName), _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserSkillDto>(_jsonOptions, cancellationToken);
            }
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to AddSkill. Updating local preview state.");
            var newSkill = new UserSkillDto(Guid.NewGuid(), skillName);
            if (_previewProfile != null)
            {
                var list = _previewProfile.Skills.ToList();
                if (!list.Any(s => s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(newSkill);
                    _previewProfile = _previewProfile with { Skills = list };
                }
            }
            return newSkill;
        }
    }

    public async Task<bool> DeleteSkillAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/users/me/skills/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (_previewProfile != null)
                {
                    _previewProfile = _previewProfile with
                    {
                        Skills = _previewProfile.Skills.Where(s => s.Id != id).ToList()
                    };
                }
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to DeleteSkill. Updating local preview state.");
            if (_previewProfile != null)
            {
                _previewProfile = _previewProfile with
                {
                    Skills = _previewProfile.Skills.Where(s => s.Id != id).ToList()
                };
            }
            return true;
        }
    }

    public async Task<List<PostDto>> GetUserPostsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/users/{userId}/posts", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PostDto>>(_jsonOptions, cancellationToken) ?? new List<PostDto>();
            }
            return GetPreviewPosts(userId);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetUserPosts. Returning preview posts.");
            return GetPreviewPosts(userId);
        }
    }

    public async Task<PostDto?> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/posts", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PostDto>(_jsonOptions, cancellationToken);
            }
            _logger?.LogWarning("CreatePost returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to CreatePost. Returning fallback post.");
            var fallback = new PostDto(
                Guid.NewGuid(),
                _sessionService.CurrentUserId,
                _sessionService.CurrentDisplayName,
                null,
                request.Content,
                request.Visibility,
                request.PublishImmediately ? "Published" : "Draft",
                DateTime.UtcNow,
                request.PublishImmediately ? DateTime.UtcNow : null,
                null,
                request.Attachments?.Select(a => new PostAttachmentDto(
                    Guid.NewGuid(),
                    a.FileName,
                    a.ContentType,
                    a.FileSizeBytes,
                    DateTime.UtcNow
                )).ToList() ?? new List<PostAttachmentDto>(),
                request.Category ?? "GeneralAcademic",
                _previewProfile?.Headline,
                _previewProfile?.Affiliations?.FirstOrDefault()?.OrganizationName
            );
            return fallback;
        }
    }

    public async Task<PostDto?> GetPostByIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/posts/{postId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PostDto>(_jsonOptions, cancellationToken);
            }
            _logger?.LogWarning("GetPostById returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetPostById for {PostId}.", postId);
            return null;
        }
    }

    public async Task<PostDto?> UpdatePostAsync(Guid postId, UpdatePostRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/posts/{postId}", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PostDto>(_jsonOptions, cancellationToken);
            }
            _logger?.LogWarning("UpdatePost returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to UpdatePost for {PostId}.", postId);
            return null;
        }
    }

    public async Task<bool> DeletePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/posts/{postId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to DeletePost for {PostId}.", postId);
            return true;
        }
    }

    public async Task<PostDto?> PublishPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/posts/{postId}/publish", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PostDto>(_jsonOptions, cancellationToken);
            }
            _logger?.LogWarning("PublishPost returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to PublishPost for {PostId}.", postId);
            return null;
        }
    }

    public async Task<PostDto?> ArchivePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/posts/{postId}/archive", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PostDto>(_jsonOptions, cancellationToken);
            }
            _logger?.LogWarning("ArchivePost returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to ArchivePost for {PostId}.", postId);
            return null;
        }
    }

    public async Task<IReadOnlyList<PostDto>> GetMyPostsAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/posts/mine", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<List<PostDto>>(_jsonOptions, cancellationToken);
                return list ?? new List<PostDto>();
            }
            _logger?.LogWarning("GetMyPosts returned {StatusCode}", response.StatusCode);
            return GetPreviewPosts(_sessionService.CurrentUserId);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetMyPosts. Returning preview posts.");
            return GetPreviewPosts(_sessionService.CurrentUserId);
        }
    }

    // =========================================================================
    // CHRONOLOGICAL FEED & KEYSET CURSOR PAGINATION
    // =========================================================================

    public async Task<FeedResponseDto?> GetMyNetworkFeedAsync(int limit = 20, string? cursor = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var url = $"api/v1/feed?limit={limit}";
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                url += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FeedResponseDto>(_jsonOptions, cancellationToken);
            }

            _logger?.LogWarning("GetMyNetworkFeed returned {StatusCode}", response.StatusCode);
            return GetFallbackNetworkFeed(cursor);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetMyNetworkFeed. Returning preview network feed.");
            return GetFallbackNetworkFeed(cursor);
        }
    }

    public async Task<FeedResponseDto?> GetDiscoverFeedAsync(int limit = 20, string? cursor = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var url = $"api/v1/feed/discover?limit={limit}";
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                url += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FeedResponseDto>(_jsonOptions, cancellationToken);
            }

            _logger?.LogWarning("GetDiscoverFeed returned {StatusCode}", response.StatusCode);
            return GetFallbackDiscoverFeed(cursor);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetDiscoverFeed. Returning preview discover feed.");
            return GetFallbackDiscoverFeed(cursor);
        }
    }

    // =========================================================================
    // RESEARCHER DISCOVERY
    // =========================================================================

    public async Task<IReadOnlyList<ResearcherDiscoveryDto>> GetDiscoverResearchersAsync(
        string? query = null,
        string? institution = null,
        string? expertise = null,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var uri = $"api/v1/users/discover?limit={limit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(query)) uri += $"&q={Uri.EscapeDataString(query)}";
            if (!string.IsNullOrWhiteSpace(institution)) uri += $"&institution={Uri.EscapeDataString(institution)}";
            if (!string.IsNullOrWhiteSpace(expertise)) uri += $"&expertise={Uri.EscapeDataString(expertise)}";

            var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<List<ResearcherDiscoveryDto>>(_jsonOptions, cancellationToken);
                return list ?? (IReadOnlyList<ResearcherDiscoveryDto>)Array.Empty<ResearcherDiscoveryDto>();
            }

            _logger?.LogWarning("GetDiscoverResearchers returned {StatusCode}", response.StatusCode);
            return FilterPreviewDiscoveries(query, institution, expertise);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetDiscoverResearchers. Returning preview data.");
            return FilterPreviewDiscoveries(query, institution, expertise);
        }
    }

    // =========================================================================
    // CONNECTIONS & REQUESTS
    // =========================================================================

    public async Task<IReadOnlyList<ConnectionUserDto>> GetAcceptedConnectionsAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/connections", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ConnectionUserDto>>(_jsonOptions, cancellationToken) ?? new List<ConnectionUserDto>();
            }
            return _previewConnections;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetAcceptedConnections. Returning preview connections.");
            return _previewConnections;
        }
    }

    public async Task<bool> RemoveConnectionAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/connections/{targetUserId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewConnections.RemoveAll(c => c.UserId == targetUserId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to RemoveConnection. Emulating success in preview.");
            _previewConnections.RemoveAll(c => c.UserId == targetUserId);
            return true;
        }
    }

    public async Task<IReadOnlyList<ConnectionDto>> GetIncomingConnectionRequestsAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/connections/requests/incoming", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ConnectionDto>>(_jsonOptions, cancellationToken) ?? new List<ConnectionDto>();
            }
            return _previewIncomingRequests;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetIncomingConnectionRequests. Returning preview requests.");
            return _previewIncomingRequests;
        }
    }

    public async Task<IReadOnlyList<ConnectionDto>> GetOutgoingConnectionRequestsAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/connections/requests/outgoing", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ConnectionDto>>(_jsonOptions, cancellationToken) ?? new List<ConnectionDto>();
            }
            return _previewOutgoingRequests;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetOutgoingConnectionRequests. Returning preview requests.");
            return _previewOutgoingRequests;
        }
    }

    public async Task<bool> SendConnectionRequestAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/connections/requests", new SendConnectionRequest(targetUserId), _jsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to SendConnectionRequest. Emulating success in preview.");
            return true;
        }
    }

    public async Task<bool> CancelConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/connections/requests/{requestId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewOutgoingRequests.RemoveAll(r => r.Id == requestId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to CancelConnectionRequest. Emulating success in preview.");
            _previewOutgoingRequests.RemoveAll(r => r.Id == requestId);
            return true;
        }
    }

    public async Task<bool> AcceptConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/connections/requests/{requestId}/accept", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewIncomingRequests.RemoveAll(r => r.Id == requestId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to AcceptConnectionRequest. Emulating success in preview.");
            _previewIncomingRequests.RemoveAll(r => r.Id == requestId);
            return true;
        }
    }

    public async Task<bool> RejectConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/connections/requests/{requestId}/reject", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewIncomingRequests.RemoveAll(r => r.Id == requestId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to RejectConnectionRequest. Emulating success in preview.");
            _previewIncomingRequests.RemoveAll(r => r.Id == requestId);
            return true;
        }
    }

    // =========================================================================
    // FOLLOWING & FOLLOWERS
    // =========================================================================

    public async Task<IReadOnlyList<FollowUserDto>> GetFollowingAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        var targetId = userId ?? _sessionService.CurrentUserId;
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/users/{targetId}/following", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<FollowUserDto>>(_jsonOptions, cancellationToken) ?? new List<FollowUserDto>();
            }
            return _previewFollowing;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetFollowing. Returning preview following.");
            return _previewFollowing;
        }
    }

    public async Task<IReadOnlyList<FollowUserDto>> GetFollowersAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        var targetId = userId ?? _sessionService.CurrentUserId;
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/users/{targetId}/followers", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<FollowUserDto>>(_jsonOptions, cancellationToken) ?? new List<FollowUserDto>();
            }
            return _previewFollowers;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetFollowers. Returning preview followers.");
            return _previewFollowers;
        }
    }

    public async Task<bool> FollowUserAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/users/{targetUserId}/follow", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to FollowUser. Emulating success in preview.");
            return true;
        }
    }

    public async Task<bool> UnfollowUserAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/users/{targetUserId}/follow", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewFollowing.RemoveAll(f => f.UserId == targetUserId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to UnfollowUser. Emulating success in preview.");
            _previewFollowing.RemoveAll(f => f.UserId == targetUserId);
            return true;
        }
    }

    // =========================================================================
    // BLOCKING
    // =========================================================================

    public async Task<IReadOnlyList<BlockedUserDto>> GetBlockedUsersAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/users/me/blocks", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<BlockedUserDto>>(_jsonOptions, cancellationToken) ?? new List<BlockedUserDto>();
            }
            return _previewBlocked;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to GetBlockedUsers. Returning preview blocked users.");
            return _previewBlocked;
        }
    }

    public async Task<bool> BlockUserAsync(Guid targetUserId, string? reason = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/users/{targetUserId}/block", new BlockUserRequest(reason), _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewConnections.RemoveAll(c => c.UserId == targetUserId);
                _previewFollowing.RemoveAll(f => f.UserId == targetUserId);
                _previewFollowers.RemoveAll(f => f.UserId == targetUserId);
                _previewIncomingRequests.RemoveAll(r => r.RequesterUserId == targetUserId);
                _previewOutgoingRequests.RemoveAll(r => r.AddresseeUserId == targetUserId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to BlockUser. Emulating success in preview.");
            _previewConnections.RemoveAll(c => c.UserId == targetUserId);
            return true;
        }
    }

    public async Task<bool> UnblockUserAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/users/{targetUserId}/block", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _previewBlocked.RemoveAll(b => b.UserId == targetUserId);
                return true;
            }
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to UnblockUser. Emulating success in preview.");
            _previewBlocked.RemoveAll(b => b.UserId == targetUserId);
            return true;
        }
    }

    // =========================================================================
    // PREVIEW DATA INITIALIZATION
    // =========================================================================

    private void InitPreviewData()
    {
        _previewProfile = new AcademicProfileDto(
            UserId: _sessionService.CurrentUserId,
            Username: _sessionService.CurrentUsername,
            DisplayName: _sessionService.CurrentDisplayName,
            Headline: "Professor of Physics & Chemistry | Nobel Laureate in Physics (1903) & Chemistry (1911)",
            Bio: "Pioneering researcher in radioactivity, isolation of radium and polonium, and development of mobile radiography units for military field hospitals. Committed to academic open access and global scientific collaboration.",
            CountryCode: "FR",
            City: "Paris",
            PreferredLanguage: "fr",
            AvatarUrl: null,
            BannerUrl: null,
            Visibility: "Public",
            UpdatedAtUtc: DateTime.UtcNow.AddMonths(-1),
            CompletenessScore: 92,
            IsVerified: true,
            Orcid: "0000-0002-1825-0097",
            Educations: new List<UserEducationDto>
            {
                new(Guid.NewGuid(), "Sorbonne Université (Faculty of Science)", "Doctorate of Science (DSc)", "Physics & Chemistry", 1898, 1903, "Summa Cum Laude"),
                new(Guid.NewGuid(), "Sorbonne Université", "Master of Science", "Physics & Mathematics", 1891, 1894, "First in Class")
            },
            Experiences: new List<UserExperienceDto>
            {
                new(Guid.NewGuid(), "Institut du Radium / Curietherapy Research Unit", "Director & Lead Investigator", "Full-time", new DateTime(1914, 1, 1), null, true, "Directing research laboratories in radioactivity, nuclear chemistry, and medical applications of ionizing radiation."),
                new(Guid.NewGuid(), "Sorbonne Université", "Professor of General Physics", "Full-time", new DateTime(1906, 5, 1), new DateTime(1934, 7, 1), false, "First female professor at the University of Paris; chair of general physics department.")
            },
            Skills: new List<UserSkillDto>
            {
                new(Guid.NewGuid(), "Radiochemistry"),
                new(Guid.NewGuid(), "Atomic Physics"),
                new(Guid.NewGuid(), "Isotope Separation"),
                new(Guid.NewGuid(), "Medical Radiography"),
                new(Guid.NewGuid(), "Peer Review & Ethics")
            },
            Interests: new List<string> { "Radioactivity", "Nuclear Physics", "Radiation Therapy" },
            Affiliations: new List<AffiliationDto>
            {
                new(Guid.NewGuid(), "Sorbonne Université", "Faculté des Sciences", "Professor", "Faculty", true),
                new(Guid.NewGuid(), "Institut du Radium", "Curie Laboratory", "Director", "Faculty", true)
            },
            PublicationsCount: 14,
            ConnectionsCount: 428,
            FollowersCount: 1250,
            FollowingCount: 189,
            ConnectionStatus: "None",
            IsFollowing: false,
            CanEdit: true
        );

        // Preview discoveries
        _previewDiscoveries.AddRange(new[]
        {
            new ResearcherDiscoveryDto(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "alan_turing",
                "Dr. Alan Turing",
                "Reader in Mathematics & Computing Pioneer | Fellow of the Royal Society",
                null,
                "University of Manchester",
                true,
                new[] { "Cryptanalysis", "Theoretical Computer Science", "Mathematical Logic" },
                9,
                312,
                "None",
                false
            ),
            new ResearcherDiscoveryDto(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "ada_lovelace",
                "Countess Ada Lovelace",
                "Computational Mathematician & Visionary of Analytical Engines",
                null,
                "Royal Scientific Society",
                true,
                new[] { "Algorithmic Design", "Bernoulli Computation", "Symbolic Math" },
                6,
                245,
                "PendingOutgoing",
                true
            ),
            new ResearcherDiscoveryDto(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "niels_bohr",
                "Prof. Niels Bohr",
                "Theoretical Physicist | Quantum Mechanics & Atomic Model",
                null,
                "University of Copenhagen",
                true,
                new[] { "Quantum Mechanics", "Atomic Models", "Complementarity Principle" },
                18,
                580,
                "PendingIncoming",
                false
            ),
            new ResearcherDiscoveryDto(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "rosalind_franklin",
                "Dr. Rosalind Franklin",
                "Biophysicist & Crystallographer | DNA B-Form Structure",
                null,
                "King's College London",
                true,
                new[] { "X-Ray Crystallography", "Molecular Biology", "Diffraction Analysis" },
                12,
                390,
                "Accepted",
                true
            )
        });

        // Preview connections
        _previewConnections.Add(new ConnectionUserDto(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Dr. Rosalind Franklin",
            "Biophysicist & Crystallographer | King's College London",
            null,
            DateTime.UtcNow.AddMonths(-3),
            "rosalind_franklin"
        ));

        // Preview incoming requests
        _previewIncomingRequests.Add(new ConnectionDto(
            Guid.NewGuid(),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            _sessionService.CurrentUserId,
            "Pending",
            DateTime.UtcNow.AddDays(-2),
            null,
            RequesterDisplayName: "Prof. Niels Bohr",
            RequesterHeadline: "Theoretical Physicist | University of Copenhagen",
            RequesterUsername: "niels_bohr"
        ));

        // Preview outgoing requests
        _previewOutgoingRequests.Add(new ConnectionDto(
            Guid.NewGuid(),
            _sessionService.CurrentUserId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Pending",
            DateTime.UtcNow.AddDays(-5),
            null,
            AddresseeDisplayName: "Countess Ada Lovelace",
            AddresseeHeadline: "Computational Mathematician | Royal Scientific Society",
            AddresseeUsername: "ada_lovelace"
        ));

        // Preview following
        _previewFollowing.Add(new FollowUserDto(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Dr. Rosalind Franklin",
            "Biophysicist & Crystallographer | King's College London",
            null,
            DateTime.UtcNow.AddMonths(-2),
            "rosalind_franklin"
        ));
        _previewFollowing.Add(new FollowUserDto(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Countess Ada Lovelace",
            "Computational Mathematician | Royal Scientific Society",
            null,
            DateTime.UtcNow.AddMonths(-1),
            "ada_lovelace"
        ));

        // Preview followers
        _previewFollowers.Add(new FollowUserDto(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Dr. Rosalind Franklin",
            "Biophysicist & Crystallographer | King's College London",
            null,
            DateTime.UtcNow.AddMonths(-2),
            "rosalind_franklin"
        ));

        // Preview blocked
        _previewBlocked.Add(new BlockedUserDto(
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            "Prof. Pseudoscientist",
            null,
            "Spam naskah tidak ilmiah berulang.",
            DateTime.UtcNow.AddMonths(-4),
            "pseudoscience_lab"
        ));
    }

    private IReadOnlyList<ResearcherDiscoveryDto> FilterPreviewDiscoveries(string? query, string? institution, string? expertise)
    {
        var list = _previewDiscoveries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            list = list.Where(r => r.DisplayName.ToLower().Contains(q) ||
                                  r.Username.ToLower().Contains(q) ||
                                  (r.Headline != null && r.Headline.ToLower().Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(institution))
        {
            var inst = institution.Trim().ToLower();
            list = list.Where(r => r.PrimaryAffiliation != null && r.PrimaryAffiliation.ToLower().Contains(inst));
        }

        if (!string.IsNullOrWhiteSpace(expertise))
        {
            var exp = expertise.Trim().ToLower();
            list = list.Where(r => r.TopSkills.Any(s => s.ToLower().Contains(exp)));
        }

        return list.ToList();
    }

    private static AcademicProfileDto CreateColleaguePreview(string identifier)
    {
        return new AcademicProfileDto(
            UserId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Username: identifier,
            DisplayName: "Dr. Alan Turing",
            Headline: "Reader in Mathematics & Computing Pioneer | Fellow of the Royal Society",
            Bio: "Mathematician, computer scientist, logician, and theoretical biologist. Researching computational machines, morphogenetic morphogenesis, and algorithmic decidability.",
            CountryCode: "GB",
            City: "Manchester",
            PreferredLanguage: "en",
            AvatarUrl: null,
            BannerUrl: null,
            Visibility: "Public",
            UpdatedAtUtc: DateTime.UtcNow.AddMonths(-2),
            CompletenessScore: 88,
            IsVerified: true,
            Orcid: "0000-0003-4921-8841",
            Educations: new List<UserEducationDto>
            {
                new(Guid.NewGuid(), "King's College, Cambridge", "Ph.D.", "Mathematics & Cryptography", 1936, 1938, "With High Honors"),
                new(Guid.NewGuid(), "King's College, Cambridge", "B.A. (Hons)", "Mathematics", 1931, 1934, "First Class")
            },
            Experiences: new List<UserExperienceDto>
            {
                new(Guid.NewGuid(), "University of Manchester", "Deputy Director, Computing Machine Laboratory", "Full-time", new DateTime(1948, 9, 1), null, true, "Leading early stored-program computer architecture (Manchester Mark 1) and artificial intelligence foundations.")
            },
            Skills: new List<UserSkillDto>
            {
                new(Guid.NewGuid(), "Theoretical Computer Science"),
                new(Guid.NewGuid(), "Cryptanalysis"),
                new(Guid.NewGuid(), "Morphogenesis"),
                new(Guid.NewGuid(), "Mathematical Logic")
            },
            Interests: new List<string> { "Algorithms", "Artificial Intelligence", "Cryptography" },
            Affiliations: new List<AffiliationDto>
            {
                new(Guid.NewGuid(), "University of Manchester", "Department of Mathematics", "Reader", "Faculty", true),
                new(Guid.NewGuid(), "King's College, Cambridge", "Faculty of Mathematics", "Fellow", "Fellow", true)
            },
            PublicationsCount: 9,
            ConnectionsCount: 312,
            FollowersCount: 890,
            FollowingCount: 140,
            ConnectionStatus: "None",
            IsFollowing: false,
            CanEdit: false
        );
    }

    private static List<PostDto> GetPreviewPosts(Guid userId)
    {
        return new List<PostDto>
        {
            new(
                Guid.NewGuid(),
                userId,
                "Dr. Marie Curie",
                null,
                "Just published our latest findings on fractional crystallization methods for polonium isolation. Ensuring rigorous reproducible standards across European laboratories.",
                "Public",
                "Published",
                DateTime.UtcNow.AddDays(-3),
                DateTime.UtcNow.AddDays(-3),
                null,
                new List<PostAttachmentDto>()
            ),
            new(
                Guid.NewGuid(),
                userId,
                "Dr. Marie Curie",
                null,
                "Our Mobile Radiography training curriculum has been successfully digitized. Academic open access allows medical researchers worldwide to inspect calibration benchmarks freely.",
                "Public",
                "Published",
                DateTime.UtcNow.AddDays(-14),
                DateTime.UtcNow.AddDays(-14),
                null,
                new List<PostAttachmentDto>()
            )
        };
    }

    private static FeedResponseDto GetFallbackNetworkFeed(string? cursor)
    {
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            // Second page preview: no more items
            return new FeedResponseDto(new List<PostDto>
            {
                new(
                    Guid.NewGuid(),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "Dr. Rosalind Franklin",
                    null,
                    "[Research Finding]\n\nPhotograph 51 confirms the helical B-form structure of DNA with clear cross-shaped X-ray diffraction patterns.",
                    "Connections",
                    "Published",
                    DateTime.UtcNow.AddDays(-5),
                    DateTime.UtcNow.AddDays(-5),
                    null,
                    new List<PostAttachmentDto>
                    {
                        new(Guid.NewGuid(), "diffraction_b_form.png", "image/png", 1024 * 350, DateTime.UtcNow.AddDays(-5))
                    }
                )
            }, null, false);
        }

        var items = new List<PostDto>
        {
            new(
                Guid.NewGuid(),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Prof. Albert Einstein",
                null,
                "[Research Update]\n\nDalam peninjauan ulang tensor kelengkungan Riemann untuk persamaan medan gravitasi, kami menemukan solusi batas baru yang menjembatani konsistensi metrik Schwarzschild pada koordinat isotropik.",
                "Public",
                "Published",
                DateTime.UtcNow.AddHours(-2),
                DateTime.UtcNow.AddHours(-2),
                null,
                new List<PostAttachmentDto>()
            ),
            new(
                Guid.NewGuid(),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Dr. Marie Curie",
                null,
                "[Academic Discussion]\n\nBagaimana protokol proteksi radiasi di laboratorium Anda untuk penanganan garam radium? Kami menyusun panduan keselamatan standar Eropa.",
                "Connections",
                "Published",
                DateTime.UtcNow.AddHours(-6),
                DateTime.UtcNow.AddHours(-6),
                null,
                new List<PostAttachmentDto>
                {
                    new(Guid.NewGuid(), "radiation_safety_draft.pdf", "application/pdf", 1024 * 120, DateTime.UtcNow.AddHours(-6))
                }
            ),
            new(
                Guid.NewGuid(),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "Dr. Alan Turing",
                null,
                "[Publication]\n\nOn Computable Numbers with an Application to the Entscheidungsproblem has been indexed for cross-referencing in mathematical logic databases.",
                "Public",
                "Published",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(-1),
                null,
                new List<PostAttachmentDto>()
            )
        };

        return new FeedResponseDto(items, "fallback_cursor_page_2", true);
    }

    private static FeedResponseDto GetFallbackDiscoverFeed(string? cursor)
    {
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            return new FeedResponseDto(new List<PostDto>(), null, false);
        }

        var items = new List<PostDto>
        {
            new(
                Guid.NewGuid(),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Prof. Albert Einstein",
                null,
                "[Research Finding]\n\nGeneral relativity formulation exhibits invariant light deflection in gravitational fields. Experimental verification during solar eclipse confirmed.",
                "Public",
                "Published",
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow.AddHours(-1),
                null,
                new List<PostAttachmentDto>()
            ),
            new(
                Guid.NewGuid(),
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "Prof. Niels Bohr",
                null,
                "[Academic Discussion]\n\nQuantum transitions of atomic electrons emit discrete spectral lines governed by quantized angular momentum.",
                "Public",
                "Published",
                DateTime.UtcNow.AddHours(-8),
                DateTime.UtcNow.AddHours(-8),
                null,
                new List<PostAttachmentDto>()
            )
        };

        return new FeedResponseDto(items, null, false);
    }
}

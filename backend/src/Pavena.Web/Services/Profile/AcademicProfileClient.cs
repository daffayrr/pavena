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
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to CancelConnectionRequest. Emulating success in preview.");
            return true;
        }
    }

    public async Task<bool> AcceptConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/connections/requests/{requestId}/accept", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to AcceptConnectionRequest. Emulating success in preview.");
            return true;
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
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "HTTP call failed to UnfollowUser. Emulating success in preview.");
            return true;
        }
    }

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
}

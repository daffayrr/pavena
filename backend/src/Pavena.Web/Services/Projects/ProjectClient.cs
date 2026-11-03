using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pavena.Application.Research.DTOs;
using Pavena.Web.Services.Auth;

namespace Pavena.Web.Services.Projects;

public class ProjectClient : IProjectClient
{
    private readonly HttpClient _httpClient;
    private readonly IScholarSessionService _sessionService;
    private readonly ILogger<ProjectClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Local in-memory state for preview/fallback
    private readonly List<ProjectDetailDto> _previewProjects = new();
    private readonly List<ProjectActivityLogDto> _previewLogs = new();
    private readonly List<ProjectInvitationDto> _previewInvitations = new();

    public ProjectClient(
        HttpClient httpClient,
        IScholarSessionService sessionService,
        ILogger<ProjectClient>? logger = null)
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

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetUserProjectsAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/projects", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProjectSummaryDto>>(_jsonOptions, cancellationToken);
                return result ?? Array.Empty<ProjectSummaryDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to GetUserProjects failed, falling back to local state.");
        }

        return _previewProjects.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetDiscoverProjectsAsync(string? q = null, string? field = null, int limit = 20, int offset = 0, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var url = $"api/v1/projects/discover?limit={limit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(q)) url += $"&q={Uri.EscapeDataString(q)}";
            if (!string.IsNullOrWhiteSpace(field)) url += $"&field={Uri.EscapeDataString(field)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProjectSummaryDto>>(_jsonOptions, cancellationToken);
                return result ?? Array.Empty<ProjectSummaryDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to GetDiscoverProjects failed, falling back to local state.");
        }

        var query = _previewProjects.Where(p => p.Visibility == "PublicDiscovery");
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || (p.Abstract != null && p.Abstract.Contains(q, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(field))
            query = query.Where(p => string.Equals(p.ResearchField, field, StringComparison.OrdinalIgnoreCase));

        return query.Select(MapToSummary).ToList();
    }

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/projects/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to GetProjectById failed, falling back to local state.");
        }

        return _previewProjects.FirstOrDefault(p => p.Id == id);
    }

    public async Task<ProjectDetailDto?> GetProjectBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/projects/slug/{slug}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to GetProjectBySlug failed, falling back to local state.");
        }

        return _previewProjects.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProjectDetailDto?> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/projects", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to CreateProject failed, falling back to local state.");
        }

        var newProject = new ProjectDetailDto(
            Guid.NewGuid(),
            request.Title,
            request.Slug,
            request.Description,
            request.ResearchObjectives,
            request.ResearchField,
            "ResearcherInitiated",
            null,
            null,
            request.Visibility,
            "Active",
            "Concept",
            _sessionService.CurrentUserId,
            "Peneliti Utama",
            null,
            null,
            DateTime.UtcNow,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            new List<ProjectMemberDto>
            {
                new(Guid.NewGuid(), Guid.Empty, _sessionService.CurrentUserId, "Peneliti Utama", "Lead Researcher", null, "ProjectLead", "Active", DateTime.UtcNow)
            },
            new List<ProjectTaskDto>(),
            new List<ProjectMilestoneDto>(),
            new List<ProjectInvitationDto>()
        );
        _previewProjects.Insert(0, newProject);
        return newProject;
    }

    public async Task<ProjectDetailDto?> UpdateProjectAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/projects/{id}", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to UpdateProject failed, falling back to local state.");
        }

        var existing = _previewProjects.FirstOrDefault(p => p.Id == id);
        if (existing != null)
        {
            var updated = existing with
            {
                Title = request.Title,
                Abstract = request.Description,
                ResearchField = request.ResearchField,
                ResearchObjectives = request.ResearchObjectives,
                Visibility = request.Visibility,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var idx = _previewProjects.IndexOf(existing);
            _previewProjects[idx] = updated;
            return updated;
        }

        return null;
    }

    public async Task<ProjectDetailDto?> UpdateStatusAsync(Guid id, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/{id}/status", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to UpdateStatus failed, falling back to local state.");
        }

        var existing = _previewProjects.FirstOrDefault(p => p.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = request.Status, UpdatedAtUtc = DateTime.UtcNow };
            var idx = _previewProjects.IndexOf(existing);
            _previewProjects[idx] = updated;
            return updated;
        }

        return null;
    }

    public async Task<ProjectDetailDto?> AdvanceStageAsync(Guid id, AdvanceProjectStageRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/{id}/stage", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to AdvanceStage failed, falling back to local state.");
        }

        var existing = _previewProjects.FirstOrDefault(p => p.Id == id);
        if (existing != null)
        {
            var updated = existing with { Stage = request.Stage, UpdatedAtUtc = DateTime.UtcNow };
            var idx = _previewProjects.IndexOf(existing);
            _previewProjects[idx] = updated;
            return updated;
        }

        return null;
    }

    public async Task<ProjectInvitationDto?> InviteMemberAsync(Guid projectId, InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/{projectId}/members/invite", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectInvitationDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to InviteMember failed, falling back to local state.");
        }

        var inv = new ProjectInvitationDto(
            Guid.NewGuid(),
            projectId,
            "Research Workspace",
            request.InvitedUserId,
            "Calon Kolaborator",
            _sessionService.CurrentUserId,
            "Peneliti Utama",
            request.Role,
            "Pending",
            request.Message,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow
        );
        _previewInvitations.Add(inv);
        return inv;
    }

    public async Task<IReadOnlyList<ProjectInvitationDto>> GetMyInvitationsAsync(CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync("api/v1/projects/invitations/mine", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProjectInvitationDto>>(_jsonOptions, cancellationToken);
                return result ?? Array.Empty<ProjectInvitationDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to GetMyInvitations failed, falling back to local state.");
        }

        return _previewInvitations;
    }

    public async Task<bool> RespondInvitationAsync(Guid invitationId, bool accept, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/invitations/{invitationId}/respond", new RespondInvitationRequest(accept), _jsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to RespondInvitation failed, falling back to local state.");
            _previewInvitations.RemoveAll(i => i.Id == invitationId);
            return true;
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid projectId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/projects/{projectId}/members/{targetUserId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to RemoveMember failed, falling back to local state.");
            var project = _previewProjects.FirstOrDefault(p => p.Id == projectId);
            if (project != null)
            {
                var updatedMembers = project.Members.Where(m => m.UserId != targetUserId).ToList();
                var idx = _previewProjects.IndexOf(project);
                _previewProjects[idx] = project with { Members = updatedMembers };
            }
            return true;
        }
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid projectId, Guid targetUserId, UpdateMemberRoleRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/projects/{projectId}/members/{targetUserId}/role", request, _jsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to UpdateMemberRole failed, falling back to local state.");
            return true;
        }
    }

    public async Task<bool> TransferLeadershipAsync(Guid projectId, TransferLeadershipRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/{projectId}/leadership/transfer", request, _jsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to TransferLeadership failed, falling back to local state.");
            return true;
        }
    }

    public async Task<ProjectMilestoneDto?> CreateMilestoneAsync(Guid projectId, CreateMilestoneRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/{projectId}/milestones", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectMilestoneDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to CreateMilestone failed, falling back to local state.");
        }

        var ms = new ProjectMilestoneDto(Guid.NewGuid(), projectId, request.Title, request.Description, request.TargetDateUtc, "Planned", null, 0, 0, DateTime.UtcNow);
        var proj = _previewProjects.FirstOrDefault(p => p.Id == projectId);
        if (proj != null)
        {
            var milestones = proj.Milestones.ToList();
            milestones.Add(ms);
            var idx = _previewProjects.IndexOf(proj);
            _previewProjects[idx] = proj with { Milestones = milestones };
        }
        return ms;
    }

    public async Task<ProjectMilestoneDto?> UpdateMilestoneAsync(Guid projectId, Guid milestoneId, UpdateMilestoneRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/projects/{projectId}/milestones/{milestoneId}", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectMilestoneDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to UpdateMilestone failed, falling back to local state.");
        }

        return null;
    }

    public async Task<ProjectTaskDto?> CreateTaskAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/projects/{projectId}/tasks", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectTaskDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to CreateTask failed, falling back to local state.");
        }

        var task = new ProjectTaskDto(Guid.NewGuid(), projectId, request.MilestoneId, "Milestone Target", request.Title, request.Description, request.AssigneeUserId, "Peneliti", request.Priority, "Todo", request.DueDateUtc, _sessionService.CurrentUserId, null, DateTime.UtcNow);
        var proj = _previewProjects.FirstOrDefault(p => p.Id == projectId);
        if (proj != null)
        {
            var tasks = proj.Tasks.ToList();
            tasks.Add(task);
            var idx = _previewProjects.IndexOf(proj);
            _previewProjects[idx] = proj with { Tasks = tasks };
        }
        return task;
    }

    public async Task<ProjectTaskDto?> UpdateTaskAsync(Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/projects/{projectId}/tasks/{taskId}", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectTaskDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to UpdateTask failed, falling back to local state.");
        }

        return null;
    }

    public async Task<IReadOnlyList<ProjectActivityLogDto>> GetActivityTimelineAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/projects/{projectId}/timeline", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProjectActivityLogDto>>(_jsonOptions, cancellationToken);
                return result ?? Array.Empty<ProjectActivityLogDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "API request to GetActivityTimeline failed, falling back to local state.");
        }

        return _previewLogs.Where(l => l.ProjectId == projectId).ToList();
    }

    private static ProjectSummaryDto MapToSummary(ProjectDetailDto p) => new(
        p.Id,
        p.Title,
        p.Slug,
        p.Abstract,
        p.ResearchField,
        p.OriginType,
        p.Visibility,
        p.Status,
        p.Stage,
        p.OwnerUserId,
        p.OwnerDisplayName,
        p.OwnerOrganizationId,
        p.OwnerOrganizationName,
        p.Members.Count,
        p.Tasks.Count,
        p.Milestones.Count,
        p.CreatedAtUtc,
        p.UpdatedAtUtc
    );

    private void InitPreviewData()
    {
        var projId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var p1 = new ProjectDetailDto(
            projId,
            "AI-Driven Drug Discovery for Neglected Tropical Diseases",
            "ai-drug-discovery-tropical-diseases",
            "Penelitian kolaboratif pemodelan bioinformatika menggunakan model graf generatif untuk sintesis molekul kandidat obat penyakit tropis terabaikan di Asia Tenggara.",
            "1. Menghasilkan pustaka 10.000 molekul baru.\n2. Validasi binding affinity in silico.\n3. Uji in vitro bersama mitra industri farmasi.",
            "Biomedical Informatics",
            "ResearcherInitiated",
            null,
            null,
            "PublicDiscovery",
            "Active",
            "ActiveResearch",
            _sessionService.CurrentUserId,
            "Prof. Dr. Ir. Farhan Hidayat, M.Sc.",
            null,
            null,
            DateTime.UtcNow.AddMonths(-3),
            DateTime.UtcNow.AddMonths(9),
            DateTime.UtcNow.AddMonths(-3),
            DateTime.UtcNow,
            new List<ProjectMemberDto>
            {
                new(Guid.NewGuid(), projId, _sessionService.CurrentUserId, "Prof. Dr. Ir. Farhan Hidayat, M.Sc.", "Principal Investigator & AI Specialist", null, "ProjectLead", "Active", DateTime.UtcNow.AddMonths(-3)),
                new(Guid.NewGuid(), projId, Guid.NewGuid(), "Dr. Siti Aminah, M.Biotech", "Senior Molecular Biologist", null, "Researcher", "Active", DateTime.UtcNow.AddMonths(-2)),
                new(Guid.NewGuid(), projId, Guid.NewGuid(), "Budi Santoso, S.Kom.", "Research Assistant (Data Engineering)", null, "ResearchAssistant", "Active", DateTime.UtcNow.AddMonths(-1))
            },
            new List<ProjectTaskDto>
            {
                new(Guid.NewGuid(), projId, null, null, "Pengumpulan dataset molekuler ChEMBL & PubChem", "Kompilasi 500k senyawa referensi", _sessionService.CurrentUserId, "Prof. Dr. Ir. Farhan Hidayat, M.Sc.", "High", "Done", DateTime.UtcNow.AddMonths(-2), _sessionService.CurrentUserId, DateTime.UtcNow.AddMonths(-2), DateTime.UtcNow.AddMonths(-3)),
                new(Guid.NewGuid(), projId, null, null, "Training model transformer graph neural network", "Iterasi epoch 50 pada GPU cluster", _sessionService.CurrentUserId, "Prof. Dr. Ir. Farhan Hidayat, M.Sc.", "High", "InProgress", DateTime.UtcNow.AddDays(7), _sessionService.CurrentUserId, null, DateTime.UtcNow.AddMonths(-1)),
                new(Guid.NewGuid(), projId, null, null, "Penyusunan manuskrip jurnal kuartil 1", "Drafting bagian metodologi dan hasil komputasi", null, null, "Medium", "Todo", DateTime.UtcNow.AddMonths(1), _sessionService.CurrentUserId, null, DateTime.UtcNow)
            },
            new List<ProjectMilestoneDto>
            {
                new(Guid.NewGuid(), projId, "Milestone 1: Pustaka Data Terkurasi", "Dataset terverifikasi dengan fitur struktural 3D", DateTime.UtcNow.AddMonths(-1), "Achieved", DateTime.UtcNow.AddMonths(-1), 2, 2, DateTime.UtcNow.AddMonths(-3)),
                new(Guid.NewGuid(), projId, "Milestone 2: Validasi In Silico Model AI", "Akurasi prediksi afinitas docking > 85%", DateTime.UtcNow.AddMonths(2), "Planned", null, 3, 1, DateTime.UtcNow.AddMonths(-3)),
                new(Guid.NewGuid(), projId, "Milestone 3: Sintesis Laboratorium & Publikasi", "Penyelesaian uji coba basah dan submit jurnal internasional", DateTime.UtcNow.AddMonths(6), "Planned", null, 2, 0, DateTime.UtcNow.AddMonths(-3))
            },
            new List<ProjectInvitationDto>()
        );

        _previewProjects.Add(p1);

        _previewLogs.Add(new ProjectActivityLogDto(Guid.NewGuid(), projId, _sessionService.CurrentUserId, "Prof. Dr. Ir. Farhan Hidayat, M.Sc.", "StageAdvanced", "Research maturity stage advanced to ActiveResearch.", DateTime.UtcNow.AddDays(-5)));
        _previewLogs.Add(new ProjectActivityLogDto(Guid.NewGuid(), projId, _sessionService.CurrentUserId, "Prof. Dr. Ir. Farhan Hidayat, M.Sc.", "MemberAdded", "Dr. Siti Aminah, M.Biotech joined as Researcher.", DateTime.UtcNow.AddMonths(-2)));
        _previewLogs.Add(new ProjectActivityLogDto(Guid.NewGuid(), projId, _sessionService.CurrentUserId, "Prof. Dr. Ir. Farhan Hidayat, M.Sc.", "ProjectCreated", "Project workspace initialized under HULU track.", DateTime.UtcNow.AddMonths(-3)));
    }
}

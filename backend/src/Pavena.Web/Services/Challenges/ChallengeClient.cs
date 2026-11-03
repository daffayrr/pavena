using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pavena.Application.Challenges.DTOs;
using Pavena.Domain.Enums;
using Pavena.Web.Services.Auth;

namespace Pavena.Web.Services.Challenges;

public class ChallengeClient : IChallengeClient
{
    private readonly HttpClient _httpClient;
    private readonly IScholarSessionService _sessionService;
    private readonly ILogger<ChallengeClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Fallback in-memory store for UI preview
    private readonly List<ChallengeDetailDto> _previewChallenges = new();
    private readonly List<ChallengeActivityLogDto> _previewLogs = new();

    public ChallengeClient(
        HttpClient httpClient,
        IScholarSessionService sessionService,
        ILogger<ChallengeClient>? logger = null)
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

    public async Task<List<ChallengeSummaryDto>> SearchChallengesAsync(
        string? query = null,
        string? domain = null,
        ChallengeStatus? status = null,
        InstitutionalEligibility? eligibility = null,
        decimal? maxBudget = null,
        Guid? organizationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(domain)) queryParams.Add($"domain={Uri.EscapeDataString(domain)}");
        if (status.HasValue) queryParams.Add($"status={status.Value}");
        if (eligibility.HasValue) queryParams.Add($"eligibility={eligibility.Value}");
        if (maxBudget.HasValue) queryParams.Add($"maxBudget={maxBudget.Value}");
        if (organizationId.HasValue) queryParams.Add($"organizationId={organizationId.Value}");

        var url = $"api/v1/challenges?{string.Join("&", queryParams)}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ChallengeSummaryDto>>(_jsonOptions, cancellationToken);
                if (result != null) return result;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch challenges from API, falling back to preview");
        }

        // Preview fallback filter
        var filtered = _previewChallenges.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                           c.Summary.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(domain))
        {
            filtered = filtered.Where(c => c.Domains.Any(d => d.DomainName.Contains(domain, StringComparison.OrdinalIgnoreCase)));
        }
        if (status.HasValue)
        {
            filtered = filtered.Where(c => c.Status == status.Value);
        }
        if (eligibility.HasValue)
        {
            filtered = filtered.Where(c => c.Eligibility == eligibility.Value);
        }
        if (maxBudget.HasValue)
        {
            filtered = filtered.Where(c => !c.MaxBudget.HasValue || c.MaxBudget.Value <= maxBudget.Value);
        }

        return filtered.Select(ToSummaryDto).Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public async Task<ChallengeDetailDto?> GetChallengeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/challenges/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get challenge {Id} from API, falling back to preview", id);
        }

        return _previewChallenges.FirstOrDefault(c => c.Id == id);
    }

    public async Task<ChallengeDetailDto?> CreateChallengeAsync(CreateChallengeRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/challenges", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create challenge via API, falling back to preview");
        }

        var newChallenge = new ChallengeDetailDto(
            Id: Guid.NewGuid(),
            OrganizationId: request.OrganizationId,
            OrganizationName: "InnovateCorp Industries",
            OrganizationLogoUrl: null,
            OrganizationType: OrganizationType.Company,
            IsOrganizationVerified: true,
            CreatedByUserId: Guid.NewGuid(),
            CreatorName: "Lead Corporate Investigator",
            Title: request.Title,
            Slug: request.Title.ToLowerInvariant().Replace(" ", "-"),
            Summary: request.Summary,
            ProblemStatement: request.ProblemStatement,
            ResearchScope: request.ResearchScope,
            ExpectedOutcomes: request.ExpectedOutcomes,
            Status: ChallengeStatus.Draft,
            Visibility: request.Visibility,
            Eligibility: request.Eligibility,
            Currency: request.Currency,
            MinBudget: request.MinBudget,
            MaxBudget: request.MaxBudget,
            SubmissionDeadlineUtc: request.SubmissionDeadlineUtc,
            EstimatedDurationMonths: request.EstimatedDurationMonths,
            RequiresManualNdaApproval: request.RequiresManualNdaApproval,
            NdaTermsVersion: "1.0",
            NdaTermsText: request.NdaTermsText,
            HasSignedNda: false,
            IsNdaApproved: false,
            PublishedAtUtc: null,
            ClosedAtUtc: null,
            CancelledAtUtc: null,
            CancellationReason: null,
            Domains: request.Domains?.Select(d => new ChallengeDomainDto(Guid.NewGuid(), d.DomainName, d.SubDomain, d.IsPrimary)).ToList() ?? new(),
            RequiredExpertise: request.Expertise?.Select(e => new ChallengeExpertiseDto(Guid.NewGuid(), e.SkillOrDiscipline, e.ImportanceLevel, e.Description)).ToList() ?? new(),
            Attachments: new(),
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow
        );

        _previewChallenges.Insert(0, newChallenge);
        return newChallenge;
    }

    public async Task<ChallengeDetailDto?> UpdateChallengeAsync(Guid id, UpdateChallengeRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/challenges/{id}", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChallengeDetailDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to update challenge via API, falling back to preview");
        }

        return _previewChallenges.FirstOrDefault(c => c.Id == id);
    }

    public async Task<bool> SubmitForApprovalAsync(Guid id, string? notes = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/challenges/{id}/submit", notes, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to submit challenge for approval via API");
        }

        var existing = _previewChallenges.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = ChallengeStatus.SubmittedForApproval };
            _previewChallenges.Remove(existing);
            _previewChallenges.Add(updated);
        }
        return true;
    }

    public async Task<bool> ProcessApprovalDecisionAsync(Guid id, ChallengeDecisionRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/challenges/{id}/decision", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to process challenge decision via API");
        }

        var existing = _previewChallenges.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = request.Decision == ChallengeApprovalDecision.Approved ? ChallengeStatus.Approved : ChallengeStatus.Draft };
            _previewChallenges.Remove(existing);
            _previewChallenges.Add(updated);
        }
        return true;
    }

    public async Task<bool> PublishChallengeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/challenges/{id}/publish", null, cancellationToken);
            if (response.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to publish challenge via API");
        }

        var existing = _previewChallenges.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = ChallengeStatus.Published, PublishedAtUtc = DateTime.UtcNow };
            _previewChallenges.Remove(existing);
            _previewChallenges.Add(updated);
        }
        return true;
    }

    public async Task<bool> OpenProposalsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/challenges/{id}/open-proposals", null, cancellationToken);
            if (response.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to open proposals for challenge via API");
        }

        var existing = _previewChallenges.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = ChallengeStatus.OpenForProposals };
            _previewChallenges.Remove(existing);
            _previewChallenges.Add(updated);
        }
        return true;
    }

    public async Task<bool> CloseChallengeAsync(Guid id, string? notes = null, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/challenges/{id}/close", notes, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to close challenge via API");
        }

        var existing = _previewChallenges.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = ChallengeStatus.Closed, ClosedAtUtc = DateTime.UtcNow };
            _previewChallenges.Remove(existing);
            _previewChallenges.Add(updated);
        }
        return true;
    }

    public async Task<bool> CancelChallengeAsync(Guid id, CancelChallengeRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/challenges/{id}/cancel", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to cancel challenge via API");
        }

        var existing = _previewChallenges.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            var updated = existing with { Status = ChallengeStatus.Cancelled, CancelledAtUtc = DateTime.UtcNow, CancellationReason = request.Reason };
            _previewChallenges.Remove(existing);
            _previewChallenges.Add(updated);
        }
        return true;
    }

    public async Task<EligibilityCheckResultDto?> CheckEligibilityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/challenges/{id}/eligibility", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EligibilityCheckResultDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to check eligibility via API");
        }

        return new EligibilityCheckResultDto(
            ChallengeId: id,
            IsEligible: true,
            RequiredEligibility: InstitutionalEligibility.OpenToAll,
            ResearcherAffiliationName: "University of Pavena",
            HasVerifiedAffiliation: true,
            MissingCriteria: new(),
            MatchedExpertise: new() { "Computer Science", "Robotics" }
        );
    }

    public async Task<ChallengeNdaStatusDto?> GetNdaStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/challenges/{id}/nda/status", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChallengeNdaStatusDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get NDA status via API");
        }

        return new ChallengeNdaStatusDto(
            ChallengeId: id,
            HasSignedNda: false,
            IsApproved: false,
            SignedNdaVersion: null,
            AgreedAtUtc: null,
            RequiresReagreement: false
        );
    }

    public async Task<ChallengeNdaStatusDto?> SignNdaAsync(Guid id, SignNdaRequest request, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/challenges/{id}/nda/sign", request, _jsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChallengeNdaStatusDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to sign NDA via API");
        }

        return new ChallengeNdaStatusDto(
            ChallengeId: id,
            HasSignedNda: true,
            IsApproved: true,
            SignedNdaVersion: request.NdaVersion,
            AgreedAtUtc: DateTime.UtcNow,
            RequiresReagreement: false
        );
    }

    public async Task<bool> ApproveNdaAsync(Guid id, Guid ndaId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/challenges/{id}/nda/{ndaId}/approve", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to approve NDA via API");
            return false;
        }
    }

    public async Task<ChallengeAttachmentDto?> UploadAttachmentAsync(
        Guid id,
        string fileName,
        byte[] fileBytes,
        string mimeType,
        AttachmentConfidentialityLevel confidentiality,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(confidentiality.ToString()), "confidentiality");
            if (!string.IsNullOrEmpty(description))
            {
                content.Add(new StringContent(description), "description");
            }

            var response = await _httpClient.PostAsync($"api/v1/challenges/{id}/attachments", content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChallengeAttachmentDto>(_jsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to upload attachment via API");
        }

        var newAtt = new ChallengeAttachmentDto(
            Id: Guid.NewGuid(),
            ChallengeId: id,
            FileName: fileName,
            FileExtension: Path.GetExtension(fileName),
            MimeType: mimeType,
            SizeBytes: fileBytes.Length,
            ConfidentialityLevel: confidentiality,
            Description: description,
            CanDownload: true,
            CreatedAtUtc: DateTime.UtcNow
        );

        return newAtt;
    }

    public async Task<byte[]?> DownloadAttachmentBytesAsync(Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/challenges/{id}/attachments/{attachmentId}/download", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to download attachment bytes via API");
        }

        return null;
    }

    public async Task<bool> DeleteAttachmentAsync(Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/challenges/{id}/attachments/{attachmentId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete attachment via API");
            return false;
        }
    }

    public async Task<List<ChallengeActivityLogDto>> GetActivityLogsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/challenges/{id}/activity", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ChallengeActivityLogDto>>(_jsonOptions, cancellationToken);
                if (result != null) return result;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get challenge activity via API");
        }

        return _previewLogs;
    }

    private ChallengeSummaryDto ToSummaryDto(ChallengeDetailDto d)
    {
        return new ChallengeSummaryDto(
            Id: d.Id,
            OrganizationId: d.OrganizationId,
            OrganizationName: d.OrganizationName,
            OrganizationLogoUrl: d.OrganizationLogoUrl,
            OrganizationType: d.OrganizationType,
            Title: d.Title,
            Slug: d.Slug,
            Summary: d.Summary,
            Status: d.Status,
            Visibility: d.Visibility,
            Eligibility: d.Eligibility,
            Currency: d.Currency,
            MinBudget: d.MinBudget,
            MaxBudget: d.MaxBudget,
            SubmissionDeadlineUtc: d.SubmissionDeadlineUtc,
            EstimatedDurationMonths: d.EstimatedDurationMonths,
            PrimaryDomains: d.Domains.Select(rd => rd.DomainName).ToList(),
            PublicAttachmentCount: d.Attachments.Count(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.PublicBriefing),
            ConfidentialAttachmentCount: d.Attachments.Count(a => a.ConfidentialityLevel == AttachmentConfidentialityLevel.ConfidentialNdaRequired),
            CreatedAtUtc: d.CreatedAtUtc
        );
    }

    private void InitPreviewData()
    {
        var id1 = Guid.Parse("11111111-2222-3333-4444-555555555551");
        var id2 = Guid.Parse("11111111-2222-3333-4444-555555555552");
        var orgId = Guid.Parse("99999999-8888-7777-6666-555555555555");

        _previewChallenges.Add(new ChallengeDetailDto(
            Id: id1,
            OrganizationId: orgId,
            OrganizationName: "AeroSpace Dynamics Ltd.",
            OrganizationLogoUrl: null,
            OrganizationType: OrganizationType.Company,
            IsOrganizationVerified: true,
            CreatedByUserId: Guid.NewGuid(),
            CreatorName: "Dr. Thomas Vance",
            Title: "Autonomous UAV Swarm Navigation in GPS-Denied Subterranean Environments",
            Slug: "autonomous-uav-swarm-navigation",
            Summary: "Seeking university research teams to develop novel decentralized SLAM algorithms for aerial drone swarms operating without satellite localization.",
            ProblemStatement: "Current single-agent drone platforms suffer catastrophic localization drift within 50 meters of subterranean entry. We need collaborative state estimation that scales across 4-8 drones with under 15ms inter-agent communication latency.",
            ResearchScope: "Subterranean tunnels, deep mine conduits, RF-attenuating disaster cavities.",
            ExpectedOutcomes: "1. Comprehensive simulation benchmarks in Gazebo/ROS2.\n2. Hardware-in-the-loop validation with optical flow and UWB transceivers.\n3. Complete source code with BSD/MIT commercial licensing option.",
            Status: ChallengeStatus.OpenForProposals,
            Visibility: ChallengeVisibility.Public,
            Eligibility: InstitutionalEligibility.OpenToAll,
            Currency: "USD",
            MinBudget: 75000,
            MaxBudget: 150000,
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(45),
            EstimatedDurationMonths: 12,
            RequiresManualNdaApproval: false,
            NdaTermsVersion: "1.0",
            NdaTermsText: "Standard Pavena Academic Industry NDA v1.0. All proprietary subterranean tunnel schematics disclosed under this challenge are strictly confidential.",
            HasSignedNda: false,
            IsNdaApproved: false,
            PublishedAtUtc: DateTime.UtcNow.AddDays(-10),
            ClosedAtUtc: null,
            CancelledAtUtc: null,
            CancellationReason: null,
            Domains: new List<ChallengeDomainDto>
            {
                new(Guid.NewGuid(), "Robotics & Autonomous Systems", "Multi-Agent UAVs", true),
                new(Guid.NewGuid(), "Computer Science", "Simultaneous Localization and Mapping", false)
            },
            RequiredExpertise: new List<ChallengeExpertiseDto>
            {
                new(Guid.NewGuid(), "ROS2 Humble / Iron", RequirementImportance.Required, "Strong proficiency in ROS2"),
                new(Guid.NewGuid(), "Nonlinear Factor Graph Optimization (GTSAM / Ceres)", RequirementImportance.Required, "Mathematical estimation background"),
                new(Guid.NewGuid(), "Ultra-Wideband (UWB) Mesh Ranging", RequirementImportance.Preferred, "Hardware prototyping")
            },
            Attachments: new List<ChallengeAttachmentDto>
            {
                new(Guid.NewGuid(), id1, "UAV_Swarm_Challenge_Brief_Public.pdf", ".pdf", "application/pdf", 1024 * 350, AttachmentConfidentialityLevel.PublicBriefing, "Public challenge brief", true, DateTime.UtcNow.AddDays(-10)),
                new(Guid.NewGuid(), id1, "Proprietary_Flight_Telemetry_Dataset_NDA.csv", ".csv", "text/csv", 1024 * 1024 * 8, AttachmentConfidentialityLevel.ConfidentialNdaRequired, "Flight telemetry under NDA", false, DateTime.UtcNow.AddDays(-10))
            },
            CreatedAtUtc: DateTime.UtcNow.AddDays(-14),
            UpdatedAtUtc: DateTime.UtcNow.AddDays(-10)
        ));

        _previewChallenges.Add(new ChallengeDetailDto(
            Id: id2,
            OrganizationId: orgId,
            OrganizationName: "BioHealth Analytics Corp",
            OrganizationLogoUrl: null,
            OrganizationType: OrganizationType.Company,
            IsOrganizationVerified: true,
            CreatedByUserId: Guid.NewGuid(),
            CreatorName: "Dr. Evelyn Reed",
            Title: "Explainable Deep Learning for Rare Pediatric Genomic Mutation Profiling",
            Slug: "explainable-deep-learning-genomics",
            Summary: "Collaborative industry challenge for bioinformaticians and machine learning researchers to uncover pathogenic structural variants in whole-genome sequencing.",
            ProblemStatement: "Standard variant effect predictors fail on rare missense variants that have zero population frequency in gnomAD.",
            ResearchScope: "Whole genome and exome sequencing datasets from pediatric cohorts.",
            ExpectedOutcomes: "Reproducible Python pipeline with Nextflow workflow, benchmarked against ClinVar and external pediatric cohort validations.",
            Status: ChallengeStatus.Published,
            Visibility: ChallengeVisibility.Public,
            Eligibility: InstitutionalEligibility.UniversitiesOnly,
            Currency: "EUR",
            MinBudget: 50000,
            MaxBudget: 120000,
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(60),
            EstimatedDurationMonths: 9,
            RequiresManualNdaApproval: true,
            NdaTermsVersion: "2.1",
            NdaTermsText: "BioHealth Special Health Data Confidentiality Protocol (HIPAA/GDPR compliance mandatory).",
            HasSignedNda: false,
            IsNdaApproved: false,
            PublishedAtUtc: DateTime.UtcNow.AddDays(-5),
            ClosedAtUtc: null,
            CancelledAtUtc: null,
            CancellationReason: null,
            Domains: new List<ChallengeDomainDto>
            {
                new(Guid.NewGuid(), "Bioinformatics", "Genomic Variant Annotation", true),
                new(Guid.NewGuid(), "Artificial Intelligence", "Graph Neural Networks", false)
            },
            RequiredExpertise: new List<ChallengeExpertiseDto>
            {
                new(Guid.NewGuid(), "PyTorch / PyTorch Geometric", RequirementImportance.Required, "GNN proficiency"),
                new(Guid.NewGuid(), "Variant Call Format (VCF) Processing", RequirementImportance.Required, "Genomics pipeline experience")
            },
            Attachments: new List<ChallengeAttachmentDto>
            {
                new(Guid.NewGuid(), id2, "BioHealth_Challenge_Overview.pdf", ".pdf", "application/pdf", 1024 * 512, AttachmentConfidentialityLevel.PublicBriefing, "Overview document", true, DateTime.UtcNow.AddDays(-5))
            },
            CreatedAtUtc: DateTime.UtcNow.AddDays(-7),
            UpdatedAtUtc: DateTime.UtcNow.AddDays(-5)
        ));
    }
}

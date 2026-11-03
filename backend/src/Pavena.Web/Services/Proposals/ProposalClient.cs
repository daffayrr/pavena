using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pavena.Application.Proposals.DTOs;
using Pavena.Domain.Enums;
using Pavena.Web.Services.Auth;

namespace Pavena.Web.Services.Proposals;

public class ProposalClient : IProposalClient
{
    private readonly HttpClient _httpClient;
    private readonly IScholarSessionService _sessionService;
    private readonly ILogger<ProposalClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProposalClient(
        HttpClient httpClient,
        IScholarSessionService sessionService,
        ILogger<ProposalClient>? logger = null)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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

    public async Task<ProposalDetailResponse?> CreateDraftAsync(CreateProposalDraftRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync("/api/v1/proposals", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> UpdateDraftAsync(Guid id, UpdateProposalDraftRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/proposals/{id}", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsync($"/api/v1/proposals/{id}/submit", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.GetAsync($"/api/v1/proposals/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<List<ProposalSummaryResponse>> GetSummariesAsync(
        Guid? challengeId = null,
        Guid? leadResearcherId = null,
        ProposalStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var query = $"/api/v1/proposals?page={page}&pageSize={pageSize}";
        if (challengeId.HasValue) query += $"&challengeId={challengeId.Value}";
        if (leadResearcherId.HasValue) query += $"&leadResearcherId={leadResearcherId.Value}";
        if (status.HasValue) query += $"&status={status.Value}";

        var response = await _httpClient.GetAsync(query, ct);
        if (!response.IsSuccessStatusCode) return new List<ProposalSummaryResponse>();
        return await response.Content.ReadFromJsonAsync<List<ProposalSummaryResponse>>(_jsonOptions, ct) ?? new List<ProposalSummaryResponse>();
    }

    public async Task<ProposalDetailResponse?> AddInvestigatorAsync(Guid id, AddInvestigatorRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/investigators", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> RemoveInvestigatorAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.DeleteAsync($"/api/v1/proposals/{id}/investigators/{userId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalAttachmentResponse?> UploadAttachmentAsync(
        Guid id,
        string fileName,
        byte[] fileBytes,
        string contentType,
        ProposalAttachmentType type = ProposalAttachmentType.TechnicalProposal,
        CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        using var form = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(fileBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(byteContent, "file", fileName);
        form.Add(new StringContent(type.ToString()), "type");

        var response = await _httpClient.PostAsync($"/api/v1/proposals/{id}/attachments", form, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalAttachmentResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> RemoveAttachmentAsync(Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.DeleteAsync($"/api/v1/proposals/{id}/attachments/{attachmentId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> ResubmitAsync(Guid id, ResubmitProposalRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/resubmit", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> WithdrawAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/withdraw", reason ?? "Withdrawn by researcher", _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<EligibilityCheckResult?> CheckEligibilityAsync(Guid id, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.GetAsync($"/api/v1/proposals/{id}/eligibility", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<EligibilityCheckResult>(_jsonOptions, ct);
    }

    public async Task<ProposalDetailResponse?> ReviewEligibilityAsync(Guid id, ReviewEligibilityRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/eligibility-review", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ReviewAssignmentResponse?> AssignReviewerAsync(Guid id, AssignReviewerRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/reviewers", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReviewAssignmentResponse>(_jsonOptions, ct);
    }

    public async Task<ReviewAssignmentResponse?> RespondToAssignmentAsync(Guid assignmentId, bool accept, string? declineReason, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var url = $"/api/v1/proposals/assignments/{assignmentId}/respond?accept={accept}";
        if (!string.IsNullOrEmpty(declineReason)) url += $"&declineReason={Uri.EscapeDataString(declineReason)}";
        var response = await _httpClient.PostAsync(url, null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReviewAssignmentResponse>(_jsonOptions, ct);
    }

    public async Task<ReviewAssignmentResponse?> DeclareCoiAsync(Guid assignmentId, DeclareCoiRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/assignments/{assignmentId}/coi", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReviewAssignmentResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalAssessmentResponse?> SubmitAssessmentAsync(Guid assignmentId, SubmitAssessmentRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/assignments/{assignmentId}/assessment", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalAssessmentResponse>(_jsonOptions, ct);
    }

    public async Task<List<ReviewAssignmentResponse>> GetMyReviewsAsync(CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.GetAsync("/api/v1/proposals/my-reviews", ct);
        if (!response.IsSuccessStatusCode) return new List<ReviewAssignmentResponse>();
        return await response.Content.ReadFromJsonAsync<List<ReviewAssignmentResponse>>(_jsonOptions, ct) ?? new List<ReviewAssignmentResponse>();
    }

    public async Task<ProposalDetailResponse?> ApplySponsorDecisionAsync(Guid id, SponsorDecisionRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/decision", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalDetailResponse>(_jsonOptions, ct);
    }

    public async Task<ProposalConversionResponse?> ConvertToProjectAsync(Guid id, ConvertProposalRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/proposals/{id}/convert", request, _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProposalConversionResponse>(_jsonOptions, ct);
    }
}

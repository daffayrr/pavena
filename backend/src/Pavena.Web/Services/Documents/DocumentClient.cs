using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pavena.Application.Documents.DTOs;
using Pavena.Web.Services.Auth;

namespace Pavena.Web.Services.Documents;

public class DocumentClient : IDocumentClient
{
    private readonly HttpClient _httpClient;
    private readonly IScholarSessionService _sessionService;
    private readonly ILogger<DocumentClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentClient(
        HttpClient httpClient,
        IScholarSessionService sessionService,
        ILogger<DocumentClient>? logger = null)
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

    public async Task<IReadOnlyList<DocumentSummaryDto>> GetUserDocumentsAsync(Guid? projectId = null, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var url = projectId.HasValue ? $"api/v1/documents?projectId={projectId.Value}" : "api/v1/documents";
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentSummaryDto>>(_jsonOptions, ct);
                return result ?? Array.Empty<DocumentSummaryDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load documents from backend.");
        }
        return Array.Empty<DocumentSummaryDto>();
    }

    public async Task<DocumentDetailDto?> GetDocumentByIdAsync(Guid id, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{id}", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentDetailDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get document {Id}.", id);
        }
        return null;
    }

    public async Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/documents", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentSummaryDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create document.");
        }
        return null;
    }

    public async Task<DocumentDetailDto?> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/documents/{id}", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentDetailDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to update document {Id}.", id);
        }
        return null;
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/documents/{id}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete document {Id}.", id);
            return false;
        }
    }

    // Collaborators
    public async Task<IReadOnlyList<CollaboratorDto>> GetCollaboratorsAsync(Guid documentId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/collaborators", ct);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<CollaboratorDto>>(_jsonOptions, ct);
                return list ?? Array.Empty<CollaboratorDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get collaborators for doc {DocumentId}.", documentId);
        }
        return Array.Empty<CollaboratorDto>();
    }

    public async Task<CollaboratorDto?> AddCollaboratorAsync(Guid documentId, AddCollaboratorRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/collaborators", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CollaboratorDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to add collaborator to doc {DocumentId}.", documentId);
        }
        return null;
    }

    public async Task<CollaboratorDto?> UpdateCollaboratorAsync(Guid documentId, Guid userId, UpdateCollaboratorRoleRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/documents/{documentId}/collaborators/{userId}", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CollaboratorDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to update collaborator role.");
        }
        return null;
    }

    public async Task<bool> RemoveCollaboratorAsync(Guid documentId, Guid userId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/documents/{documentId}/collaborators/{userId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove collaborator.");
            return false;
        }
    }

    // Files
    public async Task<DocumentFileDto?> CreateFileAsync(Guid documentId, CreateFileRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/files", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentFileDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create file.");
        }
        return null;
    }

    public async Task<string?> GetFileContentAsync(Guid documentId, Guid fileId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/files/{fileId}/content", ct);
            if (response.IsSuccessStatusCode)
            {
                var doc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions, ct);
                if (doc.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get file content.");
        }
        return null;
    }

    public async Task<DocumentFileDto?> UpdateFileContentAsync(Guid documentId, Guid fileId, string content, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var req = new UpdateFileContentRequest(content);
            var response = await _httpClient.PutAsJsonAsync($"api/v1/documents/{documentId}/files/{fileId}/content", req, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentFileDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to update file content.");
        }
        return null;
    }

    public async Task<DocumentFileDto?> RenameFileAsync(Guid documentId, Guid fileId, string newPath, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var req = new RenameFileRequest(newPath);
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/files/{fileId}/rename", req, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentFileDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to rename file.");
        }
        return null;
    }

    public async Task<bool> DeleteFileAsync(Guid documentId, Guid fileId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/documents/{documentId}/files/{fileId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete file.");
            return false;
        }
    }

    public async Task<bool> SetMainFileAsync(Guid documentId, Guid fileId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/documents/{documentId}/files/{fileId}/set-main", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to set main file.");
            return false;
        }
    }

    public async Task<DocumentFileDto?> UploadBinaryFileAsync(Guid documentId, string path, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(path), "path");

            var response = await _httpClient.PostAsync($"api/v1/documents/{documentId}/files/upload", content, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentFileDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to upload file.");
        }
        return null;
    }

    public async Task<Stream?> DownloadFileStreamAsync(Guid documentId, Guid fileId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/files/{fileId}/download", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to download file stream.");
        }
        return null;
    }

    // Compilation
    public async Task<CompilationJobDto?> TriggerCompilationAsync(Guid documentId, TriggerCompilationRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/compile", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CompilationJobDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to trigger compilation.");
        }
        return null;
    }

    public async Task<CompilationJobDto?> GetCompilationJobAsync(Guid documentId, Guid jobId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/compile/{jobId}", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CompilationJobDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get compilation job.");
        }
        return null;
    }

    public async Task<IReadOnlyList<CompilationJobDto>> GetCompilationHistoryAsync(Guid documentId, int limit = 20, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/compile/history?limit={limit}", ct);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<CompilationJobDto>>(_jsonOptions, ct);
                return list ?? Array.Empty<CompilationJobDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get compilation history.");
        }
        return Array.Empty<CompilationJobDto>();
    }

    public async Task<Stream?> DownloadPdfStreamAsync(Guid documentId, Guid jobId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/compile/{jobId}/pdf", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to download compiled PDF.");
        }
        return null;
    }

    // Versions
    public async Task<DocumentVersionDto?> CreateVersionAsync(Guid documentId, CreateVersionRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/versions", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentVersionDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create version.");
        }
        return null;
    }

    public async Task<IReadOnlyList<DocumentVersionDto>> GetVersionsAsync(Guid documentId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/versions", ct);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentVersionDto>>(_jsonOptions, ct);
                return list ?? Array.Empty<DocumentVersionDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get versions.");
        }
        return Array.Empty<DocumentVersionDto>();
    }

    public async Task<DocumentDetailDto?> RestoreVersionAsync(Guid documentId, Guid versionId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/documents/{documentId}/versions/{versionId}/restore", null, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentDetailDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to restore version.");
        }
        return null;
    }

    // Comments & Reviews
    public async Task<DocumentCommentDto?> CreateCommentAsync(Guid documentId, CreateCommentRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/comments", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentCommentDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create comment.");
        }
        return null;
    }

    public async Task<IReadOnlyList<DocumentCommentDto>> GetCommentsAsync(Guid documentId, Guid? fileId = null, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var url = fileId.HasValue ? $"api/v1/documents/{documentId}/comments?fileId={fileId.Value}" : $"api/v1/documents/{documentId}/comments";
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentCommentDto>>(_jsonOptions, ct);
                return list ?? Array.Empty<DocumentCommentDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get comments.");
        }
        return Array.Empty<DocumentCommentDto>();
    }

    public async Task<DocumentCommentDto?> ResolveCommentAsync(Guid documentId, Guid commentId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/documents/{documentId}/comments/{commentId}/resolve", null, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentCommentDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to resolve comment.");
        }
        return null;
    }

    public async Task<DocumentCommentDto?> ReopenCommentAsync(Guid documentId, Guid commentId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/documents/{documentId}/comments/{commentId}/reopen", null, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentCommentDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to reopen comment.");
        }
        return null;
    }

    public async Task<DocumentReviewDto?> RequestReviewAsync(Guid documentId, RequestReviewRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/reviews", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentReviewDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to request review.");
        }
        return null;
    }

    public async Task<DocumentReviewDto?> SubmitReviewDecisionAsync(Guid documentId, Guid reviewId, SubmitReviewDecisionRequest request, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/documents/{documentId}/reviews/{reviewId}/decision", request, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentReviewDto>(_jsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to submit review decision.");
        }
        return null;
    }

    public async Task<IReadOnlyList<DocumentReviewDto>> GetReviewsAsync(Guid documentId, CancellationToken ct = default)
    {
        SetAuthorizationHeader();
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/documents/{documentId}/reviews", ct);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentReviewDto>>(_jsonOptions, ct);
                return list ?? Array.Empty<DocumentReviewDto>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get reviews.");
        }
        return Array.Empty<DocumentReviewDto>();
    }
}

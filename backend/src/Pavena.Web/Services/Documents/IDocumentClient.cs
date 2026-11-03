using Pavena.Application.Documents.DTOs;

namespace Pavena.Web.Services.Documents;

public interface IDocumentClient
{
    Task<IReadOnlyList<DocumentSummaryDto>> GetUserDocumentsAsync(Guid? projectId = null, CancellationToken ct = default);
    Task<DocumentDetailDto?> GetDocumentByIdAsync(Guid id, CancellationToken ct = default);
    Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken ct = default);
    Task<DocumentDetailDto?> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken ct = default);
    Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default);

    // Collaborators
    Task<IReadOnlyList<CollaboratorDto>> GetCollaboratorsAsync(Guid documentId, CancellationToken ct = default);
    Task<CollaboratorDto?> AddCollaboratorAsync(Guid documentId, AddCollaboratorRequest request, CancellationToken ct = default);
    Task<CollaboratorDto?> UpdateCollaboratorAsync(Guid documentId, Guid userId, UpdateCollaboratorRoleRequest request, CancellationToken ct = default);
    Task<bool> RemoveCollaboratorAsync(Guid documentId, Guid userId, CancellationToken ct = default);

    // Files
    Task<DocumentFileDto?> CreateFileAsync(Guid documentId, CreateFileRequest request, CancellationToken ct = default);
    Task<string?> GetFileContentAsync(Guid documentId, Guid fileId, CancellationToken ct = default);
    Task<DocumentFileDto?> UpdateFileContentAsync(Guid documentId, Guid fileId, string content, CancellationToken ct = default);
    Task<DocumentFileDto?> RenameFileAsync(Guid documentId, Guid fileId, string newPath, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(Guid documentId, Guid fileId, CancellationToken ct = default);
    Task<bool> SetMainFileAsync(Guid documentId, Guid fileId, CancellationToken ct = default);
    Task<DocumentFileDto?> UploadBinaryFileAsync(Guid documentId, string path, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream?> DownloadFileStreamAsync(Guid documentId, Guid fileId, CancellationToken ct = default);

    // Compilation
    Task<CompilationJobDto?> TriggerCompilationAsync(Guid documentId, TriggerCompilationRequest request, CancellationToken ct = default);
    Task<CompilationJobDto?> GetCompilationJobAsync(Guid documentId, Guid jobId, CancellationToken ct = default);
    Task<IReadOnlyList<CompilationJobDto>> GetCompilationHistoryAsync(Guid documentId, int limit = 20, CancellationToken ct = default);
    Task<Stream?> DownloadPdfStreamAsync(Guid documentId, Guid jobId, CancellationToken ct = default);

    // Versions
    Task<DocumentVersionDto?> CreateVersionAsync(Guid documentId, CreateVersionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentVersionDto>> GetVersionsAsync(Guid documentId, CancellationToken ct = default);
    Task<DocumentDetailDto?> RestoreVersionAsync(Guid documentId, Guid versionId, CancellationToken ct = default);

    // Comments & Reviews
    Task<DocumentCommentDto?> CreateCommentAsync(Guid documentId, CreateCommentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentCommentDto>> GetCommentsAsync(Guid documentId, Guid? fileId = null, CancellationToken ct = default);
    Task<DocumentCommentDto?> ResolveCommentAsync(Guid documentId, Guid commentId, CancellationToken ct = default);
    Task<DocumentCommentDto?> ReopenCommentAsync(Guid documentId, Guid commentId, CancellationToken ct = default);
    Task<DocumentReviewDto?> RequestReviewAsync(Guid documentId, RequestReviewRequest request, CancellationToken ct = default);
    Task<DocumentReviewDto?> SubmitReviewDecisionAsync(Guid documentId, Guid reviewId, SubmitReviewDecisionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentReviewDto>> GetReviewsAsync(Guid documentId, CancellationToken ct = default);
}

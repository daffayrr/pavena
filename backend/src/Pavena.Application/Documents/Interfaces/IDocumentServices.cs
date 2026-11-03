using Pavena.Application.Documents.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Application.Documents.Interfaces;

public interface IDocumentService
{
    Task<DocumentSummaryDto> CreateDocumentAsync(Guid currentUserId, CreateDocumentRequest request, CancellationToken ct = default);
    Task<DocumentDetailDto?> GetDocumentByIdAsync(Guid currentUserId, Guid documentId, CancellationToken ct = default);
    Task<List<DocumentSummaryDto>> GetUserDocumentsAsync(Guid currentUserId, Guid? projectId = null, CancellationToken ct = default);
    Task<DocumentDetailDto> UpdateDocumentAsync(Guid currentUserId, Guid documentId, UpdateDocumentRequest request, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid currentUserId, Guid documentId, CancellationToken ct = default);
    Task<CollaboratorDto> AddCollaboratorAsync(Guid currentUserId, Guid documentId, AddCollaboratorRequest request, CancellationToken ct = default);
    Task<CollaboratorDto> UpdateCollaboratorAsync(Guid currentUserId, Guid documentId, Guid targetUserId, UpdateCollaboratorRoleRequest request, CancellationToken ct = default);
    Task RemoveCollaboratorAsync(Guid currentUserId, Guid documentId, Guid targetUserId, CancellationToken ct = default);
    Task<List<CollaboratorDto>> GetCollaboratorsAsync(Guid currentUserId, Guid documentId, CancellationToken ct = default);
}

public interface IDocumentFileSystemService
{
    Task<DocumentFileDto> CreateFileAsync(Guid currentUserId, Guid documentId, CreateFileRequest request, CancellationToken ct = default);
    Task<string> GetFileContentAsync(Guid currentUserId, Guid documentId, Guid fileId, CancellationToken ct = default);
    Task<DocumentFileDto> UpdateFileContentAsync(Guid currentUserId, Guid documentId, Guid fileId, string content, CancellationToken ct = default);
    Task<DocumentFileDto> UploadBinaryFileAsync(Guid currentUserId, Guid documentId, string path, Stream contentStream, string mimeType, CancellationToken ct = default);
    Task<Stream> DownloadFileStreamAsync(Guid currentUserId, Guid documentId, Guid fileId, CancellationToken ct = default);
    Task<DocumentFileDto> RenameFileAsync(Guid currentUserId, Guid documentId, Guid fileId, RenameFileRequest request, CancellationToken ct = default);
    Task DeleteFileAsync(Guid currentUserId, Guid documentId, Guid fileId, CancellationToken ct = default);
    Task SetMainFileAsync(Guid currentUserId, Guid documentId, Guid fileId, CancellationToken ct = default);
}

public interface ILatexCompilerService
{
    Task<CompilationJobDto> TriggerCompilationAsync(Guid currentUserId, Guid documentId, TriggerCompilationRequest request, CancellationToken ct = default);
    Task<CompilationJobDto?> GetCompilationJobAsync(Guid currentUserId, Guid documentId, Guid jobId, CancellationToken ct = default);
    Task<List<CompilationJobDto>> GetCompilationHistoryAsync(Guid currentUserId, Guid documentId, int limit = 20, CancellationToken ct = default);
    Task<Stream> DownloadPdfStreamAsync(Guid currentUserId, Guid documentId, Guid jobId, CancellationToken ct = default);
}

public interface IDocumentVersioningService
{
    Task<DocumentVersionDto> CreateVersionAsync(Guid currentUserId, Guid documentId, CreateVersionRequest request, CancellationToken ct = default);
    Task<List<DocumentVersionDto>> GetVersionsAsync(Guid currentUserId, Guid documentId, CancellationToken ct = default);
    Task<DocumentDetailDto> RestoreVersionAsync(Guid currentUserId, Guid documentId, Guid versionId, CancellationToken ct = default);
}

public interface IDocumentReviewService
{
    Task<DocumentCommentDto> CreateCommentAsync(Guid currentUserId, Guid documentId, CreateCommentRequest request, CancellationToken ct = default);
    Task<List<DocumentCommentDto>> GetCommentsAsync(Guid currentUserId, Guid documentId, Guid? fileId = null, CancellationToken ct = default);
    Task<DocumentCommentDto> ResolveCommentAsync(Guid currentUserId, Guid documentId, Guid commentId, CancellationToken ct = default);
    Task<DocumentCommentDto> ReopenCommentAsync(Guid currentUserId, Guid documentId, Guid commentId, CancellationToken ct = default);
    Task<DocumentReviewDto> RequestReviewAsync(Guid currentUserId, Guid documentId, RequestReviewRequest request, CancellationToken ct = default);
    Task<DocumentReviewDto> SubmitReviewDecisionAsync(Guid currentUserId, Guid documentId, Guid reviewId, SubmitReviewDecisionRequest request, CancellationToken ct = default);
    Task<List<DocumentReviewDto>> GetReviewsAsync(Guid currentUserId, Guid documentId, CancellationToken ct = default);
}

public interface IDocumentCollaborationService
{
    Task RecordCrdtUpdateAsync(Guid currentUserId, Guid documentId, Guid? fileId, byte[] updateBlob, string clientSessionId, CancellationToken ct = default);
    Task<byte[]> GetAggregatedCrdtStateAsync(Guid currentUserId, Guid documentId, Guid? fileId, CancellationToken ct = default);
    Task<bool> CanUserAccessDocumentAsync(Guid userId, Guid documentId, DocumentPermissionRole requiredRole = DocumentPermissionRole.Viewer, CancellationToken ct = default);
}

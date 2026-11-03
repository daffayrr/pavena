using Pavena.Domain.Enums;

namespace Pavena.Application.Documents.DTOs;

public record DocumentSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string DocumentType,
    EditorMode Mode,
    Guid? ProjectId,
    string? ProjectTitle,
    Guid OwnerUserId,
    string OwnerName,
    DocumentVisibility Visibility,
    DocumentStatus Status,
    Guid? MainFileId,
    string? MainFileName,
    DateTime? LastCompiledAtUtc,
    CompilationStatus? LastCompilationStatus,
    DateTime UpdatedAtUtc,
    DateTime CreatedAtUtc,
    int FileCount,
    int CollaboratorCount
);

public record DocumentDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string DocumentType,
    EditorMode Mode,
    Guid? ProjectId,
    string? ProjectTitle,
    Guid OwnerUserId,
    string OwnerName,
    DocumentVisibility Visibility,
    DocumentStatus Status,
    Guid? MainFileId,
    string? MainFileName,
    bool IsRealTimeEnabled,
    DateTime? LastCompiledAtUtc,
    CompilationStatus? LastCompilationStatus,
    string ConcurrencyStamp,
    DateTime UpdatedAtUtc,
    DateTime CreatedAtUtc,
    List<DocumentFileDto> Files,
    List<CollaboratorDto> Collaborators,
    string UserRole
);

public record CreateDocumentRequest(
    string Title,
    string? Description,
    string? DocumentType,
    EditorMode Mode,
    Guid? ProjectId,
    DocumentVisibility Visibility = DocumentVisibility.Private
);

public record UpdateDocumentRequest(
    string Title,
    string? Description,
    string? DocumentType,
    DocumentVisibility Visibility,
    string ConcurrencyStamp
);

public record DocumentFileDto(
    Guid Id,
    Guid DocumentId,
    string Path,
    string FileName,
    string FileExtension,
    string MimeType,
    long SizeBytes,
    string StorageKey,
    bool IsMain,
    bool IsBinary,
    DateTime UpdatedAtUtc,
    DateTime CreatedAtUtc
);

public record CreateFileRequest(
    string Path,
    string? Content,
    bool IsMain = false
);

public record UpdateFileContentRequest(
    string Content
);

public record RenameFileRequest(
    string NewPath
);

public record CollaboratorDto(
    Guid Id,
    Guid DocumentId,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    DocumentPermissionRole Role,
    DateTime GrantedAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    bool CanCompile,
    bool CanReview
);

public record AddCollaboratorRequest(
    Guid UserId,
    DocumentPermissionRole Role,
    DateTime? ExpiresAtUtc = null
);

public record UpdateCollaboratorRoleRequest(
    DocumentPermissionRole Role,
    DateTime? ExpiresAtUtc = null
);

public record DocumentVersionDto(
    Guid Id,
    Guid DocumentId,
    int VersionNumber,
    string Title,
    string? Description,
    DocumentVersionSource VersionSource,
    string ArchiveStorageKey,
    long ArchiveSizeBytes,
    string ChecksumSha256,
    bool IsRestorable,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAtUtc
);

public record CreateVersionRequest(
    string Title,
    string? Description,
    DocumentVersionSource Source = DocumentVersionSource.ManualSnapshot
);

public record CompilationJobDto(
    Guid Id,
    Guid DocumentId,
    Guid TriggeredByUserId,
    string TriggeredByUserName,
    CompilationEngine Engine,
    CompilationStatus Status,
    DateTime QueuedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    int? DurationMs,
    string? OutputLog,
    string? PdfDownloadUrl,
    long? PdfSizeBytes,
    string? ErrorMessage
);

public record TriggerCompilationRequest(
    CompilationEngine Engine = CompilationEngine.PdfLatex
);

public record DocumentCommentDto(
    Guid Id,
    Guid DocumentId,
    Guid? FileId,
    string? FilePath,
    int? LineNumber,
    string? SelectedText,
    string Content,
    CommentStatus Status,
    Guid? ParentCommentId,
    Guid AuthorUserId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    string? ResolvedByUserName,
    List<DocumentCommentDto> Replies
);

public record CreateCommentRequest(
    string Content,
    Guid? FileId = null,
    int? LineNumber = null,
    string? SelectedText = null,
    Guid? ParentCommentId = null
);

public record DocumentReviewDto(
    Guid Id,
    Guid DocumentId,
    Guid? DocumentVersionId,
    int? VersionNumber,
    Guid ReviewerUserId,
    string ReviewerUserName,
    Guid RequestedByUserId,
    string RequestedByUserName,
    ReviewStatus Status,
    string? SummaryNotes,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc
);

public record RequestReviewRequest(
    Guid ReviewerUserId,
    Guid? DocumentVersionId = null
);

public record SubmitReviewDecisionRequest(
    ReviewStatus Status,
    string? SummaryNotes
);

namespace Pavena.Application.Social.DTOs;

public record CreatePostRequest(
    string Content,
    string Visibility,
    bool PublishImmediately = true,
    List<CreateAttachmentRequest>? Attachments = null,
    string? Category = null
);

public record CreateAttachmentRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string StorageKey
);

public record UpdatePostRequest(
    string Content,
    string Visibility,
    string? Category = null
);

public record PostDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Content,
    string Visibility,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? EditedAtUtc,
    IReadOnlyList<PostAttachmentDto> Attachments,
    string Category = "GeneralAcademic",
    string? AuthorHeadline = null,
    string? AuthorInstitution = null
);

public record PostAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime CreatedAtUtc
);

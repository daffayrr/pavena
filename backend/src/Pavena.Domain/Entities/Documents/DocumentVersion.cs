using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DocumentVersionSource VersionSource { get; set; } = DocumentVersionSource.ManualSnapshot;
    public string ArchiveStorageKey { get; set; } = string.Empty;
    public long ArchiveSizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public bool IsRestorable { get; set; } = true;
    public Guid CreatedByUserId { get; set; }

    public virtual Document? Document { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public DocumentVersion()
    {
    }

    public static DocumentVersion Create(
        Guid documentId,
        int versionNumber,
        string title,
        string? description,
        DocumentVersionSource source,
        string archiveStorageKey,
        long archiveSizeBytes,
        string checksumSha256,
        Guid createdByUserId)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (versionNumber <= 0)
            throw new DomainException("Version number must be positive.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Version title cannot be empty.");

        if (string.IsNullOrWhiteSpace(archiveStorageKey))
            throw new DomainException("Archive storage key cannot be empty.");

        return new DocumentVersion
        {
            DocumentId = documentId,
            VersionNumber = versionNumber,
            Title = title.Trim(),
            Description = description?.Trim(),
            VersionSource = source,
            ArchiveStorageKey = archiveStorageKey,
            ArchiveSizeBytes = archiveSizeBytes,
            ChecksumSha256 = checksumSha256 ?? string.Empty,
            IsRestorable = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

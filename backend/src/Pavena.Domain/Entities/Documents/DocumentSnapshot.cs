using Pavena.Domain.Common;

namespace Pavena.Domain.Entities.Documents;

public class DocumentSnapshot : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid? FileId { get; set; }
    public long SnapshotSequenceNumber { get; set; }
    public long CompactedThroughSequence { get; set; }
    public string SnapshotStorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;

    public virtual Document? Document { get; set; }
    public virtual DocumentFile? File { get; set; }

    public DocumentSnapshot()
    {
    }

    public static DocumentSnapshot Create(
        Guid documentId,
        Guid? fileId,
        long snapshotSequenceNumber,
        long compactedThroughSequence,
        string snapshotStorageKey,
        long sizeBytes,
        string checksumSha256)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(snapshotStorageKey))
            throw new DomainException("Snapshot storage key cannot be empty.");

        return new DocumentSnapshot
        {
            DocumentId = documentId,
            FileId = fileId,
            SnapshotSequenceNumber = snapshotSequenceNumber,
            CompactedThroughSequence = compactedThroughSequence,
            SnapshotStorageKey = snapshotStorageKey,
            SizeBytes = sizeBytes,
            ChecksumSha256 = checksumSha256 ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

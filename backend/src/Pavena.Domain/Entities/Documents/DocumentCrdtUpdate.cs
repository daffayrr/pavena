using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;

namespace Pavena.Domain.Entities.Documents;

public class DocumentCrdtUpdate : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid? FileId { get; set; }
    public long SequenceNumber { get; set; }
    public byte[] UpdateBlob { get; set; } = Array.Empty<byte>();
    public string ClientSessionId { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }

    public virtual Document? Document { get; set; }
    public virtual DocumentFile? File { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public DocumentCrdtUpdate()
    {
    }

    public static DocumentCrdtUpdate Create(
        Guid documentId,
        Guid? fileId,
        long sequenceNumber,
        byte[] updateBlob,
        string clientSessionId,
        Guid createdByUserId)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (updateBlob == null || updateBlob.Length == 0)
            throw new DomainException("Update blob cannot be empty.");

        return new DocumentCrdtUpdate
        {
            DocumentId = documentId,
            FileId = fileId,
            SequenceNumber = sequenceNumber,
            UpdateBlob = updateBlob,
            ClientSessionId = clientSessionId ?? string.Empty,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

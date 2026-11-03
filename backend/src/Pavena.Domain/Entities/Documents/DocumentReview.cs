using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class DocumentReview : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid? DocumentVersionId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public string? SummaryNotes { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public virtual Document? Document { get; set; }
    public virtual DocumentVersion? DocumentVersion { get; set; }
    public virtual User? ReviewerUser { get; set; }
    public virtual User? RequestedByUser { get; set; }

    public DocumentReview()
    {
    }

    public static DocumentReview Create(
        Guid documentId,
        Guid reviewerUserId,
        Guid requestedByUserId,
        Guid? documentVersionId = null)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (reviewerUserId == Guid.Empty)
            throw new DomainException("Reviewer user ID cannot be empty.");

        if (requestedByUserId == Guid.Empty)
            throw new DomainException("Requested by user ID cannot be empty.");

        return new DocumentReview
        {
            DocumentId = documentId,
            ReviewerUserId = reviewerUserId,
            RequestedByUserId = requestedByUserId,
            DocumentVersionId = documentVersionId,
            Status = ReviewStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void SubmitDecision(ReviewStatus status, string? summaryNotes)
    {
        if (status == ReviewStatus.Pending)
            throw new DomainException("Cannot submit a Pending review decision.");

        Status = status;
        SummaryNotes = summaryNotes?.Trim();
        DecidedAtUtc = DateTime.UtcNow;
    }
}

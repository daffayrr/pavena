using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class DocumentComment : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid? FileId { get; set; }
    public int? LineNumber { get; set; }
    public string? SelectedText { get; set; }
    public string Content { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Active;
    public Guid? ParentCommentId { get; set; }
    public Guid AuthorUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    public virtual Document? Document { get; set; }
    public virtual DocumentFile? File { get; set; }
    public virtual User? AuthorUser { get; set; }
    public virtual User? ResolvedByUser { get; set; }
    public virtual DocumentComment? ParentComment { get; set; }
    public virtual ICollection<DocumentComment> Replies { get; set; } = new List<DocumentComment>();

    public DocumentComment()
    {
    }

    public static DocumentComment Create(
        Guid documentId,
        Guid authorUserId,
        string content,
        Guid? fileId = null,
        int? lineNumber = null,
        string? selectedText = null,
        Guid? parentCommentId = null)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (authorUserId == Guid.Empty)
            throw new DomainException("Author ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment content cannot be empty.");

        return new DocumentComment
        {
            DocumentId = documentId,
            AuthorUserId = authorUserId,
            Content = content.Trim(),
            FileId = fileId,
            LineNumber = lineNumber,
            SelectedText = selectedText?.Trim(),
            ParentCommentId = parentCommentId,
            Status = CommentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Resolve(Guid resolvedByUserId)
    {
        Status = CommentStatus.Resolved;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAtUtc = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = CommentStatus.Active;
        ResolvedByUserId = null;
        ResolvedAtUtc = null;
    }
}

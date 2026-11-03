using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public EditorMode Mode { get; private set; } = EditorMode.Wysiwyg;
    public Guid? ProjectId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.Private;
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;
    public int SchemaVersion { get; set; } = 1;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual User? OwnerUser { get; set; }
    public virtual ResearchProject? Project { get; set; }
    public virtual ICollection<DocumentCollaborator> Collaborators { get; set; } = new List<DocumentCollaborator>();

    public Document()
    {
    }

    public static Document Create(string title, EditorMode mode, Guid ownerUserId, Guid? projectId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Document title cannot be empty.");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Document must have a valid owner user ID.");

        return new Document
        {
            Title = title,
            Mode = mode,
            OwnerUserId = ownerUserId,
            ProjectId = projectId,
            Visibility = DocumentVisibility.Private,
            Status = DocumentStatus.Active,
            SchemaVersion = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}

public class DocumentCollaborator : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DocumentPermissionRole Role { get; set; } = DocumentPermissionRole.Viewer;
    public Guid GrantedByUserId { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Document? Document { get; set; }
    public virtual User? User { get; set; }
    public virtual User? GrantedByUser { get; set; }

    public DocumentCollaborator()
    {
    }

    public DocumentCollaborator(Guid documentId, Guid userId, DocumentPermissionRole role, Guid grantedByUserId)
    {
        DocumentId = documentId;
        UserId = userId;
        Role = role;
        GrantedByUserId = grantedByUserId;
        GrantedAtUtc = DateTime.UtcNow;
    }
}

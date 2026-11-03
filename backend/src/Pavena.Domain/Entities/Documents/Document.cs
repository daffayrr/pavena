using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DocumentType { get; set; } = "Manuscript";
    public EditorMode Mode { get; private set; } = EditorMode.Latex;
    public Guid? ProjectId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.Private;
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;
    public int SchemaVersion { get; set; } = 1;
    public Guid? MainFileId { get; set; }
    public bool IsRealTimeEnabled { get; set; } = true;
    public DateTime? LastCompiledAtUtc { get; set; }
    public CompilationStatus? LastCompilationStatus { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual User? OwnerUser { get; set; }
    public virtual ResearchProject? Project { get; set; }
    public virtual DocumentFile? MainFile { get; set; }
    public virtual ICollection<DocumentCollaborator> Collaborators { get; set; } = new List<DocumentCollaborator>();
    public virtual ICollection<DocumentFile> Files { get; set; } = new List<DocumentFile>();
    public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public virtual ICollection<DocumentCrdtUpdate> CrdtUpdates { get; set; } = new List<DocumentCrdtUpdate>();
    public virtual ICollection<DocumentSnapshot> Snapshots { get; set; } = new List<DocumentSnapshot>();
    public virtual ICollection<CompilationJob> CompilationJobs { get; set; } = new List<CompilationJob>();
    public virtual ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();
    public virtual ICollection<DocumentReview> Reviews { get; set; } = new List<DocumentReview>();

    public Document()
    {
    }

    public static Document Create(string title, EditorMode mode, Guid ownerUserId, Guid? projectId = null, string? description = null, string? documentType = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Document title cannot be empty.");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Document must have a valid owner user ID.");

        return new Document
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            DocumentType = string.IsNullOrWhiteSpace(documentType) ? "Manuscript" : documentType.Trim(),
            Mode = mode,
            OwnerUserId = ownerUserId,
            ProjectId = projectId,
            Visibility = DocumentVisibility.Private,
            Status = DocumentStatus.Active,
            SchemaVersion = 1,
            IsRealTimeEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateMetadata(string title, string? description, DocumentVisibility visibility, string? documentType = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Document title cannot be empty.");

        Title = title.Trim();
        Description = description?.Trim();
        Visibility = visibility;
        if (!string.IsNullOrWhiteSpace(documentType))
        {
            DocumentType = documentType.Trim();
        }
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void SetMainFile(Guid fileId)
    {
        MainFileId = fileId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordCompilationResult(CompilationStatus status)
    {
        LastCompilationStatus = status;
        LastCompiledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = DocumentStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void Trash()
    {
        Status = DocumentStatus.Trashed;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void Restore()
    {
        Status = DocumentStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }
}

public class DocumentCollaborator : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DocumentPermissionRole Role { get; set; } = DocumentPermissionRole.Viewer;
    public Guid GrantedByUserId { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool CanCompile { get; set; } = true;
    public bool CanReview { get; set; } = true;

    public virtual Document? Document { get; set; }
    public virtual User? User { get; set; }
    public virtual User? GrantedByUser { get; set; }

    public DocumentCollaborator()
    {
    }

    public DocumentCollaborator(Guid documentId, Guid userId, DocumentPermissionRole role, Guid grantedByUserId, DateTime? expiresAtUtc = null)
    {
        DocumentId = documentId;
        UserId = userId;
        Role = role;
        GrantedByUserId = grantedByUserId;
        GrantedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        CanCompile = role != DocumentPermissionRole.Viewer;
        CanReview = true;
    }

    public bool IsActive()
    {
        if (RevokedAtUtc.HasValue) return false;
        if (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTime.UtcNow) return false;
        return true;
    }

    public void Revoke()
    {
        RevokedAtUtc = DateTime.UtcNow;
    }
}

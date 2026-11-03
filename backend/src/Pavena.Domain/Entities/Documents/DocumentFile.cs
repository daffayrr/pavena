using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;

namespace Pavena.Domain.Entities.Documents;

public class DocumentFile : BaseEntity
{
    public Guid DocumentId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = "text/plain";
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsBinary { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Document? Document { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public DocumentFile()
    {
    }

    public static DocumentFile Create(
        Guid documentId,
        string path,
        string fileName,
        string fileExtension,
        string mimeType,
        long sizeBytes,
        string storageKey,
        bool isMain,
        bool isBinary,
        Guid createdByUserId)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(path))
            throw new DomainException("File path cannot be empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name cannot be empty.");

        // Normalize path: unix separators, trim leading/trailing slashes
        var normalizedPath = path.Replace('\\', '/').Trim('/');

        return new DocumentFile
        {
            DocumentId = documentId,
            Path = normalizedPath,
            FileName = fileName.Trim(),
            FileExtension = fileExtension.Trim().ToLowerInvariant(),
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            IsMain = isMain,
            IsBinary = isBinary,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateStorage(string storageKey, long sizeBytes)
    {
        StorageKey = storageKey;
        SizeBytes = sizeBytes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetMain(bool isMain)
    {
        IsMain = isMain;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string newPath, string newFileName)
    {
        if (string.IsNullOrWhiteSpace(newPath))
            throw new DomainException("New path cannot be empty.");

        if (string.IsNullOrWhiteSpace(newFileName))
            throw new DomainException("New file name cannot be empty.");

        Path = newPath.Replace('\\', '/').Trim('/');
        FileName = newFileName.Trim();
        FileExtension = System.IO.Path.GetExtension(FileName).ToLowerInvariant();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

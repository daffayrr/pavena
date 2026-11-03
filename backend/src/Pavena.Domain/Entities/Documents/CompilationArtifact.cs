using Pavena.Domain.Common;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class CompilationArtifact : BaseEntity
{
    public Guid CompilationJobId { get; set; }
    public CompilationArtifactType ArtifactType { get; set; } = CompilationArtifactType.Pdf;
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public virtual CompilationJob? CompilationJob { get; set; }

    public CompilationArtifact()
    {
    }

    public static CompilationArtifact Create(
        Guid compilationJobId,
        CompilationArtifactType artifactType,
        string fileName,
        string storageKey,
        long sizeBytes)
    {
        if (compilationJobId == Guid.Empty)
            throw new DomainException("CompilationJobId cannot be empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("Storage key cannot be empty.");

        return new CompilationArtifact
        {
            CompilationJobId = compilationJobId,
            ArtifactType = artifactType,
            FileName = fileName.Trim(),
            StorageKey = storageKey,
            SizeBytes = sizeBytes,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

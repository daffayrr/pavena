using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Documents.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Documents;

public class DocumentVersioningService : IDocumentVersioningService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentVersioningService> _logger;

    public DocumentVersioningService(
        IPavenaDbContext dbContext,
        IObjectStorageService storageService,
        IDocumentService documentService,
        ILogger<DocumentVersioningService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<DocumentVersionDto> CreateVersionAsync(
        Guid currentUserId,
        Guid documentId,
        CreateVersionRequest request,
        CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Collaborators)
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (doc == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = doc.OwnerUserId == currentUserId;
        var isEditor = doc.Collaborators.Any(c => c.UserId == currentUserId && c.IsActive() &&
                                                 (c.Role == DocumentPermissionRole.Owner || c.Role == DocumentPermissionRole.Editor));

        if (!isOwner && !isEditor)
            throw new UnauthorizedAccessException("You do not have permission to create versions for this document.");

        var nextVersionNum = (await _dbContext.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0) + 1;

        // Archive all files to a zip stream in memory
        using var zipMemory = new MemoryStream();
        using (var archive = new ZipArchive(zipMemory, ZipArchiveMode.Create, true))
        {
            foreach (var file in doc.Files)
            {
                var entry = archive.CreateEntry(file.Path, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                try
                {
                    using var fileStream = await _storageService.GetObjectStreamAsync(file.StorageKey, ct);
                    await fileStream.CopyToAsync(entryStream, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read file {StorageKey} during versioning of doc {DocumentId}.", file.StorageKey, documentId);
                }
            }
        }

        zipMemory.Position = 0;
        var archiveBytes = zipMemory.ToArray();

        string checksum;
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(archiveBytes);
            checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        var storageKey = $"documents/{documentId}/versions/{nextVersionNum}/snapshot.zip";
        using (var uploadStream = new MemoryStream(archiveBytes))
        {
            await _storageService.SaveObjectAsync(storageKey, uploadStream, "application/zip", ct);
        }

        var version = DocumentVersion.Create(
            documentId,
            nextVersionNum,
            request.Title,
            request.Description,
            request.Source,
            storageKey,
            archiveBytes.Length,
            checksum,
            currentUserId
        );

        _dbContext.DocumentVersions.Add(version);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("DocumentVersion v{VersionNumber} created for document {DocumentId}.", nextVersionNum, documentId);

        var user = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        var userName = user?.Profile?.DisplayName ?? user?.UserName ?? "Author";

        return new DocumentVersionDto(
            version.Id,
            version.DocumentId,
            version.VersionNumber,
            version.Title,
            version.Description,
            version.VersionSource,
            version.ArchiveStorageKey,
            version.ArchiveSizeBytes,
            version.ChecksumSha256,
            version.IsRestorable,
            version.CreatedByUserId,
            userName,
            version.CreatedAtUtc
        );
    }

    public async Task<List<DocumentVersionDto>> GetVersionsAsync(
        Guid currentUserId,
        Guid documentId,
        CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .AsNoTracking()
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (doc == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        if (doc.OwnerUserId != currentUserId && doc.Visibility != DocumentVisibility.Public)
        {
            var isCollab = doc.Collaborators.Any(c => c.UserId == currentUserId && c.IsActive());
            if (!isCollab)
                throw new KeyNotFoundException($"Document with ID {documentId} was not found.");
        }

        var versions = await _dbContext.DocumentVersions
            .AsNoTracking()
            .Include(v => v.CreatedByUser)
                .ThenInclude(u => u!.Profile)
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        return versions.Select(v => new DocumentVersionDto(
            v.Id,
            v.DocumentId,
            v.VersionNumber,
            v.Title,
            v.Description,
            v.VersionSource,
            v.ArchiveStorageKey,
            v.ArchiveSizeBytes,
            v.ChecksumSha256,
            v.IsRestorable,
            v.CreatedByUserId,
            v.CreatedByUser?.Profile?.DisplayName ?? v.CreatedByUser?.UserName ?? "Author",
            v.CreatedAtUtc
        )).ToList();
    }

    public async Task<DocumentDetailDto> RestoreVersionAsync(
        Guid currentUserId,
        Guid documentId,
        Guid versionId,
        CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Collaborators)
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (doc == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = doc.OwnerUserId == currentUserId ||
                      doc.Collaborators.Any(c => c.UserId == currentUserId && c.Role == DocumentPermissionRole.Owner && c.IsActive());

        if (!isOwner)
            throw new UnauthorizedAccessException("Only document owners can restore historical versions.");

        var version = await _dbContext.DocumentVersions
            .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId, ct);

        if (version == null)
            throw new KeyNotFoundException($"Version with ID {versionId} was not found.");

        if (!version.IsRestorable)
            throw new DomainException("This version is marked as non-restorable.");

        // First capture a pre-restore backup version if needed
        try
        {
            await CreateVersionAsync(currentUserId, documentId, new CreateVersionRequest(
                $"Pre-restore snapshot before reverting to v{version.VersionNumber}",
                $"Automatic safeguard snapshot before restoring version {version.VersionNumber}",
                DocumentVersionSource.AutoSave
            ), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create pre-restore snapshot for document {DocumentId}.", documentId);
        }

        // Download and unpack archive
        using var archiveStream = await _storageService.GetObjectStreamAsync(version.ArchiveStorageKey, ct);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name)) continue; // Directory entry

            var normalizedPath = entry.FullName.Replace('\\', '/').Trim('/');
            var fileName = Path.GetFileName(normalizedPath);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            var existingFile = doc.Files.FirstOrDefault(f => f.Path == normalizedPath);
            using var entryContent = entry.Open();
            using var mem = new MemoryStream();
            await entryContent.CopyToAsync(mem, ct);
            mem.Position = 0;

            if (existingFile != null)
            {
                await _storageService.SaveObjectAsync(existingFile.StorageKey, mem, existingFile.MimeType, ct);
                existingFile.UpdateStorage(existingFile.StorageKey, mem.Length);
            }
            else
            {
                var newFileId = Guid.NewGuid();
                var storageKey = $"documents/{documentId}/files/{newFileId}/{fileName}";
                var mimeType = ext == ".tex" ? "application/x-tex" : "application/octet-stream";
                await _storageService.SaveObjectAsync(storageKey, mem, mimeType, ct);

                var newFile = DocumentFile.Create(
                    documentId,
                    normalizedPath,
                    fileName,
                    ext,
                    mimeType,
                    mem.Length,
                    storageKey,
                    fileName.Equals("main.tex", StringComparison.OrdinalIgnoreCase),
                    ext != ".tex" && ext != ".bib" && ext != ".json" && ext != ".md" && ext != ".txt",
                    currentUserId
                );
                _dbContext.DocumentFiles.Add(newFile);
            }
        }

        doc.UpdatedAtUtc = DateTime.UtcNow;
        doc.ConcurrencyStamp = Guid.NewGuid().ToString();

        // Create a forward version indicating restoration
        var nextVersionNum = (await _dbContext.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0) + 1;

        var restoredForwardVersion = DocumentVersion.Create(
            documentId,
            nextVersionNum,
            $"Restored from v{version.VersionNumber}: {version.Title}",
            $"Restored document content from version {version.VersionNumber}",
            DocumentVersionSource.ManualSnapshot,
            version.ArchiveStorageKey,
            version.ArchiveSizeBytes,
            version.ChecksumSha256,
            currentUserId
        );
        _dbContext.DocumentVersions.Add(restoredForwardVersion);

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Document {DocumentId} restored to version {VersionNumber} (forward v{ForwardVersionNumber}).",
            documentId, version.VersionNumber, nextVersionNum);

        return (await _documentService.GetDocumentByIdAsync(currentUserId, documentId, ct))!;
    }
}

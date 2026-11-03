using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Documents.Interfaces;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Documents;

public class DocumentCollaborationService : IDocumentCollaborationService
{
    private const int SnapshotCompactionThreshold = 100;

    private readonly IPavenaDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DocumentCollaborationService> _logger;

    public DocumentCollaborationService(
        IPavenaDbContext dbContext,
        IObjectStorageService storageService,
        ILogger<DocumentCollaborationService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<bool> CanUserAccessDocumentAsync(
        Guid userId,
        Guid documentId,
        DocumentPermissionRole requiredRole = DocumentPermissionRole.Viewer,
        CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .AsNoTracking()
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (doc == null || doc.Status == DocumentStatus.Trashed)
            return false;

        if (doc.OwnerUserId == userId)
            return true;

        if (requiredRole == DocumentPermissionRole.Viewer && doc.Visibility == DocumentVisibility.Public)
            return true;

        var collab = doc.Collaborators.FirstOrDefault(c => c.UserId == userId && c.IsActive());
        if (collab == null)
            return false;

        return requiredRole switch
        {
            DocumentPermissionRole.Viewer => true,
            DocumentPermissionRole.Reviewer => collab.Role != DocumentPermissionRole.Viewer,
            DocumentPermissionRole.Editor => collab.Role == DocumentPermissionRole.Owner || collab.Role == DocumentPermissionRole.Editor,
            DocumentPermissionRole.Owner => collab.Role == DocumentPermissionRole.Owner,
            _ => false
        };
    }

    public async Task RecordCrdtUpdateAsync(
        Guid currentUserId,
        Guid documentId,
        Guid? fileId,
        byte[] updateBlob,
        string clientSessionId,
        CancellationToken ct = default)
    {
        var canEdit = await CanUserAccessDocumentAsync(currentUserId, documentId, DocumentPermissionRole.Editor, ct);
        if (!canEdit)
            throw new UnauthorizedAccessException("User does not have write permissions for this document.");

        var lastSeq = await _dbContext.DocumentCrdtUpdates
            .Where(u => u.DocumentId == documentId && u.FileId == fileId)
            .MaxAsync(u => (long?)u.SequenceNumber, ct) ?? 0;

        var nextSeq = lastSeq + 1;

        var update = DocumentCrdtUpdate.Create(
            documentId,
            fileId,
            nextSeq,
            updateBlob,
            clientSessionId,
            currentUserId
        );

        _dbContext.DocumentCrdtUpdates.Add(update);
        await _dbContext.SaveChangesAsync(ct);

        // Compact snapshot if threshold reached
        if (nextSeq % SnapshotCompactionThreshold == 0)
        {
            try
            {
                await CreateCompactedSnapshotAsync(documentId, fileId, nextSeq, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background snapshot compaction failed for document {DocumentId}.", documentId);
            }
        }
    }

    public async Task<byte[]> GetAggregatedCrdtStateAsync(
        Guid currentUserId,
        Guid documentId,
        Guid? fileId,
        CancellationToken ct = default)
    {
        var canRead = await CanUserAccessDocumentAsync(currentUserId, documentId, DocumentPermissionRole.Viewer, ct);
        if (!canRead)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        // Check latest snapshot
        var latestSnapshot = await _dbContext.DocumentSnapshots
            .AsNoTracking()
            .Where(s => s.DocumentId == documentId && s.FileId == fileId)
            .OrderByDescending(s => s.SnapshotSequenceNumber)
            .FirstOrDefaultAsync(ct);

        long startSeq = 0;
        var resultList = new List<byte>();

        if (latestSnapshot != null)
        {
            startSeq = latestSnapshot.CompactedThroughSequence;
            try
            {
                using var snapshotStream = await _storageService.GetObjectStreamAsync(latestSnapshot.SnapshotStorageKey, ct);
                using var mem = new MemoryStream();
                await snapshotStream.CopyToAsync(mem, ct);
                resultList.AddRange(mem.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load snapshot {SnapshotKey}, aggregating from zero.", latestSnapshot.SnapshotStorageKey);
                startSeq = 0;
                resultList.Clear();
            }
        }

        var trailingUpdates = await _dbContext.DocumentCrdtUpdates
            .AsNoTracking()
            .Where(u => u.DocumentId == documentId && u.FileId == fileId && u.SequenceNumber > startSeq)
            .OrderBy(u => u.SequenceNumber)
            .ToListAsync(ct);

        foreach (var update in trailingUpdates)
        {
            resultList.AddRange(update.UpdateBlob);
        }

        return resultList.ToArray();
    }

    private async Task CreateCompactedSnapshotAsync(Guid documentId, Guid? fileId, long currentSequence, CancellationToken ct)
    {
        var allUpdates = await _dbContext.DocumentCrdtUpdates
            .AsNoTracking()
            .Where(u => u.DocumentId == documentId && u.FileId == fileId && u.SequenceNumber <= currentSequence)
            .OrderBy(u => u.SequenceNumber)
            .ToListAsync(ct);

        if (allUpdates.Count == 0) return;

        using var mem = new MemoryStream();
        foreach (var u in allUpdates)
        {
            mem.Write(u.UpdateBlob, 0, u.UpdateBlob.Length);
        }

        var aggregatedBytes = mem.ToArray();
        var snapshotId = Guid.NewGuid();
        var storageKey = $"documents/{documentId}/snapshots/{snapshotId}.bin";

        using (var uploadStream = new MemoryStream(aggregatedBytes))
        {
            await _storageService.SaveObjectAsync(storageKey, uploadStream, "application/octet-stream", ct);
        }

        string checksum;
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(aggregatedBytes);
            checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        var snapshot = DocumentSnapshot.Create(
            documentId,
            fileId,
            currentSequence,
            currentSequence,
            storageKey,
            aggregatedBytes.Length,
            checksum
        );

        _dbContext.DocumentSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Compacted snapshot created for doc {DocumentId} through seq {Seq}.", documentId, currentSequence);
    }
}

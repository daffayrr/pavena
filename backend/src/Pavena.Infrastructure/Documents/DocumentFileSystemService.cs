using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Documents.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Documents;

public class DocumentFileSystemService : IDocumentFileSystemService
{
    private static readonly HashSet<string> AllowedBinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".pdf", ".eps", ".svg", ".bib", ".sty", ".cls", ".bst", ".csv", ".tsv", ".dat", ".txt"
    };

    private readonly IPavenaDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DocumentFileSystemService> _logger;

    public DocumentFileSystemService(
        IPavenaDbContext dbContext,
        IObjectStorageService storageService,
        ILogger<DocumentFileSystemService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<DocumentFileDto> CreateFileAsync(
        Guid currentUserId,
        Guid documentId,
        CreateFileRequest request,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var normalizedPath = NormalizePath(request.Path);
        var fileName = Path.GetFileName(normalizedPath);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("Invalid file path specified.");

        var alreadyExists = await _dbContext.DocumentFiles
            .AnyAsync(f => f.DocumentId == documentId && f.Path == normalizedPath, ct);

        if (alreadyExists)
            throw new DomainException($"A file with path '{normalizedPath}' already exists in this document.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var mimeType = ext switch
        {
            ".tex" => "application/x-tex",
            ".bib" => "application/x-bibtex",
            ".sty" => "text/x-tex",
            ".cls" => "text/x-tex",
            ".json" => "application/json",
            ".md" => "text/markdown",
            _ => "text/plain"
        };

        var content = request.Content ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(content);
        var fileId = Guid.NewGuid();
        var storageKey = $"documents/{documentId}/files/{fileId}/{fileName}";

        using (var memStream = new MemoryStream(bytes))
        {
            await _storageService.SaveObjectAsync(storageKey, memStream, mimeType, ct);
        }

        var file = DocumentFile.Create(
            documentId,
            normalizedPath,
            fileName,
            ext,
            mimeType,
            bytes.Length,
            storageKey,
            request.IsMain,
            false,
            currentUserId
        );

        _dbContext.DocumentFiles.Add(file);

        if (request.IsMain)
        {
            var otherFiles = await _dbContext.DocumentFiles
                .Where(f => f.DocumentId == documentId && f.IsMain)
                .ToListAsync(ct);

            foreach (var other in otherFiles)
            {
                other.IsMain = false;
            }

            var doc = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
            if (doc != null)
            {
                doc.SetMainFile(file.Id);
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("File {Path} created in document {DocumentId} by user {UserId}.", normalizedPath, documentId, currentUserId);

        return MapToFileDto(file);
    }

    public async Task<string> GetFileContentAsync(
        Guid currentUserId,
        Guid documentId,
        Guid fileId,
        CancellationToken ct = default)
    {
        await AssertUserCanReadAsync(currentUserId, documentId, ct);

        var file = await _dbContext.DocumentFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id == fileId, ct);

        if (file == null)
            throw new KeyNotFoundException($"File with ID {fileId} was not found.");

        if (file.IsBinary)
            throw new DomainException("Cannot read binary file as UTF-8 text string.");

        using var stream = await _storageService.GetObjectStreamAsync(file.StorageKey, ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }

    public async Task<DocumentFileDto> UpdateFileContentAsync(
        Guid currentUserId,
        Guid documentId,
        Guid fileId,
        string content,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var file = await _dbContext.DocumentFiles
            .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id == fileId, ct);

        if (file == null)
            throw new KeyNotFoundException($"File with ID {fileId} was not found.");

        if (file.IsBinary)
            throw new DomainException("Cannot update binary file using text content.");

        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        using (var memStream = new MemoryStream(bytes))
        {
            await _storageService.SaveObjectAsync(file.StorageKey, memStream, file.MimeType, ct);
        }

        file.UpdateStorage(file.StorageKey, bytes.Length);

        var doc = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc != null)
        {
            doc.UpdatedAtUtc = DateTime.UtcNow;
            doc.ConcurrencyStamp = Guid.NewGuid().ToString();
        }

        await _dbContext.SaveChangesAsync(ct);

        return MapToFileDto(file);
    }

    public async Task<DocumentFileDto> UploadBinaryFileAsync(
        Guid currentUserId,
        Guid documentId,
        string path,
        Stream contentStream,
        string mimeType,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var normalizedPath = NormalizePath(path);
        var fileName = Path.GetFileName(normalizedPath);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (!AllowedBinaryExtensions.Contains(ext))
            throw new DomainException($"File extension '{ext}' is not permitted for upload.");

        var alreadyExists = await _dbContext.DocumentFiles
            .AnyAsync(f => f.DocumentId == documentId && f.Path == normalizedPath, ct);

        if (alreadyExists)
            throw new DomainException($"A file with path '{normalizedPath}' already exists.");

        var fileId = Guid.NewGuid();
        var storageKey = $"documents/{documentId}/files/{fileId}/{fileName}";

        await _storageService.SaveObjectAsync(storageKey, contentStream, mimeType, ct);

        long sizeBytes = contentStream.CanSeek ? contentStream.Length : 0;

        var file = DocumentFile.Create(
            documentId,
            normalizedPath,
            fileName,
            ext,
            mimeType,
            sizeBytes,
            storageKey,
            false,
            true,
            currentUserId
        );

        _dbContext.DocumentFiles.Add(file);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Binary file {Path} uploaded to document {DocumentId}.", normalizedPath, documentId);

        return MapToFileDto(file);
    }

    public async Task<Stream> DownloadFileStreamAsync(
        Guid currentUserId,
        Guid documentId,
        Guid fileId,
        CancellationToken ct = default)
    {
        await AssertUserCanReadAsync(currentUserId, documentId, ct);

        var file = await _dbContext.DocumentFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id == fileId, ct);

        if (file == null)
            throw new KeyNotFoundException($"File with ID {fileId} was not found.");

        return await _storageService.GetObjectStreamAsync(file.StorageKey, ct);
    }

    public async Task<DocumentFileDto> RenameFileAsync(
        Guid currentUserId,
        Guid documentId,
        Guid fileId,
        RenameFileRequest request,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var file = await _dbContext.DocumentFiles
            .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id == fileId, ct);

        if (file == null)
            throw new KeyNotFoundException($"File with ID {fileId} was not found.");

        var normalizedPath = NormalizePath(request.NewPath);
        var newFileName = Path.GetFileName(normalizedPath);

        var conflict = await _dbContext.DocumentFiles
            .AnyAsync(f => f.DocumentId == documentId && f.Id != fileId && f.Path == normalizedPath, ct);

        if (conflict)
            throw new DomainException($"Another file with path '{normalizedPath}' already exists.");

        file.Rename(normalizedPath, newFileName);
        await _dbContext.SaveChangesAsync(ct);

        return MapToFileDto(file);
    }

    public async Task DeleteFileAsync(
        Guid currentUserId,
        Guid documentId,
        Guid fileId,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var file = await _dbContext.DocumentFiles
            .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id == fileId, ct);

        if (file == null)
            throw new KeyNotFoundException($"File with ID {fileId} was not found.");

        var totalFiles = await _dbContext.DocumentFiles.CountAsync(f => f.DocumentId == documentId, ct);
        if (totalFiles <= 1)
            throw new DomainException("Cannot delete the only remaining file in a document.");

        if (file.IsMain)
        {
            var nextMain = await _dbContext.DocumentFiles
                .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id != fileId && !f.IsBinary, ct);

            if (nextMain != null)
            {
                nextMain.SetMain(true);
                var doc = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
                if (doc != null)
                {
                    doc.SetMainFile(nextMain.Id);
                }
            }
        }

        try
        {
            await _storageService.DeleteObjectAsync(file.StorageKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete storage object {StorageKey} for file {FileId}.", file.StorageKey, fileId);
        }

        _dbContext.DocumentFiles.Remove(file);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("File {Path} deleted from document {DocumentId}.", file.Path, documentId);
    }

    public async Task SetMainFileAsync(
        Guid currentUserId,
        Guid documentId,
        Guid fileId,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var targetFile = await _dbContext.DocumentFiles
            .FirstOrDefaultAsync(f => f.DocumentId == documentId && f.Id == fileId, ct);

        if (targetFile == null)
            throw new KeyNotFoundException($"File with ID {fileId} was not found.");

        if (targetFile.IsBinary)
            throw new DomainException("A binary file cannot be designated as the main document entry point.");

        var otherFiles = await _dbContext.DocumentFiles
            .Where(f => f.DocumentId == documentId && f.IsMain)
            .ToListAsync(ct);

        foreach (var other in otherFiles)
        {
            other.SetMain(false);
        }

        targetFile.SetMain(true);

        var doc = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc != null)
        {
            doc.SetMainFile(targetFile.Id);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task AssertUserCanReadAsync(Guid userId, Guid documentId, CancellationToken ct)
    {
        var doc = await _dbContext.Documents
            .AsNoTracking()
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (doc == null || doc.Status == DocumentStatus.Trashed)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        if (doc.OwnerUserId == userId || doc.Visibility == DocumentVisibility.Public)
            return;

        var isCollab = doc.Collaborators.Any(c => c.UserId == userId && c.IsActive());
        if (!isCollab)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found."); // Conceal 404
    }

    private async Task AssertUserCanEditAsync(Guid userId, Guid documentId, CancellationToken ct)
    {
        var doc = await _dbContext.Documents
            .AsNoTracking()
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (doc == null || doc.Status == DocumentStatus.Trashed)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        if (doc.OwnerUserId == userId)
            return;

        var isEditor = doc.Collaborators.Any(c => c.UserId == userId && c.IsActive() &&
                                                 (c.Role == DocumentPermissionRole.Owner || c.Role == DocumentPermissionRole.Editor));
        if (!isEditor)
            throw new UnauthorizedAccessException("You do not have permission to modify files in this document.");
    }

    private static string NormalizePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            throw new DomainException("Path cannot be empty.");

        var normalized = rawPath.Replace('\\', '/').Trim('/');
        if (normalized.Contains(".."))
            throw new DomainException("Directory traversal sequences (..) are not allowed.");

        return normalized;
    }

    private static DocumentFileDto MapToFileDto(DocumentFile f)
    {
        return new DocumentFileDto(
            f.Id,
            f.DocumentId,
            f.Path,
            f.FileName,
            f.FileExtension,
            f.MimeType,
            f.SizeBytes,
            f.StorageKey,
            f.IsMain,
            f.IsBinary,
            f.UpdatedAtUtc,
            f.CreatedAtUtc
        );
    }
}

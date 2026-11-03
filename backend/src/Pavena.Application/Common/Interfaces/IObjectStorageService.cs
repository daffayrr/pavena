using Pavena.Application.Common.DTOs;

namespace Pavena.Application.Common.Interfaces;

public interface IObjectStorageService
{
    bool ValidateFileMetadata(string fileName, string contentType, long sizeBytes, out string? errorMessage);
    Task<UploadTicketResponse> GenerateUploadTicketAsync(string fileName, string contentType, long sizeBytes, Guid userId, CancellationToken cancellationToken = default);
    Task<string> GenerateDownloadUrlAsync(string storageKey, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<Stream> GetObjectStreamAsync(string storageKey, CancellationToken cancellationToken = default);
    Task SaveObjectAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(string storageKey, CancellationToken cancellationToken = default);
    bool VerifyDownloadSignature(string storageKey, long expiresUnixTimestamp, string signature);
    bool VerifyUploadSignature(string storageKey, long expiresUnixTimestamp, long sizeBytes, string signature);
}

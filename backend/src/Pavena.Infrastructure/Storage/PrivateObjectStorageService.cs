using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.DTOs;
using Pavena.Application.Common.Interfaces;

namespace Pavena.Infrastructure.Storage;

public class PrivateObjectStorageService : IObjectStorageService
{
    private const long MaxAttachmentSizeBytes = 25 * 1024 * 1024; // 25 MB
    private static readonly TimeSpan DefaultTicketExpiry = TimeSpan.FromMinutes(15);

    private static readonly HashSet<string> DisallowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".sh", ".bash", ".vbs", ".ps1", ".psm1",
        ".msi", ".com", ".scr", ".jar", ".pif", ".application", ".gadget", ".hta",
        ".cpl", ".msc", ".wsf", ".reg", ".inf", ".scf", ".vb"
    };

    private readonly byte[] _hmacSecretKey;
    private readonly string _storageBasePath;
    private readonly ILogger<PrivateObjectStorageService> _logger;

    public PrivateObjectStorageService(IConfiguration configuration, ILogger<PrivateObjectStorageService> logger)
    {
        _logger = logger;

        var secret = configuration["Storage:SignedTokenSecret"] 
            ?? configuration["Jwt:SecretKey"] 
            ?? "PavenaSuperSecretKeyForStorageSignatures2026!#$DefaultTestingKey";

        _hmacSecretKey = Encoding.UTF8.GetBytes(secret);

        var configuredPath = configuration["Storage:BasePath"];
        _storageBasePath = !string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "App_Data", "storage"));

        if (!Directory.Exists(_storageBasePath))
        {
            Directory.CreateDirectory(_storageBasePath);
        }
    }

    public bool ValidateFileMetadata(string fileName, string contentType, long sizeBytes, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errorMessage = "File name is required.";
            return false;
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
        {
            errorMessage = "File name contains invalid characters or traversal sequences.";
            return false;
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
        {
            errorMessage = "File extension is required.";
            return false;
        }

        if (DisallowedExtensions.Contains(ext))
        {
            errorMessage = $"Files with extension '{ext}' are prohibited for academic security reasons.";
            return false;
        }

        if (sizeBytes <= 0)
        {
            errorMessage = "File size must be greater than zero bytes.";
            return false;
        }

        if (sizeBytes > MaxAttachmentSizeBytes)
        {
            errorMessage = $"File size ({sizeBytes} bytes) exceeds maximum permitted limit of {MaxAttachmentSizeBytes / (1024 * 1024)} MB.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            errorMessage = "Content type is required.";
            return false;
        }

        return true;
    }

    public Task<UploadTicketResponse> GenerateUploadTicketAsync(string fileName, string contentType, long sizeBytes, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!ValidateFileMetadata(fileName, contentType, sizeBytes, out var error))
        {
            throw new ArgumentException(error);
        }

        var safeFileName = Path.GetFileName(fileName);
        var randomId = Guid.NewGuid().ToString("N");
        var storageKey = $"storage/posts/{userId:N}/{randomId}_{safeFileName}";

        var expiresAt = DateTime.UtcNow.Add(DefaultTicketExpiry);
        var expiresUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();

        var signature = ComputeHmacSignature($"UPLOAD:{storageKey}:{expiresUnix}:{sizeBytes}");
        var uploadUrl = $"/api/v1/storage/upload?key={Uri.EscapeDataString(storageKey)}&expires={expiresUnix}&size={sizeBytes}&sig={Uri.EscapeDataString(signature)}";

        var response = new UploadTicketResponse(
            StorageKey: storageKey,
            UploadUrl: uploadUrl,
            FileName: safeFileName,
            SizeBytes: sizeBytes,
            ExpiresAtUtc: expiresAt,
            Signature: signature
        );

        return Task.FromResult(response);
    }

    public Task<string> GenerateDownloadUrlAsync(string storageKey, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        var expiresAt = DateTime.UtcNow.Add(expiry);
        var expiresUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();

        var signature = ComputeHmacSignature($"DOWNLOAD:{storageKey}:{expiresUnix}");
        var downloadUrl = $"/api/v1/storage/download?key={Uri.EscapeDataString(storageKey)}&expires={expiresUnix}&sig={Uri.EscapeDataString(signature)}";

        return Task.FromResult(downloadUrl);
    }

    public Task<Stream> GetObjectStreamAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSecurePhysicalPath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Object '{storageKey}' does not exist in storage.");
        }

        Stream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(fileStream);
    }

    public async Task SaveObjectAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSecurePhysicalPath(storageKey);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task DeleteObjectAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetSecurePhysicalPath(storageKey);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted storage object: {StorageKey}", storageKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete storage object: {StorageKey}", storageKey);
        }

        return Task.CompletedTask;
    }

    public bool VerifyDownloadSignature(string storageKey, long expiresUnixTimestamp, string signature)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnixTimestamp)
        {
            return false;
        }

        var expectedSignature = ComputeHmacSignature($"DOWNLOAD:{storageKey}:{expiresUnixTimestamp}");
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    public bool VerifyUploadSignature(string storageKey, long expiresUnixTimestamp, long sizeBytes, string signature)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnixTimestamp)
        {
            return false;
        }

        var expected = ComputeHmacSignature($"UPLOAD:{storageKey}:{expiresUnixTimestamp}:{sizeBytes}");
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    private string ComputeHmacSignature(string payload)
    {
        using var hmac = new HMACSHA256(_hmacSecretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string GetSecurePhysicalPath(string storageKey)
    {
        // Replace potential backslashes and normalize
        var normalizedKey = storageKey.Replace('\\', '/').TrimStart('/');
        var targetPath = Path.GetFullPath(Path.Combine(_storageBasePath, normalizedKey));

        // Strict directory traversal prevention: must reside within _storageBasePath
        if (!targetPath.StartsWith(_storageBasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Illegal storage key path traversal attempted.");
        }

        return targetPath;
    }
}

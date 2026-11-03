namespace Pavena.Application.Common.DTOs;

public record UploadTicketRequest(
    string FileName,
    string ContentType,
    long SizeBytes
);

public record UploadTicketResponse(
    string StorageKey,
    string UploadUrl,
    string FileName,
    long SizeBytes,
    DateTime ExpiresAtUtc,
    string Signature
);

public record DownloadTicketResponse(
    string DownloadUrl,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime ExpiresAtUtc
);

public record MalwareScanResult(
    bool IsClean,
    string? ThreatName = null,
    string? Message = null
);

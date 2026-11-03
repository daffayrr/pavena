using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Documents;

public class CompilationJob : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid TriggeredByUserId { get; set; }
    public CompilationEngine Engine { get; set; } = CompilationEngine.PdfLatex;
    public CompilationStatus Status { get; set; } = CompilationStatus.Queued;
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationMs { get; set; }
    public string? OutputLog { get; set; }
    public string? LogStorageKey { get; set; }
    public string? PdfStorageKey { get; set; }
    public long? PdfSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual Document? Document { get; set; }
    public virtual User? TriggeredByUser { get; set; }
    public virtual ICollection<CompilationArtifact> Artifacts { get; set; } = new List<CompilationArtifact>();

    public CompilationJob()
    {
    }

    public static CompilationJob Create(Guid documentId, Guid triggeredByUserId, CompilationEngine engine = CompilationEngine.PdfLatex)
    {
        if (documentId == Guid.Empty)
            throw new DomainException("Document ID cannot be empty.");

        if (triggeredByUserId == Guid.Empty)
            throw new DomainException("TriggeredByUserId cannot be empty.");

        return new CompilationJob
        {
            DocumentId = documentId,
            TriggeredByUserId = triggeredByUserId,
            Engine = engine,
            Status = CompilationStatus.Queued,
            QueuedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkRunning()
    {
        Status = CompilationStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void MarkSucceeded(string? pdfStorageKey, long? pdfSizeBytes, string? logStorageKey, string? outputLog, int durationMs)
    {
        Status = CompilationStatus.Succeeded;
        PdfStorageKey = pdfStorageKey;
        PdfSizeBytes = pdfSizeBytes;
        LogStorageKey = logStorageKey;
        OutputLog = outputLog;
        DurationMs = durationMs;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage, string? logStorageKey, string? outputLog, int durationMs)
    {
        Status = CompilationStatus.Failed;
        ErrorMessage = errorMessage;
        LogStorageKey = logStorageKey;
        OutputLog = outputLog;
        DurationMs = durationMs;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkTimedOut(int durationMs)
    {
        Status = CompilationStatus.TimedOut;
        ErrorMessage = "Compilation exceeded execution time limit.";
        DurationMs = durationMs;
        CompletedAtUtc = DateTime.UtcNow;
    }
}

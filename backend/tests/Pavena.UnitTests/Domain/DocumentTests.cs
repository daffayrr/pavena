using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class DocumentTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesCorrectly()
    {
        var ownerId = Guid.NewGuid();
        var doc = Document.Create("Deep Learning Foundations", EditorMode.Latex, ownerId, null, "Neural networks research", "Manuscript");

        doc.Title.Should().Be("Deep Learning Foundations");
        doc.Mode.Should().Be(EditorMode.Latex);
        doc.OwnerUserId.Should().Be(ownerId);
        doc.Visibility.Should().Be(DocumentVisibility.Private);
        doc.Status.Should().Be(DocumentStatus.Active);
        doc.DocumentType.Should().Be("Manuscript");
        doc.Description.Should().Be("Neural networks research");
        doc.SchemaVersion.Should().Be(1);
        doc.IsRealTimeEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsDomainException()
    {
        Action act = () => Document.Create("   ", EditorMode.Latex, Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("*title cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyOwner_ThrowsDomainException()
    {
        Action act = () => Document.Create("Quantum Cryptography", EditorMode.Latex, Guid.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("*must have a valid owner*");
    }

    [Fact]
    public void UpdateMetadata_UpdatesValuesAndRefreshesConcurrencyStamp()
    {
        var ownerId = Guid.NewGuid();
        var doc = Document.Create("Original Title", EditorMode.Latex, ownerId);
        var originalStamp = doc.ConcurrencyStamp;

        doc.UpdateMetadata("Updated Title", "Updated Description", DocumentVisibility.Public, "Proposal");

        doc.Title.Should().Be("Updated Title");
        doc.Description.Should().Be("Updated Description");
        doc.Visibility.Should().Be(DocumentVisibility.Public);
        doc.DocumentType.Should().Be("Proposal");
        doc.ConcurrencyStamp.Should().NotBe(originalStamp);
    }

    [Fact]
    public void DocumentFile_Create_NormalizesPathAndExtension()
    {
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var file = DocumentFile.Create(
            docId,
            "chapters\\intro.tex",
            "intro.tex",
            ".TEX",
            "application/x-tex",
            1024,
            "docs/key",
            true,
            false,
            userId
        );

        file.Path.Should().Be("chapters/intro.tex");
        file.FileExtension.Should().Be(".tex");
        file.IsMain.Should().BeTrue();
        file.IsBinary.Should().BeFalse();
        file.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void DocumentFile_Rename_UpdatesPathAndExtension()
    {
        var docId = Guid.NewGuid();
        var file = DocumentFile.Create(docId, "old.tex", "old.tex", ".tex", "application/x-tex", 100, "key", false, false, Guid.NewGuid());

        file.Rename("sections/new_chapter.tex", "new_chapter.tex");

        file.Path.Should().Be("sections/new_chapter.tex");
        file.FileName.Should().Be("new_chapter.tex");
        file.FileExtension.Should().Be(".tex");
    }

    [Fact]
    public void DocumentVersion_Create_ValidatesInputs()
    {
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var version = DocumentVersion.Create(
            docId,
            1,
            "First Submission",
            "Initial peer review draft",
            DocumentVersionSource.ManualSnapshot,
            "versions/v1.zip",
            50000,
            "abcdef123456",
            userId
        );

        version.VersionNumber.Should().Be(1);
        version.Title.Should().Be("First Submission");
        version.IsRestorable.Should().BeTrue();
        version.ArchiveSizeBytes.Should().Be(50000);

        Action invalidVersionNum = () => DocumentVersion.Create(docId, 0, "Bad", null, DocumentVersionSource.ManualSnapshot, "k", 1, "h", userId);
        invalidVersionNum.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void CompilationJob_StateTransitions_FunctionCorrectly()
    {
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var job = CompilationJob.Create(docId, userId, CompilationEngine.PdfLatex);

        job.Status.Should().Be(CompilationStatus.Queued);
        job.StartedAtUtc.Should().BeNull();

        job.MarkRunning();
        job.Status.Should().Be(CompilationStatus.Running);
        job.StartedAtUtc.Should().NotBeNull();

        job.MarkSucceeded("pdf/key.pdf", 4096, "log/key.log", "Compiled successfully", 1250);
        job.Status.Should().Be(CompilationStatus.Succeeded);
        job.PdfStorageKey.Should().Be("pdf/key.pdf");
        job.DurationMs.Should().Be(1250);
        job.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void DocumentComment_ResolveAndReopen_TogglesStatus()
    {
        var docId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var resolverId = Guid.NewGuid();

        var comment = DocumentComment.Create(docId, authorId, "Please clarify equation 4", null, 42, "\\frac{a}{b}");

        comment.Status.Should().Be(CommentStatus.Active);
        comment.LineNumber.Should().Be(42);
        comment.SelectedText.Should().Be("\\frac{a}{b}");

        comment.Resolve(resolverId);
        comment.Status.Should().Be(CommentStatus.Resolved);
        comment.ResolvedByUserId.Should().Be(resolverId);
        comment.ResolvedAtUtc.Should().NotBeNull();

        comment.Reopen();
        comment.Status.Should().Be(CommentStatus.Active);
        comment.ResolvedByUserId.Should().BeNull();
        comment.ResolvedAtUtc.Should().BeNull();
    }

    [Fact]
    public void DocumentReview_Decision_ValidatesAndRecords()
    {
        var docId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var review = DocumentReview.Create(docId, reviewerId, requesterId);
        review.Status.Should().Be(ReviewStatus.Pending);

        Action invalidPending = () => review.SubmitDecision(ReviewStatus.Pending, "notes");
        invalidPending.Should().Throw<DomainException>().WithMessage("*Cannot submit a Pending*");

        review.SubmitDecision(ReviewStatus.Approved, "Rigorous methodology and reproducible benchmarks.");
        review.Status.Should().Be(ReviewStatus.Approved);
        review.SummaryNotes.Should().Be("Rigorous methodology and reproducible benchmarks.");
        review.DecidedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void DocumentCollaborator_IsActive_EnforcesExpirationAndRevocation()
    {
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var granterId = Guid.NewGuid();

        var collab = new DocumentCollaborator(docId, userId, DocumentPermissionRole.Editor, granterId, DateTime.UtcNow.AddDays(7));
        collab.IsActive().Should().BeTrue();

        collab.Revoke();
        collab.IsActive().Should().BeFalse();

        var expiredCollab = new DocumentCollaborator(docId, userId, DocumentPermissionRole.Viewer, granterId, DateTime.UtcNow.AddMinutes(-5));
        expiredCollab.IsActive().Should().BeFalse();
    }
}

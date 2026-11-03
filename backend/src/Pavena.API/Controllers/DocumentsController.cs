using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Documents.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentFileSystemService _fileSystemService;
    private readonly ILatexCompilerService _compilerService;
    private readonly IDocumentVersioningService _versioningService;
    private readonly IDocumentReviewService _reviewService;
    private readonly IValidator<CreateDocumentRequest> _createValidator;
    private readonly IValidator<UpdateDocumentRequest> _updateValidator;
    private readonly IValidator<CreateFileRequest> _createFileValidator;
    private readonly IValidator<CreateCommentRequest> _createCommentValidator;
    private readonly IValidator<CreateVersionRequest> _createVersionValidator;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentFileSystemService fileSystemService,
        ILatexCompilerService compilerService,
        IDocumentVersioningService versioningService,
        IDocumentReviewService reviewService,
        IValidator<CreateDocumentRequest> createValidator,
        IValidator<UpdateDocumentRequest> updateValidator,
        IValidator<CreateFileRequest> createFileValidator,
        IValidator<CreateCommentRequest> createCommentValidator,
        IValidator<CreateVersionRequest> createVersionValidator)
    {
        _documentService = documentService;
        _fileSystemService = fileSystemService;
        _compilerService = compilerService;
        _versioningService = versioningService;
        _reviewService = reviewService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _createFileValidator = createFileValidator;
        _createCommentValidator = createCommentValidator;
        _createVersionValidator = createVersionValidator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var document = await _documentService.CreateDocumentAsync(callerUserId.Value, request, ct);
        return CreatedAtAction(nameof(GetDocumentById), new { id = document.Id }, document);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDocuments([FromQuery] Guid? projectId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var documents = await _documentService.GetUserDocumentsAsync(callerUserId.Value, projectId, ct);
        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocumentById([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var document = await _documentService.GetDocumentByIdAsync(callerUserId.Value, id, ct);
        if (document == null) return NotFound(); // Conceal private document existence

        return Ok(document);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDocument([FromRoute] Guid id, [FromBody] UpdateDocumentRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var updated = await _documentService.UpdateDocumentAsync(callerUserId.Value, id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _documentService.DeleteDocumentAsync(callerUserId.Value, id, ct);
        return NoContent();
    }

    // Collaborators
    [HttpGet("{id:guid}/collaborators")]
    public async Task<IActionResult> GetCollaborators([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var collaborators = await _documentService.GetCollaboratorsAsync(callerUserId.Value, id, ct);
        return Ok(collaborators);
    }

    [HttpPost("{id:guid}/collaborators")]
    public async Task<IActionResult> AddCollaborator([FromRoute] Guid id, [FromBody] AddCollaboratorRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var collaborator = await _documentService.AddCollaboratorAsync(callerUserId.Value, id, request, ct);
        return Ok(collaborator);
    }

    [HttpPut("{id:guid}/collaborators/{userId:guid}")]
    public async Task<IActionResult> UpdateCollaborator([FromRoute] Guid id, [FromRoute] Guid userId, [FromBody] UpdateCollaboratorRoleRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var collaborator = await _documentService.UpdateCollaboratorAsync(callerUserId.Value, id, userId, request, ct);
        return Ok(collaborator);
    }

    [HttpDelete("{id:guid}/collaborators/{userId:guid}")]
    public async Task<IActionResult> RemoveCollaborator([FromRoute] Guid id, [FromRoute] Guid userId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _documentService.RemoveCollaboratorAsync(callerUserId.Value, id, userId, ct);
        return NoContent();
    }

    // File Tree Operations
    [HttpPost("{id:guid}/files")]
    public async Task<IActionResult> CreateFile([FromRoute] Guid id, [FromBody] CreateFileRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _createFileValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var file = await _fileSystemService.CreateFileAsync(callerUserId.Value, id, request, ct);
        return Ok(file);
    }

    [HttpGet("{id:guid}/files/{fileId:guid}/content")]
    public async Task<IActionResult> GetFileContent([FromRoute] Guid id, [FromRoute] Guid fileId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var content = await _fileSystemService.GetFileContentAsync(callerUserId.Value, id, fileId, ct);
        return Ok(new { content });
    }

    [HttpPut("{id:guid}/files/{fileId:guid}/content")]
    public async Task<IActionResult> UpdateFileContent([FromRoute] Guid id, [FromRoute] Guid fileId, [FromBody] UpdateFileContentRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var file = await _fileSystemService.UpdateFileContentAsync(callerUserId.Value, id, fileId, request.Content, ct);
        return Ok(file);
    }

    [HttpPost("{id:guid}/files/{fileId:guid}/rename")]
    public async Task<IActionResult> RenameFile([FromRoute] Guid id, [FromRoute] Guid fileId, [FromBody] RenameFileRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var file = await _fileSystemService.RenameFileAsync(callerUserId.Value, id, fileId, request, ct);
        return Ok(file);
    }

    [HttpDelete("{id:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile([FromRoute] Guid id, [FromRoute] Guid fileId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _fileSystemService.DeleteFileAsync(callerUserId.Value, id, fileId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/files/{fileId:guid}/set-main")]
    public async Task<IActionResult> SetMainFile([FromRoute] Guid id, [FromRoute] Guid fileId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _fileSystemService.SetMainFileAsync(callerUserId.Value, id, fileId, ct);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/files/upload")]
    public async Task<IActionResult> UploadBinaryFile([FromRoute] Guid id, [FromForm] IFormFile file, [FromForm] string path, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        using var stream = file.OpenReadStream();
        var targetPath = string.IsNullOrWhiteSpace(path) ? file.FileName : path;
        var uploaded = await _fileSystemService.UploadBinaryFileAsync(callerUserId.Value, id, targetPath, stream, file.ContentType, ct);
        return Ok(uploaded);
    }

    [HttpGet("{id:guid}/files/{fileId:guid}/download")]
    public async Task<IActionResult> DownloadFile([FromRoute] Guid id, [FromRoute] Guid fileId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var stream = await _fileSystemService.DownloadFileStreamAsync(callerUserId.Value, id, fileId, ct);
        return File(stream, "application/octet-stream");
    }

    // Compilation Pipeline
    [HttpPost("{id:guid}/compile")]
    public async Task<IActionResult> TriggerCompilation([FromRoute] Guid id, [FromBody] TriggerCompilationRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var job = await _compilerService.TriggerCompilationAsync(callerUserId.Value, id, request, ct);
        return Ok(job);
    }

    [HttpGet("{id:guid}/compile/{jobId:guid}")]
    public async Task<IActionResult> GetCompilationJob([FromRoute] Guid id, [FromRoute] Guid jobId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var job = await _compilerService.GetCompilationJobAsync(callerUserId.Value, id, jobId, ct);
        if (job == null) return NotFound();

        return Ok(job);
    }

    [HttpGet("{id:guid}/compile/history")]
    public async Task<IActionResult> GetCompilationHistory([FromRoute] Guid id, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var history = await _compilerService.GetCompilationHistoryAsync(callerUserId.Value, id, limit, ct);
        return Ok(history);
    }

    [HttpGet("{id:guid}/compile/{jobId:guid}/pdf")]
    public async Task<IActionResult> DownloadCompiledPdf([FromRoute] Guid id, [FromRoute] Guid jobId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var stream = await _compilerService.DownloadPdfStreamAsync(callerUserId.Value, id, jobId, ct);
        return File(stream, "application/pdf", "compiled_document.pdf");
    }

    // Versioning
    [HttpPost("{id:guid}/versions")]
    public async Task<IActionResult> CreateVersion([FromRoute] Guid id, [FromBody] CreateVersionRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _createVersionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var version = await _versioningService.CreateVersionAsync(callerUserId.Value, id, request, ct);
        return Ok(version);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var versions = await _versioningService.GetVersionsAsync(callerUserId.Value, id, ct);
        return Ok(versions);
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    public async Task<IActionResult> RestoreVersion([FromRoute] Guid id, [FromRoute] Guid versionId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var restored = await _versioningService.RestoreVersionAsync(callerUserId.Value, id, versionId, ct);
        return Ok(restored);
    }

    // Comments & Reviews
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> CreateComment([FromRoute] Guid id, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _createCommentValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var comment = await _reviewService.CreateCommentAsync(callerUserId.Value, id, request, ct);
        return Ok(comment);
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments([FromRoute] Guid id, [FromQuery] Guid? fileId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var comments = await _reviewService.GetCommentsAsync(callerUserId.Value, id, fileId, ct);
        return Ok(comments);
    }

    [HttpPost("{id:guid}/comments/{commentId:guid}/resolve")]
    public async Task<IActionResult> ResolveComment([FromRoute] Guid id, [FromRoute] Guid commentId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var comment = await _reviewService.ResolveCommentAsync(callerUserId.Value, id, commentId, ct);
        return Ok(comment);
    }

    [HttpPost("{id:guid}/comments/{commentId:guid}/reopen")]
    public async Task<IActionResult> ReopenComment([FromRoute] Guid id, [FromRoute] Guid commentId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var comment = await _reviewService.ReopenCommentAsync(callerUserId.Value, id, commentId, ct);
        return Ok(comment);
    }

    [HttpPost("{id:guid}/reviews")]
    public async Task<IActionResult> RequestReview([FromRoute] Guid id, [FromBody] RequestReviewRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var review = await _reviewService.RequestReviewAsync(callerUserId.Value, id, request, ct);
        return Ok(review);
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var reviews = await _reviewService.GetReviewsAsync(callerUserId.Value, id, ct);
        return Ok(reviews);
    }

    [HttpPost("{id:guid}/reviews/{reviewId:guid}/decision")]
    public async Task<IActionResult> SubmitReviewDecision([FromRoute] Guid id, [FromRoute] Guid reviewId, [FromBody] SubmitReviewDecisionRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var review = await _reviewService.SubmitReviewDecisionAsync(callerUserId.Value, id, reviewId, request, ct);
        return Ok(review);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

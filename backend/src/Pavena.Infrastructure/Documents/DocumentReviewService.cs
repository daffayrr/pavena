using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Documents.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Documents;

public class DocumentReviewService : IDocumentReviewService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly ILogger<DocumentReviewService> _logger;

    public DocumentReviewService(IPavenaDbContext dbContext, ILogger<DocumentReviewService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DocumentCommentDto> CreateCommentAsync(
        Guid currentUserId,
        Guid documentId,
        CreateCommentRequest request,
        CancellationToken ct = default)
    {
        await AssertUserCanReadAsync(currentUserId, documentId, ct);

        if (request.FileId.HasValue)
        {
            var fileExists = await _dbContext.DocumentFiles
                .AnyAsync(f => f.DocumentId == documentId && f.Id == request.FileId.Value, ct);
            if (!fileExists)
                throw new KeyNotFoundException($"File with ID {request.FileId.Value} was not found.");
        }

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _dbContext.DocumentComments
                .AnyAsync(c => c.DocumentId == documentId && c.Id == request.ParentCommentId.Value, ct);
            if (!parentExists)
                throw new KeyNotFoundException($"Parent comment with ID {request.ParentCommentId.Value} was not found.");
        }

        var comment = DocumentComment.Create(
            documentId,
            currentUserId,
            request.Content,
            request.FileId,
            request.LineNumber,
            request.SelectedText,
            request.ParentCommentId
        );

        _dbContext.DocumentComments.Add(comment);
        await _dbContext.SaveChangesAsync(ct);

        var author = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        var authorName = author?.Profile?.DisplayName ?? author?.UserName ?? "Researcher";

        string? filePath = null;
        if (comment.FileId.HasValue)
        {
            filePath = await _dbContext.DocumentFiles
                .Where(f => f.Id == comment.FileId.Value)
                .Select(f => f.Path)
                .FirstOrDefaultAsync(ct);
        }

        return new DocumentCommentDto(
            comment.Id,
            comment.DocumentId,
            comment.FileId,
            filePath,
            comment.LineNumber,
            comment.SelectedText,
            comment.Content,
            comment.Status,
            comment.ParentCommentId,
            comment.AuthorUserId,
            authorName,
            comment.CreatedAtUtc,
            comment.ResolvedAtUtc,
            comment.ResolvedByUserId,
            null,
            new List<DocumentCommentDto>()
        );
    }

    public async Task<List<DocumentCommentDto>> GetCommentsAsync(
        Guid currentUserId,
        Guid documentId,
        Guid? fileId = null,
        CancellationToken ct = default)
    {
        await AssertUserCanReadAsync(currentUserId, documentId, ct);

        var query = _dbContext.DocumentComments
            .AsNoTracking()
            .Include(c => c.AuthorUser)
            .Include(c => c.ResolvedByUser)
            .Include(c => c.File)
            .Where(c => c.DocumentId == documentId);

        if (fileId.HasValue)
        {
            query = query.Where(c => c.FileId == fileId.Value);
        }

        var allComments = await query
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        // Map root comments and nest replies
        var rootComments = allComments.Where(c => c.ParentCommentId == null).ToList();

        var dtoList = new List<DocumentCommentDto>();
        foreach (var root in rootComments)
        {
            var replies = allComments
                .Where(r => r.ParentCommentId == root.Id)
                .Select(r => MapCommentDto(r, new List<DocumentCommentDto>()))
                .ToList();

            dtoList.Add(MapCommentDto(root, replies));
        }

        return dtoList;
    }

    public async Task<DocumentCommentDto> ResolveCommentAsync(
        Guid currentUserId,
        Guid documentId,
        Guid commentId,
        CancellationToken ct = default)
    {
        var comment = await _dbContext.DocumentComments
            .Include(c => c.AuthorUser)
            .Include(c => c.File)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.DocumentId == documentId, ct);

        if (comment == null)
            throw new KeyNotFoundException($"Comment with ID {commentId} was not found.");

        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        comment.Resolve(currentUserId);
        await _dbContext.SaveChangesAsync(ct);

        var resolver = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        var resolverName = resolver?.Profile?.DisplayName ?? resolver?.UserName ?? "Researcher";

        return MapCommentDto(comment, new List<DocumentCommentDto>(), resolverName);
    }

    public async Task<DocumentCommentDto> ReopenCommentAsync(
        Guid currentUserId,
        Guid documentId,
        Guid commentId,
        CancellationToken ct = default)
    {
        var comment = await _dbContext.DocumentComments
            .Include(c => c.AuthorUser)
            .Include(c => c.File)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.DocumentId == documentId, ct);

        if (comment == null)
            throw new KeyNotFoundException($"Comment with ID {commentId} was not found.");

        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        comment.Reopen();
        await _dbContext.SaveChangesAsync(ct);

        return MapCommentDto(comment, new List<DocumentCommentDto>());
    }

    public async Task<DocumentReviewDto> RequestReviewAsync(
        Guid currentUserId,
        Guid documentId,
        RequestReviewRequest request,
        CancellationToken ct = default)
    {
        await AssertUserCanEditAsync(currentUserId, documentId, ct);

        var reviewer = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == request.ReviewerUserId, ct);
        if (reviewer == null)
            throw new KeyNotFoundException($"User with ID {request.ReviewerUserId} was not found.");

        int? versionNumber = null;
        if (request.DocumentVersionId.HasValue)
        {
            var version = await _dbContext.DocumentVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.Id == request.DocumentVersionId.Value, ct);
            if (version == null)
                throw new KeyNotFoundException($"Document version with ID {request.DocumentVersionId.Value} was not found.");
            versionNumber = version.VersionNumber;
        }

        var review = DocumentReview.Create(
            documentId,
            request.ReviewerUserId,
            currentUserId,
            request.DocumentVersionId
        );

        _dbContext.DocumentReviews.Add(review);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Review {ReviewId} requested for document {DocumentId} to reviewer {ReviewerId}.",
            review.Id, documentId, request.ReviewerUserId);

        var requester = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        return new DocumentReviewDto(
            review.Id,
            review.DocumentId,
            review.DocumentVersionId,
            versionNumber,
            review.ReviewerUserId,
            reviewer.Profile?.DisplayName ?? reviewer.UserName ?? "Reviewer",
            review.RequestedByUserId,
            requester?.Profile?.DisplayName ?? requester?.UserName ?? "Author",
            review.Status,
            review.SummaryNotes,
            review.CreatedAtUtc,
            review.DecidedAtUtc
        );
    }

    public async Task<DocumentReviewDto> SubmitReviewDecisionAsync(
        Guid currentUserId,
        Guid documentId,
        Guid reviewId,
        SubmitReviewDecisionRequest request,
        CancellationToken ct = default)
    {
        var review = await _dbContext.DocumentReviews
            .Include(r => r.DocumentVersion)
            .Include(r => r.ReviewerUser)
                .ThenInclude(u => u!.Profile)
            .Include(r => r.RequestedByUser)
                .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.DocumentId == documentId, ct);

        if (review == null)
            throw new KeyNotFoundException($"Review with ID {reviewId} was not found.");

        if (review.ReviewerUserId != currentUserId)
            throw new UnauthorizedAccessException("Only the assigned reviewer can submit a review decision.");

        review.SubmitDecision(request.Status, request.SummaryNotes);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Review {ReviewId} decision submitted: {Status}.", reviewId, request.Status);

        return new DocumentReviewDto(
            review.Id,
            review.DocumentId,
            review.DocumentVersionId,
            review.DocumentVersion?.VersionNumber,
            review.ReviewerUserId,
            review.ReviewerUser?.Profile?.DisplayName ?? review.ReviewerUser?.UserName ?? "Reviewer",
            review.RequestedByUserId,
            review.RequestedByUser?.Profile?.DisplayName ?? review.RequestedByUser?.UserName ?? "Requester",
            review.Status,
            review.SummaryNotes,
            review.CreatedAtUtc,
            review.DecidedAtUtc
        );
    }

    public async Task<List<DocumentReviewDto>> GetReviewsAsync(
        Guid currentUserId,
        Guid documentId,
        CancellationToken ct = default)
    {
        await AssertUserCanReadAsync(currentUserId, documentId, ct);

        var reviews = await _dbContext.DocumentReviews
            .AsNoTracking()
            .Include(r => r.DocumentVersion)
            .Include(r => r.ReviewerUser)
                .ThenInclude(u => u!.Profile)
            .Include(r => r.RequestedByUser)
                .ThenInclude(u => u!.Profile)
            .Where(r => r.DocumentId == documentId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return reviews.Select(r => new DocumentReviewDto(
            r.Id,
            r.DocumentId,
            r.DocumentVersionId,
            r.DocumentVersion?.VersionNumber,
            r.ReviewerUserId,
            r.ReviewerUser?.Profile?.DisplayName ?? r.ReviewerUser?.UserName ?? "Reviewer",
            r.RequestedByUserId,
            r.RequestedByUser?.Profile?.DisplayName ?? r.RequestedByUser?.UserName ?? "Requester",
            r.Status,
            r.SummaryNotes,
            r.CreatedAtUtc,
            r.DecidedAtUtc
        )).ToList();
    }

    private static DocumentCommentDto MapCommentDto(DocumentComment c, List<DocumentCommentDto> replies, string? resolverName = null)
    {
        var authorName = c.AuthorUser?.Profile?.DisplayName ?? c.AuthorUser?.UserName ?? "Author";
        var resName = resolverName ?? (c.ResolvedByUser?.Profile?.DisplayName ?? c.ResolvedByUser?.UserName);

        return new DocumentCommentDto(
            c.Id,
            c.DocumentId,
            c.FileId,
            c.File?.Path,
            c.LineNumber,
            c.SelectedText,
            c.Content,
            c.Status,
            c.ParentCommentId,
            c.AuthorUserId,
            authorName,
            c.CreatedAtUtc,
            c.ResolvedAtUtc,
            c.ResolvedByUserId,
            resName,
            replies
        );
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
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");
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
            throw new UnauthorizedAccessException("You do not have permission to modify this document.");
    }
}

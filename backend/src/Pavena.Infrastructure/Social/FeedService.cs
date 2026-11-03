using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;
using Pavena.Application.Social.Pagination;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Social;

public class FeedService : IFeedService
{
    private readonly IPavenaDbContext _dbContext;

    public FeedService(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FeedResponseDto> GetMyNetworkFeedAsync(
        Guid callerUserId,
        int limit = 20,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(limit, 1, 50);

        // 1. Bilateral block list
        var blockedUserIds = await _dbContext.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerUserId == callerUserId || b.BlockedUserId == callerUserId)
            .Select(b => b.BlockerUserId == callerUserId ? b.BlockedUserId : b.BlockerUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // 2. Accepted connection peers
        var connectedUserIds = await _dbContext.Connections
            .AsNoTracking()
            .Where(c => (c.CanonicalUserA == callerUserId || c.CanonicalUserB == callerUserId) &&
                        c.Status == ConnectionStatus.Accepted)
            .Select(c => c.CanonicalUserA == callerUserId ? c.CanonicalUserB : c.CanonicalUserA)
            .Distinct()
            .ToListAsync(cancellationToken);

        // 3. Followed user IDs
        var followedUserIds = await _dbContext.Follows
            .AsNoTracking()
            .Where(f => f.FollowerUserId == callerUserId && f.TargetType == FollowTargetType.User)
            .Select(f => f.TargetId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Remove blocked users from connections and follows
        if (blockedUserIds.Count > 0)
        {
            connectedUserIds.RemoveAll(id => blockedUserIds.Contains(id));
            followedUserIds.RemoveAll(id => blockedUserIds.Contains(id));
        }

        // 4. Build query
        var query = _dbContext.Posts
            .AsNoTracking()
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .Where(p => !p.IsDeleted && p.Status == PostStatus.Published);

        if (blockedUserIds.Count > 0)
        {
            query = query.Where(p => !blockedUserIds.Contains(p.AuthorUserId));
        }

        // Visibility and author eligibility:
        // - Own posts
        // - Connected peer posts (Public or Connections)
        // - Followed accounts (Public only; follow does NOT grant access to Connections)
        query = query.Where(p =>
            p.AuthorUserId == callerUserId ||
            (connectedUserIds.Contains(p.AuthorUserId) && (p.Visibility == PostVisibility.Public || p.Visibility == PostVisibility.Connections)) ||
            (followedUserIds.Contains(p.AuthorUserId) && p.Visibility == PostVisibility.Public)
        );

        return await ExecuteKeysetFeedQueryAsync(query, pageSize, cursor, cancellationToken);
    }

    public async Task<FeedResponseDto> GetDiscoverFeedAsync(
        Guid callerUserId,
        int limit = 20,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(limit, 1, 50);

        // Bilateral block list
        var blockedUserIds = await _dbContext.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerUserId == callerUserId || b.BlockedUserId == callerUserId)
            .Select(b => b.BlockerUserId == callerUserId ? b.BlockedUserId : b.BlockerUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _dbContext.Posts
            .AsNoTracking()
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .Where(p => !p.IsDeleted &&
                        p.Status == PostStatus.Published &&
                        p.Visibility == PostVisibility.Public);

        if (blockedUserIds.Count > 0)
        {
            query = query.Where(p => !blockedUserIds.Contains(p.AuthorUserId));
        }

        return await ExecuteKeysetFeedQueryAsync(query, pageSize, cursor, cancellationToken);
    }

    private static async Task<FeedResponseDto> ExecuteKeysetFeedQueryAsync(
        IQueryable<Post> baseQuery,
        int pageSize,
        string? cursorString,
        CancellationToken cancellationToken)
    {
        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(cursorString))
        {
            var cursor = FeedCursorCodec.Decode(cursorString);
            var cursorDate = cursor.PublishedAtUtc;
            var cursorId = cursor.PostId;

            // Keyset condition: PublishedAtUtc < cursor.PublishedAtUtc OR (PublishedAtUtc == cursor.PublishedAtUtc AND Id < cursor.PostId)
            query = query.Where(p => p.PublishedAtUtc < cursorDate ||
                                    (p.PublishedAtUtc == cursorDate && p.Id.CompareTo(cursorId) < 0));
        }

        // Deterministic reverse chronological ordering
        query = query
            .OrderByDescending(p => p.PublishedAtUtc)
            .ThenByDescending(p => p.Id);

        // Fetch pageSize + 1 to detect hasMore
        var posts = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = posts.Count > pageSize;
        var items = hasMore ? posts.Take(pageSize).ToList() : posts;

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var lastItem = items[^1];
            if (lastItem.PublishedAtUtc.HasValue)
            {
                nextCursor = FeedCursorCodec.Encode(lastItem.PublishedAtUtc.Value, lastItem.Id);
            }
        }

        var dtos = items.Select(MapToDto).ToList();
        return new FeedResponseDto(dtos, nextCursor, hasMore);
    }

    private static PostDto MapToDto(Post post) => new(
        post.Id,
        post.AuthorUserId,
        post.AuthorUser?.Profile?.DisplayName ?? "Scholar",
        post.AuthorUser?.Profile?.AvatarUrl,
        post.Content,
        post.Visibility.ToString(),
        post.Status.ToString(),
        post.CreatedAtUtc,
        post.PublishedAtUtc,
        post.EditedAtUtc,
        post.Attachments.Select(a => new PostAttachmentDto(
            a.Id,
            a.FileName,
            a.ContentType,
            a.FileSizeBytes,
            a.CreatedAtUtc
        )).ToList()
    );
}

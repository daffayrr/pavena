using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Authorization;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Social;

public class PostService : IPostService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostService(
        IPavenaDbContext dbContext,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PostDto> CreatePostAsync(Guid authorId, CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        var authorExists = await _dbContext.Users
            .AnyAsync(u => u.Id == authorId, cancellationToken);
        if (!authorExists)
            throw new DomainException("Author user does not exist.");

        if (!Enum.TryParse<PostVisibility>(request.Visibility, true, out var visibility))
            visibility = PostVisibility.Public;

        Post post = request.PublishImmediately
            ? Post.CreatePublished(authorId, request.Content, visibility)
            : Post.CreateDraft(authorId, request.Content, visibility);

        if (request.Attachments != null && request.Attachments.Count > 0)
        {
            foreach (var att in request.Attachments)
            {
                post.AddAttachment(att.FileName, att.ContentType, att.FileSizeBytes, att.StorageKey);
            }
        }

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetPostByIdAsync(authorId, post.Id, cancellationToken);
    }

    public async Task<PostDto> GetPostByIdAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post == null)
            throw new KeyNotFoundException("Post not found.");

        var principal = GetUserPrincipal(callerUserId);
        var authResult = await _authorizationService.AuthorizeAsync(principal, post, new ResourceOperationRequirement(ResourceOperation.Read));
        if (!authResult.Succeeded)
        {
            // Avoid leaking the existence of private or restricted posts
            throw new KeyNotFoundException("Post not found.");
        }

        return MapToDto(post);
    }

    public async Task<PostDto> UpdatePostAsync(Guid callerUserId, Guid postId, UpdatePostRequest request, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post == null)
            throw new KeyNotFoundException("Post not found.");

        var principal = GetUserPrincipal(callerUserId);
        var authResult = await _authorizationService.AuthorizeAsync(principal, post, new ResourceOperationRequirement(ResourceOperation.Update));
        if (!authResult.Succeeded)
        {
            throw new KeyNotFoundException("Post not found.");
        }

        if (!Enum.TryParse<PostVisibility>(request.Visibility, true, out var visibility))
            visibility = PostVisibility.Public;

        post.UpdateContent(callerUserId, request.Content, visibility);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("The post was modified by another operation. Please reload and try again.");
        }

        return MapToDto(post);
    }

    public async Task DeletePostAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post == null)
            throw new KeyNotFoundException("Post not found.");

        var principal = GetUserPrincipal(callerUserId);
        var authResult = await _authorizationService.AuthorizeAsync(principal, post, new ResourceOperationRequirement(ResourceOperation.Delete));
        if (!authResult.Succeeded)
        {
            throw new KeyNotFoundException("Post not found.");
        }

        var isPrivileged = principal.IsInRole(PlatformRole.Superadmin) || principal.IsInRole(PlatformRole.PlatformAdmin);
        post.SoftDelete(callerUserId, isPrivileged);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PostDto> PublishPostAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post == null)
            throw new KeyNotFoundException("Post not found.");

        var principal = GetUserPrincipal(callerUserId);
        var authResult = await _authorizationService.AuthorizeAsync(principal, post, new ResourceOperationRequirement(ResourceOperation.Publish));
        if (!authResult.Succeeded)
        {
            throw new KeyNotFoundException("Post not found.");
        }

        post.Publish(callerUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(post);
    }

    public async Task<PostDto> ArchivePostAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post == null)
            throw new KeyNotFoundException("Post not found.");

        var principal = GetUserPrincipal(callerUserId);
        var authResult = await _authorizationService.AuthorizeAsync(principal, post, new ResourceOperationRequirement(ResourceOperation.Archive));
        if (!authResult.Succeeded)
        {
            throw new KeyNotFoundException("Post not found.");
        }

        post.Archive(callerUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(post);
    }

    public async Task<IReadOnlyList<PostDto>> GetUserPostsAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        // 1. Bilateral block check: if blocked in either direction, return empty
        var isBlocked = await _dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(b => (b.BlockerUserId == callerUserId && b.BlockedUserId == targetUserId) ||
                           (b.BlockerUserId == targetUserId && b.BlockedUserId == callerUserId), cancellationToken);

        if (isBlocked)
        {
            return Array.Empty<PostDto>();
        }

        IQueryable<Post> query = _dbContext.Posts
            .AsNoTracking()
            .Include(p => p.Attachments)
            .Include(p => p.AuthorUser)
                .ThenInclude(u => u!.Profile)
            .Where(p => p.AuthorUserId == targetUserId && !p.IsDeleted);

        if (callerUserId == targetUserId)
        {
            // Owner sees all their posts (Draft, Published, Archived)
            query = query.OrderByDescending(p => p.CreatedAtUtc);
        }
        else
        {
            // Non-owner only sees Published posts
            query = query.Where(p => p.Status == PostStatus.Published);

            var (canonA, canonB) = callerUserId.CompareTo(targetUserId) < 0
                ? (callerUserId, targetUserId)
                : (targetUserId, callerUserId);

            var isConnected = await _dbContext.Connections
                .AsNoTracking()
                .AnyAsync(c => c.CanonicalUserA == canonA &&
                               c.CanonicalUserB == canonB &&
                               c.Status == ConnectionStatus.Accepted, cancellationToken);

            if (isConnected)
            {
                query = query.Where(p => p.Visibility == PostVisibility.Public || p.Visibility == PostVisibility.Connections);
            }
            else
            {
                query = query.Where(p => p.Visibility == PostVisibility.Public);
            }

            query = query.OrderByDescending(p => p.PublishedAtUtc);
        }

        var posts = await query.ToListAsync(cancellationToken);
        return posts.Select(MapToDto).ToList();
    }

    private ClaimsPrincipal GetUserPrincipal(Guid userId)
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            return httpUser;
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
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

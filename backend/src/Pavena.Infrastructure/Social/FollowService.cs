using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Social;

public class FollowService : IFollowService
{
    private readonly IPavenaDbContext _dbContext;

    public FollowService(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task FollowUserAsync(Guid followerId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        if (followerId == targetUserId)
            throw new DomainException("Users cannot follow themselves.");

        var targetExists = await _dbContext.Users
            .AnyAsync(u => u.Id == targetUserId, cancellationToken);
        if (!targetExists)
            throw new DomainException("Target user does not exist.");

        var isBlocked = await _dbContext.UserBlocks
            .AnyAsync(b => (b.BlockerUserId == followerId && b.BlockedUserId == targetUserId) ||
                           (b.BlockerUserId == targetUserId && b.BlockedUserId == followerId), cancellationToken);
        if (isBlocked)
            throw new DomainException("Cannot follow this user due to privacy or block restrictions.");

        var existing = await _dbContext.Follows
            .AnyAsync(f => f.FollowerUserId == followerId &&
                           f.TargetType == FollowTargetType.User &&
                           f.TargetId == targetUserId, cancellationToken);

        if (existing)
            throw new DomainException("Already following this user.");

        var follow = Follow.FollowUser(followerId, targetUserId);
        _dbContext.Follows.Add(follow);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnfollowUserAsync(Guid followerId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var follow = await _dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerUserId == followerId &&
                                      f.TargetType == FollowTargetType.User &&
                                      f.TargetId == targetUserId, cancellationToken);

        if (follow == null)
            throw new DomainException("Not following this user.");

        _dbContext.Follows.Remove(follow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FollowUserDto>> GetFollowersAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var follows = await _dbContext.Follows
            .AsNoTracking()
            .Where(f => f.TargetType == FollowTargetType.User && f.TargetId == targetUserId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var followerIds = follows.Select(f => f.FollowerUserId).Distinct().ToList();

        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => followerIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        var result = new List<FollowUserDto>();
        foreach (var follow in follows)
        {
            if (profiles.TryGetValue(follow.FollowerUserId, out var profile))
            {
                result.Add(new FollowUserDto(
                    follow.FollowerUserId,
                    profile.DisplayName,
                    profile.Headline,
                    profile.AvatarUrl,
                    follow.CreatedAtUtc
                ));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<FollowUserDto>> GetFollowingAsync(Guid followerUserId, CancellationToken cancellationToken = default)
    {
        var follows = await _dbContext.Follows
            .AsNoTracking()
            .Where(f => f.FollowerUserId == followerUserId && f.TargetType == FollowTargetType.User)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var targetUserIds = follows.Select(f => f.TargetId).Distinct().ToList();

        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => targetUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        var result = new List<FollowUserDto>();
        foreach (var follow in follows)
        {
            if (profiles.TryGetValue(follow.TargetId, out var profile))
            {
                result.Add(new FollowUserDto(
                    follow.TargetId,
                    profile.DisplayName,
                    profile.Headline,
                    profile.AvatarUrl,
                    follow.CreatedAtUtc
                ));
            }
        }

        return result;
    }

    public async Task<FollowStatusDto> GetFollowStatusAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var followersCount = await _dbContext.Follows
            .CountAsync(f => f.TargetType == FollowTargetType.User && f.TargetId == targetUserId, cancellationToken);

        var followingCount = await _dbContext.Follows
            .CountAsync(f => f.FollowerUserId == targetUserId && f.TargetType == FollowTargetType.User, cancellationToken);

        var isFollowing = await _dbContext.Follows
            .AnyAsync(f => f.FollowerUserId == callerUserId &&
                           f.TargetType == FollowTargetType.User &&
                           f.TargetId == targetUserId, cancellationToken);

        var isFollowedBy = await _dbContext.Follows
            .AnyAsync(f => f.FollowerUserId == targetUserId &&
                           f.TargetType == FollowTargetType.User &&
                           f.TargetId == callerUserId, cancellationToken);

        return new FollowStatusDto(isFollowing, isFollowedBy, followersCount, followingCount);
    }
}

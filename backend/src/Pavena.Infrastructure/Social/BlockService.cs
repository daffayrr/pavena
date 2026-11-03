using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Social;

public class BlockService : IBlockService
{
    private readonly IPavenaDbContext _dbContext;

    public BlockService(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BlockUserAsync(Guid blockerId, Guid blockedId, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (blockerId == blockedId)
            throw new DomainException("Users cannot block themselves.");

        var blockedExists = await _dbContext.Users
            .AnyAsync(u => u.Id == blockedId, cancellationToken);
        if (!blockedExists)
            throw new DomainException("Target user does not exist.");

        var alreadyBlocked = await _dbContext.UserBlocks
            .AnyAsync(b => b.BlockerUserId == blockerId && b.BlockedUserId == blockedId, cancellationToken);
        if (alreadyBlocked)
            return;

        // Atomically sever connections and bilateral follows within a resilient database transaction
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // 1. Sever any connection between the two users
            var (canonA, canonB) = blockerId.CompareTo(blockedId) < 0
                ? (blockerId, blockedId)
                : (blockedId, blockerId);

            var connection = await _dbContext.Connections
                .FirstOrDefaultAsync(c => c.CanonicalUserA == canonA && c.CanonicalUserB == canonB, cancellationToken);

            if (connection != null)
            {
                _dbContext.Connections.Remove(connection);
            }

            // 2. Sever follows in both directions
            var follows = await _dbContext.Follows
                .Where(f => (f.FollowerUserId == blockerId && f.TargetType == FollowTargetType.User && f.TargetId == blockedId) ||
                            (f.FollowerUserId == blockedId && f.TargetType == FollowTargetType.User && f.TargetId == blockerId))
                .ToListAsync(cancellationToken);

            if (follows.Count > 0)
            {
                _dbContext.Follows.RemoveRange(follows);
            }

            // 3. Insert block record
            var block = UserBlock.CreateBlock(blockerId, blockedId, reason);
            _dbContext.UserBlocks.Add(block);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task UnblockUserAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        var block = await _dbContext.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerUserId == blockerId && b.BlockedUserId == blockedId, cancellationToken);

        if (block == null)
            throw new DomainException("User is not blocked.");

        _dbContext.UserBlocks.Remove(block);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BlockedUserDto>> GetBlockedUsersAsync(Guid blockerId, CancellationToken cancellationToken = default)
    {
        var blocks = await _dbContext.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerUserId == blockerId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var blockedUserIds = blocks.Select(b => b.BlockedUserId).Distinct().ToList();

        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => blockedUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => blockedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = new List<BlockedUserDto>();
        foreach (var block in blocks)
        {
            profiles.TryGetValue(block.BlockedUserId, out var profile);
            users.TryGetValue(block.BlockedUserId, out var user);
            result.Add(new BlockedUserDto(
                block.BlockedUserId,
                profile?.DisplayName ?? "Unknown User",
                profile?.AvatarUrl,
                block.Reason,
                block.CreatedAtUtc,
                user?.UserName
            ));
        }

        return result;
    }

    public async Task<bool> IsBlockedAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBlocks
            .AnyAsync(b => (b.BlockerUserId == userA && b.BlockedUserId == userB) ||
                           (b.BlockerUserId == userB && b.BlockedUserId == userA), cancellationToken);
    }
}

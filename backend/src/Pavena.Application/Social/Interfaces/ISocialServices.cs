using Pavena.Application.Social.DTOs;

namespace Pavena.Application.Social.Interfaces;

public interface IConnectionService
{
    Task<ConnectionDto> SendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);
    Task<ConnectionDto> AcceptRequestAsync(Guid callerUserId, Guid connectionId, CancellationToken cancellationToken = default);
    Task<ConnectionDto> RejectRequestAsync(Guid callerUserId, Guid connectionId, CancellationToken cancellationToken = default);
    Task<ConnectionDto> CancelRequestAsync(Guid callerUserId, Guid connectionId, CancellationToken cancellationToken = default);
    Task RemoveConnectionAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectionDto>> GetIncomingRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectionDto>> GetOutgoingRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectionUserDto>> GetAcceptedConnectionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ConnectionStatusDto> GetConnectionStatusAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
}

public interface IFollowService
{
    Task FollowUserAsync(Guid followerId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task UnfollowUserAsync(Guid followerId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FollowUserDto>> GetFollowersAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FollowUserDto>> GetFollowingAsync(Guid followerUserId, CancellationToken cancellationToken = default);
    Task<FollowStatusDto> GetFollowStatusAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
}

public interface IBlockService
{
    Task BlockUserAsync(Guid blockerId, Guid blockedId, string? reason = null, CancellationToken cancellationToken = default);
    Task UnblockUserAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlockedUserDto>> GetBlockedUsersAsync(Guid blockerId, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);
}

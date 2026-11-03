namespace Pavena.Application.Social.DTOs;

public record SendConnectionRequest(Guid AddresseeUserId);

public record ConnectionDto(
    Guid Id,
    Guid RequesterUserId,
    Guid AddresseeUserId,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? RespondedAtUtc
);

public record ConnectionUserDto(
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? AvatarUrl,
    DateTime ConnectedAtUtc
);

public record ConnectionStatusDto(
    string Status, // "None", "PendingIncoming", "PendingOutgoing", "Accepted", "Rejected", "Cancelled", "Blocked"
    Guid? ConnectionId,
    bool IsRequester
);

public record FollowUserDto(
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? AvatarUrl,
    DateTime FollowedAtUtc
);

public record FollowStatusDto(
    bool IsFollowing,
    bool IsFollowedBy,
    int FollowersCount,
    int FollowingCount
);

public record BlockUserRequest(string? Reason);

public record BlockedUserDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Reason,
    DateTime BlockedAtUtc
);

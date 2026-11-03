namespace Pavena.Application.Social.DTOs;

public record SendConnectionRequest(Guid AddresseeUserId);

public record ConnectionDto(
    Guid Id,
    Guid RequesterUserId,
    Guid AddresseeUserId,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? RespondedAtUtc,
    string? RequesterDisplayName = null,
    string? RequesterAvatarUrl = null,
    string? RequesterHeadline = null,
    string? RequesterUsername = null,
    string? AddresseeDisplayName = null,
    string? AddresseeAvatarUrl = null,
    string? AddresseeHeadline = null,
    string? AddresseeUsername = null
);

public record ConnectionUserDto(
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? AvatarUrl,
    DateTime ConnectedAtUtc,
    string? Username = null
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
    DateTime FollowedAtUtc,
    string? Username = null
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
    DateTime BlockedAtUtc,
    string? Username = null
);

public record ResearcherDiscoveryDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Headline,
    string? AvatarUrl,
    string? PrimaryAffiliation,
    bool IsVerified,
    IReadOnlyList<string> TopSkills,
    int PublicationsCount,
    int ConnectionsCount,
    string ConnectionStatus,
    bool IsFollowing
);

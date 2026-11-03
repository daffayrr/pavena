using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Social;

public class Follow : BaseEntity
{
    public Guid FollowerUserId { get; set; }
    public FollowTargetType TargetType { get; set; } = FollowTargetType.User;
    public Guid TargetId { get; set; }

    public virtual User? FollowerUser { get; set; }

    public Follow()
    {
    }

    public static Follow FollowUser(Guid followerId, Guid targetUserId)
    {
        if (followerId == targetUserId)
            throw new DomainException("Users cannot follow themselves.");

        return new Follow
        {
            FollowerUserId = followerId,
            TargetType = FollowTargetType.User,
            TargetId = targetUserId
        };
    }

    public static Follow FollowOrganization(Guid followerId, Guid organizationId)
    {
        return new Follow
        {
            FollowerUserId = followerId,
            TargetType = FollowTargetType.Organization,
            TargetId = organizationId
        };
    }
}

public class UserBlock : BaseEntity
{
    public Guid BlockerUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public string? Reason { get; set; }

    public virtual User? BlockerUser { get; set; }
    public virtual User? BlockedUser { get; set; }

    public UserBlock()
    {
    }

    public static UserBlock CreateBlock(Guid blockerId, Guid blockedId, string? reason = null)
    {
        if (blockerId == blockedId)
            throw new DomainException("Users cannot block themselves.");

        return new UserBlock
        {
            BlockerUserId = blockerId,
            BlockedUserId = blockedId,
            Reason = reason
        };
    }
}

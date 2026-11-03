using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class FollowTests
{
    [Fact]
    public void FollowUser_WhenTargetIsSelf_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();

        Action act = () => Follow.FollowUser(userId, userId);

        act.Should().Throw<DomainException>()
            .WithMessage("*Users cannot follow themselves*");
    }

    [Fact]
    public void FollowUser_WithValidUsers_CreatesFollow()
    {
        var followerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var follow = Follow.FollowUser(followerId, targetId);

        follow.FollowerUserId.Should().Be(followerId);
        follow.TargetType.Should().Be(FollowTargetType.User);
        follow.TargetId.Should().Be(targetId);
    }

    [Fact]
    public void FollowOrganization_CreatesOrganizationFollow()
    {
        var followerId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var follow = Follow.FollowOrganization(followerId, orgId);

        follow.FollowerUserId.Should().Be(followerId);
        follow.TargetType.Should().Be(FollowTargetType.Organization);
        follow.TargetId.Should().Be(orgId);
    }
}

using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class UserBlockTests
{
    [Fact]
    public void CreateBlock_WhenTargetIsSelf_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();

        Action act = () => UserBlock.CreateBlock(userId, userId);

        act.Should().Throw<DomainException>()
            .WithMessage("*Users cannot block themselves*");
    }

    [Fact]
    public void CreateBlock_WithValidUsers_CreatesBlock()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();

        var block = UserBlock.CreateBlock(blockerId, blockedId, "Harassment");

        block.BlockerUserId.Should().Be(blockerId);
        block.BlockedUserId.Should().Be(blockedId);
        block.Reason.Should().Be("Harassment");
    }
}

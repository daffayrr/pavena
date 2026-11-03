using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class ConnectionTests
{
    [Fact]
    public void CreateRequest_WithSameUsers_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();

        Action act = () => Connection.CreateRequest(userId, userId);

        act.Should().Throw<DomainException>()
            .WithMessage("*cannot send a connection request to themselves*");
    }

    [Fact]
    public void CreateRequest_WithValidUsers_CalculatesCanonicalOrder()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Request direction: id2 -> id1
        var connection = Connection.CreateRequest(id2, id1);

        connection.RequesterUserId.Should().Be(id2);
        connection.AddresseeUserId.Should().Be(id1);
        connection.CanonicalUserA.Should().Be(id1);
        connection.CanonicalUserB.Should().Be(id2);
        connection.CanonicalUserA.CompareTo(connection.CanonicalUserB).Should().BeNegative();
        connection.Status.Should().Be(ConnectionStatus.Pending);
    }

    [Fact]
    public void Accept_WhenPending_UpdatesStatusAndTimestamp()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var connection = Connection.CreateRequest(id1, id2);

        connection.Accept();

        connection.Status.Should().Be(ConnectionStatus.Accepted);
        connection.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WhenPending_UpdatesStatusAndTimestamp()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var connection = Connection.CreateRequest(id1, id2);

        connection.Reject();

        connection.Status.Should().Be(ConnectionStatus.Rejected);
        connection.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Accept_WhenNotPending_ThrowsDomainException()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var connection = Connection.CreateRequest(id1, id2);
        connection.Accept();

        Action act = () => connection.Accept();

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot accept a connection in 'Accepted' status*");
    }
}

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
    public void CreateRequest_WithEmptyGuid_ThrowsDomainException()
    {
        Action act = () => Connection.CreateRequest(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("*User identifiers cannot be empty*");
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
        connection.RespondedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Accept_ByAddressee_WhenPending_UpdatesStatusAndTimestamp()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);

        connection.Accept(addressee);

        connection.Status.Should().Be(ConnectionStatus.Accepted);
        connection.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Accept_ByRequester_ThrowsDomainException()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);

        Action act = () => connection.Accept(requester);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the recipient of a connection request can accept it*");
    }

    [Fact]
    public void Reject_ByAddressee_WhenPending_UpdatesStatusAndTimestamp()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);

        connection.Reject(addressee);

        connection.Status.Should().Be(ConnectionStatus.Rejected);
        connection.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reject_ByRequester_ThrowsDomainException()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);

        Action act = () => connection.Reject(requester);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the recipient of a connection request can reject it*");
    }

    [Fact]
    public void Cancel_ByRequester_WhenPending_UpdatesStatusToCancelled()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);

        connection.Cancel(requester);

        connection.Status.Should().Be(ConnectionStatus.Cancelled);
        connection.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ByAddressee_ThrowsDomainException()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);

        Action act = () => connection.Cancel(addressee);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the sender of a connection request can cancel it*");
    }

    [Fact]
    public void Accept_WhenNotPending_ThrowsDomainException()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var connection = Connection.CreateRequest(requester, addressee);
        connection.Accept(addressee);

        Action act = () => connection.Accept(addressee);

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot accept a connection in 'Accepted' status*");
    }

    [Fact]
    public void ReRequest_WhenRejected_ReactivatesToPending()
    {
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var connection = Connection.CreateRequest(u1, u2);
        connection.Reject(u2);

        // u2 sends request back to u1
        connection.ReRequest(u2, u1);

        connection.RequesterUserId.Should().Be(u2);
        connection.AddresseeUserId.Should().Be(u1);
        connection.Status.Should().Be(ConnectionStatus.Pending);
        connection.RespondedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ReRequest_WhenAlreadyAccepted_ThrowsDomainException()
    {
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var connection = Connection.CreateRequest(u1, u2);
        connection.Accept(u2);

        Action act = () => connection.ReRequest(u1, u2);

        act.Should().Throw<DomainException>()
            .WithMessage("*Users are already connected*");
    }

    [Fact]
    public void ParticipantHelpers_WorkCorrectly()
    {
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var connection = Connection.CreateRequest(u1, u2);

        connection.IsParticipant(u1).Should().BeTrue();
        connection.IsParticipant(u2).Should().BeTrue();
        connection.IsParticipant(stranger).Should().BeFalse();

        connection.GetOtherUserId(u1).Should().Be(u2);
        connection.GetOtherUserId(u2).Should().Be(u1);

        Action act = () => connection.GetOtherUserId(stranger);
        act.Should().Throw<DomainException>();
    }
}

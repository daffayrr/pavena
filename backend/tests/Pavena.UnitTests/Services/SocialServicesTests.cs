using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pavena.Application.Social.DTOs;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Pavena.Infrastructure.Social;
using Xunit;

namespace Pavena.UnitTests.Services;

public class SocialServicesTests
{
    private PavenaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PavenaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new PavenaDbContext(options);
    }

    private async Task<(User User1, User User2, User User3)> SeedUsersAsync(PavenaDbContext db)
    {
        var u1 = new User("alice@pavena.org", "alice") { Id = Guid.NewGuid(), PasswordHash = "AQAAAAIAAYagAAAAEI...testHash1" };
        var u2 = new User("bob@pavena.org", "bob") { Id = Guid.NewGuid(), PasswordHash = "AQAAAAIAAYagAAAAEI...testHash2" };
        var u3 = new User("carol@pavena.org", "carol") { Id = Guid.NewGuid(), PasswordHash = "AQAAAAIAAYagAAAAEI...testHash3" };

        u1.Profile = new UserProfile(u1.Id, "Alice Researcher");
        u2.Profile = new UserProfile(u2.Id, "Bob Professor");
        u3.Profile = new UserProfile(u3.Id, "Carol Student");

        db.Users.AddRange(u1, u2, u3);
        db.UserProfiles.AddRange(u1.Profile, u2.Profile, u3.Profile);
        await db.SaveChangesAsync();

        return (u1, u2, u3);
    }

    // --- CONNECTION SERVICE TESTS ---

    [Fact]
    public async Task SendRequest_WhenValidUsers_CreatesPendingConnection()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var service = new ConnectionService(db);

        var result = await service.SendRequestAsync(u1.Id, u2.Id);

        result.Should().NotBeNull();
        result.RequesterUserId.Should().Be(u1.Id);
        result.AddresseeUserId.Should().Be(u2.Id);
        result.Status.Should().Be("Pending");

        var inDb = await db.Connections.FirstOrDefaultAsync(c => c.Id == result.Id);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be(ConnectionStatus.Pending);
    }

    [Fact]
    public async Task SendRequest_WhenSelfConnection_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, _, _) = await SeedUsersAsync(db);
        var service = new ConnectionService(db);

        Func<Task> act = async () => await service.SendRequestAsync(u1.Id, u1.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*cannot send a connection request to themselves*");
    }

    [Fact]
    public async Task SendRequest_WhenTargetUserDoesNotExist_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, _, _) = await SeedUsersAsync(db);
        var service = new ConnectionService(db);

        Func<Task> act = async () => await service.SendRequestAsync(u1.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Target user does not exist*");
    }

    [Fact]
    public async Task SendRequest_WhenBlocked_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        db.UserBlocks.Add(UserBlock.CreateBlock(u2.Id, u1.Id));
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);

        Func<Task> act = async () => await service.SendRequestAsync(u1.Id, u2.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Cannot send a connection request to this user*");
    }

    [Fact]
    public async Task SendRequest_WhenAlreadyConnected_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        conn.Accept(u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);

        Func<Task> act = async () => await service.SendRequestAsync(u2.Id, u1.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Users are already connected*");
    }

    [Fact]
    public async Task SendRequest_WhenAlreadyPending_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);

        Func<Task> act = async () => await service.SendRequestAsync(u2.Id, u1.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already pending*");
    }

    [Fact]
    public async Task SendRequest_WhenPreviouslyRejected_ReactivatesToPending()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        conn.Reject(u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);
        var result = await service.SendRequestAsync(u2.Id, u1.Id);

        result.Status.Should().Be("Pending");
        result.RequesterUserId.Should().Be(u2.Id);
        result.AddresseeUserId.Should().Be(u1.Id);
    }

    [Fact]
    public async Task AcceptRequest_WhenAddressee_UpdatesToAccepted()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);
        var result = await service.AcceptRequestAsync(u2.Id, conn.Id);

        result.Status.Should().Be("Accepted");
        result.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptRequest_WhenRequesterAttempts_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);

        Func<Task> act = async () => await service.AcceptRequestAsync(u1.Id, conn.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Only the recipient of a connection request can accept it*");
    }

    [Fact]
    public async Task CancelRequest_WhenRequester_UpdatesToCancelled()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);
        var result = await service.CancelRequestAsync(u1.Id, conn.Id);

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task RemoveConnection_WhenAccepted_DeletesConnection()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        conn.Accept(u2.Id);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var service = new ConnectionService(db);
        await service.RemoveConnectionAsync(u1.Id, u2.Id);

        var exists = await db.Connections.AnyAsync();
        exists.Should().BeFalse();
    }

    // --- FOLLOW SERVICE TESTS ---

    [Fact]
    public async Task FollowUser_WhenValid_CreatesFollow()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        var service = new FollowService(db);

        await service.FollowUserAsync(u1.Id, u2.Id);

        var exists = await db.Follows.AnyAsync(f => f.FollowerUserId == u1.Id && f.TargetId == u2.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FollowUser_WhenSelf_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, _, _) = await SeedUsersAsync(db);
        var service = new FollowService(db);

        Func<Task> act = async () => await service.FollowUserAsync(u1.Id, u1.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Users cannot follow themselves*");
    }

    [Fact]
    public async Task FollowUser_WhenAlreadyFollowing_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        db.Follows.Add(Follow.FollowUser(u1.Id, u2.Id));
        await db.SaveChangesAsync();

        var service = new FollowService(db);

        Func<Task> act = async () => await service.FollowUserAsync(u1.Id, u2.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Already following this user*");
    }

    [Fact]
    public async Task UnfollowUser_WhenFollowing_RemovesFollow()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        db.Follows.Add(Follow.FollowUser(u1.Id, u2.Id));
        await db.SaveChangesAsync();

        var service = new FollowService(db);
        await service.UnfollowUserAsync(u1.Id, u2.Id);

        var exists = await db.Follows.AnyAsync();
        exists.Should().BeFalse();
    }

    // --- BLOCK SERVICE TESTS: ATOMIC MUTUAL SEVERING ---

    [Fact]
    public async Task BlockUser_AtomicallySeversConnectionsAndBilateralFollows()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);

        // Setup: accepted connection + bilateral follows
        var conn = Connection.CreateRequest(u1.Id, u2.Id);
        conn.Accept(u2.Id);
        db.Connections.Add(conn);

        db.Follows.Add(Follow.FollowUser(u1.Id, u2.Id)); // u1 follows u2
        db.Follows.Add(Follow.FollowUser(u2.Id, u1.Id)); // u2 follows u1
        await db.SaveChangesAsync();

        var blockService = new BlockService(db);

        // Action: u1 blocks u2
        await blockService.BlockUserAsync(u1.Id, u2.Id, "Spam");

        // Assert 1: Connection severed
        var connectionExists = await db.Connections.AnyAsync();
        connectionExists.Should().BeFalse();

        // Assert 2: Follows in BOTH directions severed
        var followsExist = await db.Follows.AnyAsync();
        followsExist.Should().BeFalse();

        // Assert 3: Block record created
        var block = await db.UserBlocks.FirstOrDefaultAsync();
        block.Should().NotBeNull();
        block!.BlockerUserId.Should().Be(u1.Id);
        block.BlockedUserId.Should().Be(u2.Id);
        block.Reason.Should().Be("Spam");
    }

    [Fact]
    public async Task BlockUser_WhenSelf_ThrowsDomainException()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, _, _) = await SeedUsersAsync(db);
        var blockService = new BlockService(db);

        Func<Task> act = async () => await blockService.BlockUserAsync(u1.Id, u1.Id);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Users cannot block themselves*");
    }

    [Fact]
    public async Task UnblockUser_WhenBlocked_RemovesBlockRecord()
    {
        using var db = CreateInMemoryDbContext();
        var (u1, u2, _) = await SeedUsersAsync(db);
        db.UserBlocks.Add(UserBlock.CreateBlock(u1.Id, u2.Id));
        await db.SaveChangesAsync();

        var blockService = new BlockService(db);
        await blockService.UnblockUserAsync(u1.Id, u2.Id);

        var exists = await db.UserBlocks.AnyAsync();
        exists.Should().BeFalse();
    }
}

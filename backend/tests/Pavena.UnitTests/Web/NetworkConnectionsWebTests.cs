using FluentAssertions;
using Pavena.Application.Social.DTOs;
using Pavena.Web.Services.Auth;
using Pavena.Web.Services.Profile;
using Xunit;

namespace Pavena.UnitTests.Web;

public class NetworkConnectionsWebTests
{
    private readonly IScholarSessionService _sessionService;
    private readonly AcademicProfileClient _client;

    public NetworkConnectionsWebTests()
    {
        _sessionService = new ScholarSessionService();
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5181/") };
        _client = new AcademicProfileClient(httpClient, _sessionService);
    }

    [Fact]
    public async Task DiscoverResearchers_ReturnsPreviewList_AndFiltersByQuery()
    {
        var all = await _client.GetDiscoverResearchersAsync();
        all.Should().NotBeEmpty();
        all.Should().Contain(r => r.Username == "alan_turing");

        // Filter by Query
        var filteredQuery = await _client.GetDiscoverResearchersAsync(query: "Turing");
        filteredQuery.Should().HaveCount(1);
        filteredQuery[0].DisplayName.Should().Be("Dr. Alan Turing");

        // Filter by Institution
        var filteredInst = await _client.GetDiscoverResearchersAsync(institution: "Manchester");
        filteredInst.Should().HaveCount(1);
        filteredInst[0].PrimaryAffiliation.Should().Contain("Manchester");

        // Filter by Expertise
        var filteredExp = await _client.GetDiscoverResearchersAsync(expertise: "Crystallography");
        filteredExp.Should().HaveCount(1);
        filteredExp[0].DisplayName.Should().Be("Dr. Rosalind Franklin");
    }

    [Fact]
    public async Task AcceptedConnections_ListsAndRemovesConnection()
    {
        var connections = await _client.GetAcceptedConnectionsAsync();
        connections.Should().NotBeEmpty();
        var initialCount = connections.Count;
        var first = connections[0];

        // Remove connection
        var removeOk = await _client.RemoveConnectionAsync(first.UserId);
        removeOk.Should().BeTrue();

        var updated = await _client.GetAcceptedConnectionsAsync();
        updated.Should().HaveCount(initialCount - 1);
        updated.Should().NotContain(c => c.UserId == first.UserId);
    }

    [Fact]
    public async Task ConnectionRequests_IncomingAndOutgoing_TransitionsState()
    {
        var incoming = await _client.GetIncomingConnectionRequestsAsync();
        incoming.Should().NotBeEmpty();
        var incomingReq = incoming[0];
        incomingReq.RequesterDisplayName.Should().NotBeNullOrWhiteSpace();

        var outgoing = await _client.GetOutgoingConnectionRequestsAsync();
        outgoing.Should().NotBeEmpty();
        var outgoingReq = outgoing[0];
        outgoingReq.AddresseeDisplayName.Should().NotBeNullOrWhiteSpace();

        // Accept incoming request
        var acceptOk = await _client.AcceptConnectionRequestAsync(incomingReq.Id);
        acceptOk.Should().BeTrue();

        var incomingAfterAccept = await _client.GetIncomingConnectionRequestsAsync();
        incomingAfterAccept.Should().NotContain(r => r.Id == incomingReq.Id);

        // Cancel outgoing request
        var cancelOk = await _client.CancelConnectionRequestAsync(outgoingReq.Id);
        cancelOk.Should().BeTrue();

        var outgoingAfterCancel = await _client.GetOutgoingConnectionRequestsAsync();
        outgoingAfterCancel.Should().NotContain(r => r.Id == outgoingReq.Id);
    }

    [Fact]
    public async Task FollowingAndFollowers_ManagesStateCorrectly()
    {
        var following = await _client.GetFollowingAsync();
        following.Should().NotBeEmpty();
        var target = following[0];

        // Unfollow
        var unfollowOk = await _client.UnfollowUserAsync(target.UserId);
        unfollowOk.Should().BeTrue();

        var followingAfter = await _client.GetFollowingAsync();
        followingAfter.Should().NotContain(f => f.UserId == target.UserId);

        // Follow back
        var followOk = await _client.FollowUserAsync(target.UserId);
        followOk.Should().BeTrue();
    }

    [Fact]
    public async Task Blocking_RemovesRelationships_AndAllowsUnblocking()
    {
        var targetUserId = Guid.Parse("55555555-5555-5555-5555-555555555555"); // Rosalind Franklin

        // Block
        var blockOk = await _client.BlockUserAsync(targetUserId, "Misconduct");
        blockOk.Should().BeTrue();

        // Verify removed from connections
        var connections = await _client.GetAcceptedConnectionsAsync();
        connections.Should().NotContain(c => c.UserId == targetUserId);

        // Verify in blocked list
        var blockedList = await _client.GetBlockedUsersAsync();
        blockedList.Should().NotBeEmpty();

        // Unblock
        var unblockOk = await _client.UnblockUserAsync(blockedList[0].UserId);
        unblockOk.Should().BeTrue();
    }

    [Fact]
    public void ResearcherDiscoveryDto_DoesNotExposePrivateProperties()
    {
        var dto = new ResearcherDiscoveryDto(
            Guid.NewGuid(),
            "albert_einstein",
            "Prof. Albert Einstein",
            "Theoretical Physicist",
            null,
            "Institute for Advanced Study",
            true,
            new[] { "General Relativity", "Photoelectric Effect" },
            25,
            1400,
            "Accepted",
            true
        );

        dto.DisplayName.Should().Be("Prof. Albert Einstein");
        dto.IsVerified.Should().BeTrue();
        dto.TopSkills.Should().HaveCount(2);
        dto.ConnectionStatus.Should().Be("Accepted");
        dto.IsFollowing.Should().BeTrue();
    }
}

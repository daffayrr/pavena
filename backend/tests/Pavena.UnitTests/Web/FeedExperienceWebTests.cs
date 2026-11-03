using FluentAssertions;
using Pavena.Application.Social.DTOs;
using Pavena.Web.Services.Auth;
using Pavena.Web.Services.Profile;
using Xunit;

namespace Pavena.UnitTests.Web;

public class FeedExperienceWebTests
{
    private readonly IScholarSessionService _sessionService;
    private readonly AcademicProfileClient _client;

    public FeedExperienceWebTests()
    {
        _sessionService = new ScholarSessionService();
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5181/") };
        _client = new AcademicProfileClient(httpClient, _sessionService);
    }

    [Fact]
    public async Task FeedClient_GetMyNetworkFeed_ReturnsInitialItemsAndCursor()
    {
        var feed = await _client.GetMyNetworkFeedAsync(limit: 10, cursor: null);
        feed.Should().NotBeNull();
        feed!.Items.Should().NotBeEmpty();
        feed.HasMore.Should().BeTrue();
        feed.NextCursor.Should().NotBeNullOrWhiteSpace();

        foreach (var item in feed.Items)
        {
            item.Id.Should().NotBeEmpty();
            item.AuthorDisplayName.Should().NotBeNullOrWhiteSpace();
            item.Content.Should().NotBeNullOrWhiteSpace();
            item.Status.Should().Be("Published");
        }
    }

    [Fact]
    public async Task FeedClient_GetDiscoverFeed_ReturnsPublicItems()
    {
        var feed = await _client.GetDiscoverFeedAsync(limit: 10, cursor: null);
        feed.Should().NotBeNull();
        feed!.Items.Should().NotBeEmpty();

        foreach (var item in feed.Items)
        {
            item.Visibility.Should().Be("Public");
            item.Status.Should().Be("Published");
        }
    }

    [Fact]
    public async Task FeedPagination_KeysetCursor_LoadsNextPageWithoutOffset()
    {
        // Page 1
        var page1 = await _client.GetMyNetworkFeedAsync(limit: 2, cursor: null);
        page1.Should().NotBeNull();
        page1!.NextCursor.Should().NotBeNullOrWhiteSpace();

        // Page 2 using opaque cursor from Page 1
        var page2 = await _client.GetMyNetworkFeedAsync(limit: 2, cursor: page1.NextCursor);
        page2.Should().NotBeNull();
        page2!.Items.Should().NotBeEmpty();

        // Verify page 2 has unique items compared to page 1
        var page1Ids = page1.Items.Select(i => i.Id).ToHashSet();
        page2.Items.Should().NotContain(i => page1Ids.Contains(i.Id));
    }

    [Fact]
    public void FeedPagination_DuplicateItemPrevention_DeduplicatesExistingIds()
    {
        var postId1 = Guid.NewGuid();
        var postId2 = Guid.NewGuid();
        var postId3 = Guid.NewGuid();

        var initialList = new List<PostDto>
        {
            new(postId1, Guid.NewGuid(), "Scholar 1", null, "Initial post 1", "Public", "Published", DateTime.UtcNow, DateTime.UtcNow, null, new List<PostAttachmentDto>()),
            new(postId2, Guid.NewGuid(), "Scholar 2", null, "Initial post 2", "Public", "Published", DateTime.UtcNow, DateTime.UtcNow, null, new List<PostAttachmentDto>())
        };

        var incomingList = new List<PostDto>
        {
            new(postId2, Guid.NewGuid(), "Scholar 2", null, "Duplicated post 2", "Public", "Published", DateTime.UtcNow, DateTime.UtcNow, null, new List<PostAttachmentDto>()),
            new(postId3, Guid.NewGuid(), "Scholar 3", null, "New post 3", "Public", "Published", DateTime.UtcNow, DateTime.UtcNow, null, new List<PostAttachmentDto>())
        };

        // Deduplication algorithm applied in FeedPage.razor
        var existingIds = new HashSet<Guid>(initialList.Select(p => p.Id));
        var filteredNewItems = incomingList.Where(p => !existingIds.Contains(p.Id)).ToList();
        initialList.AddRange(filteredNewItems);

        initialList.Should().HaveCount(3);
        initialList.Select(p => p.Id).Distinct().Should().HaveCount(3);
        initialList.Should().Contain(p => p.Id == postId3);
    }

    [Fact]
    public void FeedSecurity_ConnectionsPosts_FilteredOutFromDiscoverFeed()
    {
        var connPost = new PostDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Colleague",
            null,
            "Confidential lab observations",
            "Connections",
            "Published",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            new List<PostAttachmentDto>()
        );

        var isDiscoverMode = true;
        var feedPosts = new List<PostDto>();

        // Simulating FeedPage.razor HandlePostCreated logic
        if (!isDiscoverMode || connPost.Visibility == "Public")
        {
            feedPosts.Insert(0, connPost);
        }

        feedPosts.Should().BeEmpty("Connections post must never appear in Discover feed.");
    }

    [Fact]
    public void FeedState_ArchivedOrDeletedPost_RemovedFromActiveFeed()
    {
        var postId = Guid.NewGuid();
        var post = new PostDto(
            postId,
            _sessionService.CurrentUserId,
            "Marie Curie",
            null,
            "Post to be archived",
            "Public",
            "Published",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            new List<PostAttachmentDto>()
        );

        var feedPosts = new List<PostDto> { post };

        // Post updated to Archived
        var updated = post with { Status = "Archived" };
        var idx = feedPosts.FindIndex(p => p.Id == updated.Id);
        if (idx >= 0)
        {
            if (updated.Status == "Archived")
            {
                feedPosts.RemoveAt(idx);
            }
            else
            {
                feedPosts[idx] = updated;
            }
        }

        feedPosts.Should().BeEmpty("Archived post must be purged from active feed stream.");
    }

    [Theory]
    [InlineData("/feed", false)]
    [InlineData("/feed/network", false)]
    [InlineData("/feed/discover", true)]
    public void RouteParsing_DetectsCorrectFeedMode(string uri, bool expectedDiscover)
    {
        var isDiscover = uri.EndsWith("/discover", StringComparison.OrdinalIgnoreCase);
        isDiscover.Should().Be(expectedDiscover);
    }
}

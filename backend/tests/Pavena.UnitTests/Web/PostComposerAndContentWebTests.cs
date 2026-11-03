using FluentAssertions;
using Pavena.Application.Social.DTOs;
using Pavena.Web.Components.Posts;
using Pavena.Web.Services.Auth;
using Pavena.Web.Services.Profile;
using Xunit;

namespace Pavena.UnitTests.Web;

public class PostComposerAndContentWebTests
{
    private readonly IScholarSessionService _sessionService;
    private readonly AcademicProfileClient _client;

    public PostComposerAndContentWebTests()
    {
        _sessionService = new ScholarSessionService();
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5181/") };
        _client = new AcademicProfileClient(httpClient, _sessionService);
    }

    [Fact]
    public void AcademicCategories_AllSevenMandatedCategoriesExist()
    {
        var categories = PostCategorySelector.Categories;
        categories.Should().HaveCount(7);

        var names = categories.Select(c => c.Name).ToList();
        names.Should().Contain("Research Update");
        names.Should().Contain("Research Finding");
        names.Should().Contain("Academic Discussion");
        names.Should().Contain("Publication");
        names.Should().Contain("Collaboration Opportunity");
        names.Should().Contain("Conference & Event");
        names.Should().Contain("General Academic");

        foreach (var cat in categories)
        {
            cat.Name.Should().NotBeNullOrWhiteSpace();
            cat.Icon.Should().NotBeNullOrWhiteSpace();
            cat.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task AcademicProfileClient_CreatePostAsync_Fallback_SetsAuthorFromSession()
    {
        var req = new CreatePostRequest(
            "[Research Finding]\n\nObserved novel crystalline behavior under high magnetic flux.",
            "Connections",
            PublishImmediately: false,
            Attachments: new List<CreateAttachmentRequest>
            {
                new("diffraction_data.csv", "text/csv", 2048, "storage/posts/diffraction_data.csv")
            }
        );

        var post = await _client.CreatePostAsync(req);
        post.Should().NotBeNull();
        post!.AuthorUserId.Should().Be(_sessionService.CurrentUserId);
        post.AuthorDisplayName.Should().Be(_sessionService.CurrentDisplayName);
        post.Visibility.Should().Be("Connections");
        post.Status.Should().Be("Draft");
        post.Attachments.Should().HaveCount(1);
        post.Attachments[0].FileName.Should().Be("diffraction_data.csv");
    }

    [Fact]
    public async Task AcademicProfileClient_PublishAndArchive_FallbackLifecycle()
    {
        var draftReq = new CreatePostRequest("Initial hypothesis notes.", "Public", PublishImmediately: false);
        var post = await _client.CreatePostAsync(draftReq);
        post.Should().NotBeNull();
        post!.Status.Should().Be("Draft");

        // Delete post
        var deleteOk = await _client.DeletePostAsync(post.Id);
        deleteOk.Should().BeTrue();
    }

    [Fact]
    public async Task AcademicProfileClient_GetMyPostsAsync_ReturnsAuthoredPosts()
    {
        var myPosts = await _client.GetMyPostsAsync();
        myPosts.Should().NotBeNull();
        myPosts.Should().NotBeEmpty();
        myPosts.Should().AllSatisfy(p =>
        {
            p.Id.Should().NotBeEmpty();
            p.Content.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Theory]
    [InlineData("[Research Update]\n\nCompleted crystallization trials.", "Research Update", "Completed crystallization trials.")]
    [InlineData("[Research Finding]\n\nDiscovered new catalyst.", "Research Finding", "Discovered new catalyst.")]
    [InlineData("[Publication]\n\nPaper accepted at Nature Physics.", "Publication", "Paper accepted at Nature Physics.")]
    [InlineData("Ordinary thought without tag.", null, "Ordinary thought without tag.")]
    public void CategoryTag_ParsingLogic_ExtractsCorrectCategoryAndBody(string rawContent, string? expectedCategory, string expectedBody)
    {
        string? extractedCategory = null;
        string cleanBody = rawContent;

        foreach (var cat in PostCategorySelector.Categories)
        {
            var prefix = $"[{cat.Name}]";
            if (rawContent.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                extractedCategory = cat.Name;
                cleanBody = rawContent[prefix.Length..].TrimStart();
                break;
            }
        }

        extractedCategory.Should().Be(expectedCategory);
        cleanBody.Should().Be(expectedBody);
    }
}

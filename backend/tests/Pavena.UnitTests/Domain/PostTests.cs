using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class PostTests
{
    [Fact]
    public void CreateDraft_WithValidData_CreatesDraftPost()
    {
        var authorId = Guid.NewGuid();
        var content = "This is a draft research idea regarding quantum entanglement.";

        var post = Post.CreateDraft(authorId, content, PostVisibility.Connections);

        post.AuthorUserId.Should().Be(authorId);
        post.Content.Should().Be(content);
        post.Visibility.Should().Be(PostVisibility.Connections);
        post.Status.Should().Be(PostStatus.Draft);
        post.PublishedAtUtc.Should().BeNull();
        post.EditedAtUtc.Should().BeNull();
        post.IsDeleted.Should().BeFalse();
        post.DeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CreatePublished_WithEmptyContent_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();

        Action act = () => Post.CreatePublished(authorId, "   ", PostVisibility.Public);

        act.Should().Throw<DomainException>()
            .WithMessage("*Published post content cannot be empty*");
    }

    [Fact]
    public void CreatePublished_WithContentExceedingMaxLength_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();
        var excessiveContent = new string('A', Post.MaxContentLength + 1);

        Action act = () => Post.CreatePublished(authorId, excessiveContent, PostVisibility.Public);

        act.Should().Throw<DomainException>()
            .WithMessage($"*Post content cannot exceed {Post.MaxContentLength} characters*");
    }

    [Fact]
    public void Publish_WhenDraft_TransitionsToPublished()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreateDraft(authorId, "Draft to publish", PostVisibility.Public);

        post.Publish(authorId);

        post.Status.Should().Be(PostStatus.Published);
        post.PublishedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Publish_ByNonAuthor_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var post = Post.CreateDraft(authorId, "Draft to publish", PostVisibility.Public);

        Action act = () => post.Publish(strangerId);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the author has permission*");
    }

    [Fact]
    public void Archive_ByAuthor_TransitionsToArchived()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Active research note", PostVisibility.Public);

        post.Archive(authorId);

        post.Status.Should().Be(PostStatus.Archived);
    }

    [Fact]
    public void UpdateContent_ByAuthor_UpdatesContentAndVisibility()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Original content", PostVisibility.Public);

        post.UpdateContent(authorId, "Updated refined discussion", PostVisibility.Connections);

        post.Content.Should().Be("Updated refined discussion");
        post.Visibility.Should().Be(PostVisibility.Connections);
        post.EditedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_ByNonAuthor_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Original content", PostVisibility.Public);

        Action act = () => post.UpdateContent(strangerId, "Malicious edit", PostVisibility.Public);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the author has permission*");
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndTimestamp()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Post to delete", PostVisibility.Public);

        post.SoftDelete(authorId);

        post.IsDeleted.Should().BeTrue();
        post.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AddAttachment_WhenValid_AddsAttachment()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Paper announcement", PostVisibility.Public);

        var attachment = post.AddAttachment("paper.pdf", "application/pdf", 1024 * 1024, "storage/posts/paper_123.pdf");

        post.Attachments.Should().ContainSingle();
        attachment.FileName.Should().Be("paper.pdf");
        attachment.FileSizeBytes.Should().Be(1024 * 1024);
        attachment.StorageKey.Should().Be("storage/posts/paper_123.pdf");
    }

    [Fact]
    public void AddAttachment_WhenExceedingMaxSize_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Paper announcement", PostVisibility.Public);
        var oversized = Post.MaxAttachmentSizeBytes + 1;

        Action act = () => post.AddAttachment("video.mp4", "video/mp4", oversized, "storage/video.mp4");

        act.Should().Throw<DomainException>()
            .WithMessage("*Attachment file size must be between*");
    }

    [Fact]
    public void AddAttachment_WhenExceedingMaxCount_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();
        var post = Post.CreatePublished(authorId, "Attachments test", PostVisibility.Public);

        for (int i = 0; i < Post.MaxAttachmentsCount; i++)
        {
            post.AddAttachment($"file_{i}.txt", "text/plain", 100, $"storage/file_{i}.txt");
        }

        Action act = () => post.AddAttachment("extra.txt", "text/plain", 100, "storage/extra.txt");

        act.Should().Throw<DomainException>()
            .WithMessage($"*cannot have more than {Post.MaxAttachmentsCount} attachments*");
    }
}

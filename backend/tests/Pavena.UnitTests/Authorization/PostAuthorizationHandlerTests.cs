using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pavena.API.Authorization;
using Pavena.Application.Common.Authorization;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Xunit;

namespace Pavena.UnitTests.Authorization;

public class PostAuthorizationHandlerTests
{
    private PavenaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PavenaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new PavenaDbContext(options);
    }

    private ClaimsPrincipal CreatePrincipal(Guid userId, string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task HandleRequirement_AuthorCanViewOwnPost_InAnyStatusOrVisibility()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var principal = CreatePrincipal(authorId);

        var draftPost = Post.CreateDraft(authorId, "Draft idea", PostVisibility.OnlyMe);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            draftPost
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CannotViewDraftPost()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var principal = CreatePrincipal(strangerId);

        var draftPost = Post.CreateDraft(authorId, "Draft idea", PostVisibility.Public);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            draftPost
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CannotViewArchivedPost()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var principal = CreatePrincipal(strangerId);

        var post = Post.CreatePublished(authorId, "Archived thought", PostVisibility.Public);
        post.Archive(authorId);

        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CanViewPublishedPublicPost_WhenNotBlocked()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var principal = CreatePrincipal(strangerId);

        var post = Post.CreatePublished(authorId, "Public research announcement", PostVisibility.Public);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CannotViewPublicPost_WhenBlocked()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        // Author blocked stranger
        db.UserBlocks.Add(UserBlock.CreateBlock(authorId, strangerId));
        await db.SaveChangesAsync();

        var principal = CreatePrincipal(strangerId);

        var post = Post.CreatePublished(authorId, "Public research announcement", PostVisibility.Public);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CanViewConnectionsPost_OnlyWhenAcceptedConnection()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        // Mutual accepted connection
        var conn = Connection.CreateRequest(authorId, friendId);
        conn.Accept(friendId);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var principal = CreatePrincipal(friendId);

        var post = Post.CreatePublished(authorId, "Lab discussion for connections", PostVisibility.Connections);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_FollowerCannotViewConnectionsPost_WithoutConnection()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var followerId = Guid.NewGuid();

        // User follows author, but has NO accepted connection
        db.Follows.Add(Follow.FollowUser(followerId, authorId));
        await db.SaveChangesAsync();

        var principal = CreatePrincipal(followerId);

        var post = Post.CreatePublished(authorId, "Lab discussion for connections", PostVisibility.Connections);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CannotViewOnlyMePost()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        // Even with accepted connection
        var conn = Connection.CreateRequest(authorId, friendId);
        conn.Accept(friendId);
        db.Connections.Add(conn);
        await db.SaveChangesAsync();

        var principal = CreatePrincipal(friendId);

        var post = Post.CreatePublished(authorId, "Private research diary", PostVisibility.OnlyMe);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Read) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_NonAuthor_CannotEditPost()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var principal = CreatePrincipal(strangerId);

        var post = Post.CreatePublished(authorId, "Original post", PostVisibility.Public);
        var context = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Update) },
            principal,
            post
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_Delete_SuperadminCanDelete_RegularNonAuthorCannot()
    {
        using var db = CreateDbContext();
        var handler = new PostAuthorizationHandler(db);
        var authorId = Guid.NewGuid();
        var regularUserId = Guid.NewGuid();
        var superadminId = Guid.NewGuid();

        var post = Post.CreatePublished(authorId, "Moderation test post", PostVisibility.Public);

        // 1. Regular non-author
        var regularPrincipal = CreatePrincipal(regularUserId);
        var regularContext = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Delete) },
            regularPrincipal,
            post
        );
        await handler.HandleAsync(regularContext);
        regularContext.HasSucceeded.Should().BeFalse();

        // 2. Superadmin
        var adminPrincipal = CreatePrincipal(superadminId, PlatformRole.Superadmin);
        var adminContext = new AuthorizationHandlerContext(
            new[] { new ResourceOperationRequirement(ResourceOperation.Delete) },
            adminPrincipal,
            post
        );
        await handler.HandleAsync(adminContext);
        adminContext.HasSucceeded.Should().BeTrue();
    }
}
